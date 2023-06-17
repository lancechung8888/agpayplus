﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Params.SxfPay;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Payment.Api.Channel.SxfPay.Utils;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.SxfPay
{
    public class SxfPayPaymentService : AbstractPaymentService
    {
        private readonly ILog log = LogManager.GetLogger(typeof(SxfPayPaymentService));

        public SxfPayPaymentService(IServiceProvider serviceProvider,
            ISysConfigService sysConfigService,
            ConfigContextQueryService configContextQueryService)
            : base(serviceProvider, sysConfigService, configContextQueryService)
        {
        }

        public override string GetIfCode()
        {
            return CS.IF_CODE.SXFPAY;
        }

        public override bool IsSupport(string wayCode)
        {
            return true;
        }

        public override AbstractRS Pay(UnifiedOrderRQ bizRQ, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            return PayWayUtil.GetRealPayWayService(this, payOrder.WayCode).Pay(bizRQ, payOrder, mchAppConfigContext);
        }

        public override string PreCheck(UnifiedOrderRQ bizRQ, PayOrderDto payOrder)
        {
            return PayWayUtil.GetRealPayWayService(this, payOrder.WayCode).PreCheck(bizRQ, payOrder);
        }

        public ChannelRetMsg SxfBar(JObject reqParams, string logPrefix, MchAppConfigContext mchAppConfigContext)
        {
            ChannelRetMsg channelRetMsg = new ChannelRetMsg();
            // 发送请求
            JObject resJSON = PackageParamAndReq("/order/reverseScan", reqParams, logPrefix, mchAppConfigContext);
            //请求 & 响应成功， 判断业务逻辑
            string code = resJSON.GetValue("code").ToString(); //请求响应码
            string msg = resJSON.GetValue("msg").ToString(); //响应信息
            try
            {
                if ("0000".Equals(code))
                {
                    var respData = resJSON.GetValue("respData").ToObject<JObject>();
                    string bizCode = respData.GetValue("bizCode").ToString(); //业务响应码
                    string bizMsg = respData.GetValue("bizMsg").ToString(); //业务响应信息
                    if ("0000".Equals(bizCode))
                    {
                        /*订单状态
                        取值范围：
                        SUCCESS 交易成功
                        FAIL 交易失败
                        PAYING 支付中*/
                        string tranSts = respData.GetValue("tranSts").ToString();
                        string uuid = respData.GetValue("uuid").ToString();//天阙平台订单号
                        /*落单号
                        仅供退款使用
                        消费者账单中的条形码订单号*/
                        string sxfUuid = respData.GetValue("sxfUuid").ToString();
                        string transactionId = respData.GetValue("transactionId").ToString();//微信/支付宝流水号
                        /*买家用户号
                        支付宝渠道：买家支付宝用户号buyer_user_id
                        微信渠道：微信平台的sub_openid*/
                        string buyerId = respData.GetValue("buyerId").ToString();
                        switch (tranSts)
                        {
                            case "SUCCESS":
                                channelRetMsg.ChannelOrderId = uuid;
                                channelRetMsg.ChannelUserId = buyerId;
                                channelRetMsg.PlatformOrderId = transactionId;
                                channelRetMsg.PlatformMchOrderId = sxfUuid;
                                channelRetMsg.ChannelState = ChannelState.CONFIRM_SUCCESS;
                                break;
                            case "FAIL":
                                channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                                break;
                            case "PAYING":
                                channelRetMsg.ChannelState = ChannelState.WAITING;
                                channelRetMsg.IsNeedQuery = true; // 开启轮询查单;
                                break;
                        }
                    }
                    else
                    {
                        channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                        channelRetMsg.ChannelErrCode = bizCode;
                        channelRetMsg.ChannelErrMsg = bizMsg;
                    }
                }
                else
                {
                    channelRetMsg.ChannelState = ChannelState.WAITING;
                    channelRetMsg.IsNeedQuery = true; // 开启轮询查单
                }
            }
            catch (Exception e)
            {
                channelRetMsg.ChannelErrCode = code;
                channelRetMsg.ChannelErrMsg = msg;
            }

            return channelRetMsg;
        }

        /// <summary>
        /// 获取随行付正式环境/沙箱HOST地址
        /// </summary>
        /// <param name="isvParams"></param>
        /// <returns></returns>
        public static string GetSxfpayHost4env(SxfPayIsvParams isvParams)
        {
            return CS.YES == isvParams.Sandbox ? SxfPayConfig.SANDBOX_SERVER_URL : SxfPayConfig.PROD_SERVER_URL;
        }

        /// <summary>
        /// 封装参数 & 统一请求
        /// </summary>
        /// <param name="apiUri"></param>
        /// <param name="reqParams"></param>
        /// <param name="logPrefix"></param>
        /// <param name="mchAppConfigContext"></param>
        /// <returns></returns>
        /// <exception cref="BizException"></exception>
        public JObject PackageParamAndReq(string apiUri, JObject reqData, string logPrefix, MchAppConfigContext mchAppConfigContext)
        {
            SxfPayIsvParams isvParams = (SxfPayIsvParams)_configContextQueryService.QueryIsvParams(mchAppConfigContext.MchInfo.IsvNo, GetIfCode());

            if (isvParams.OrgId == null)
            {
                log.Error($"服务商配置为空：isvParams：{JsonConvert.SerializeObject(isvParams)}");
                throw new BizException("服务商配置为空。");
            }

            SxfPayIsvSubMchParams isvsubMchParams = (SxfPayIsvSubMchParams)_configContextQueryService.QueryIsvSubMchParams(mchAppConfigContext.MchNo, mchAppConfigContext.AppId, GetIfCode());
            reqData.Add("mno", isvsubMchParams.Mno); // 商户号

            var reqParams = new JObject();//reqData
            reqParams.Add("orgId", isvParams.OrgId); //天阙平台机构编号
            reqParams.Add("reqId", Guid.NewGuid().ToString("N")); //合作方系统生成的唯一请求ID，最大长度32位
            reqParams.Add("reqData", reqData.ToString()); //每个接口的业务参数
            reqParams.Add("timestamp", DateTime.Now.ToString("yyyyMMddHHmmss")); //请求时间戳，格式：yyyyMMddHHmmss
            reqParams.Add("version", "1.0"); //接口版本号，默认值：1.0
            reqParams.Add("signType", "RSA"); //签名类型，默认值：RSA

            // 签名
            string privateKey = isvParams.PrivateKey;
            reqParams.Add("sign", SxfSignUtil.Sign(reqParams, privateKey)); //RSA 签名字符串

            // 调起上游接口
            log.Info($"{logPrefix} reqJSON={reqParams}");
            string resText = SxfHttpUtil.DoPostJson(GetSxfpayHost4env(isvParams) + apiUri, reqParams);
            log.Info($"{logPrefix} resJSON={resText}");

            if (string.IsNullOrWhiteSpace(resText))
            {
                return null;
            }

            // 验签
            var resParams = JObject.Parse(resText);
            string publicKey = isvParams.PublicKey;
            if (!SxfSignUtil.Verify(resParams, publicKey))
            {
                log.Warn($"{logPrefix} 验签失败！ reqJSON={reqParams} resJSON={resParams}");
            }

            return resParams;
        }

        /// <summary>
        /// 随行付 jsapi下单请求统一发送参数
        /// </summary>
        /// <param name="reqParams"></param>
        /// <param name="payOrder"></param>
        /// <param name="notifyUrl"></param>
        /// <param name="returnUrl"></param>
        public static void JsapiParamsSet(JObject reqParams, PayOrderDto payOrder, string notifyUrl, string returnUrl)
        {
            SxfPublicParams(reqParams, payOrder);
            string payType = SxfHttpUtil.GetPayType(payOrder.WayCode);
            /*支付渠道，枚举值
            取值范围：
            WECHAT 微信
            ALIPAY 支付宝
            UNIONPAY 银联*/
            reqParams.Add("payType", payType);
            string payWay = SxfHttpUtil.GetPayWay(payOrder.WayCode);
            /*支付方式，枚举值
            取值范围：
            02 微信公众号 / 支付宝生活号 / 银联js支付 / 支付宝小程序
            03 微信小程序*/
            reqParams.Add("payWay", payWay);
            reqParams.Add("notifyUrl", notifyUrl); //支付结果通知地址不上送则交易成功后，无异步交易结果通知
            reqParams.Add("outFrontUrl", returnUrl); //银联js支付成功前端跳转地址与成功地址/失败地址同时存在或同时不存在
            reqParams.Add("outFrontFailUrl", returnUrl); //银联js支付成功前端跳转地址与成功地址/失败地址同时存在或同时不存在
        }

        /// <summary>
        /// 随行付 bar下单请求统一发送参数
        /// </summary>
        /// <param name="reqParams"></param>
        /// <param name="payOrder"></param>
        public static void BarParamsSet(JObject reqParams, PayOrderDto payOrder)
        {
            SxfPublicParams(reqParams, payOrder);
            string payType = SxfHttpUtil.GetPayType(payOrder.WayCode);
            /*支付渠道，枚举值
            取值范围：
            WECHAT 微信
            ALIPAY 支付宝
            UNIONPAY 银联*/
            reqParams.Add("payType", payType);
        }

        /// <summary>
        /// 随行付公共参数赋值
        /// </summary>
        /// <param name="reqParams"></param>
        /// <param name="payOrder"></param>
        public static void SxfPublicParams(JObject reqParams, PayOrderDto payOrder)
        {
            //获取订单类型
            reqParams.Add("ordNo", payOrder.PayOrderId); //商户订单号（字母、数字、下划线）需保证在合作方系统中不重复
            reqParams.Add("amt", payOrder.Amount); //订单总金额(元)，格式：#########.##
            /*支付渠道，枚举值
            取值范围：
            WECHAT 微信
            ALIPAY 支付宝
            UNIONPAY 银联*/
            //reqParams.Add("payType", "");
            /*支付方式，枚举值
            取值范围：
            02 微信公众号 / 支付宝生活号 / 银联js支付 / 支付宝小程序
            03 微信小程序*/
            //reqParams.Add("payWay", "");
            reqParams.Add("subject", payOrder.Subject); //订单标题
            reqParams.Add("trmIp", payOrder.ClientIp); //商户终端ip地址
        }
    }
}

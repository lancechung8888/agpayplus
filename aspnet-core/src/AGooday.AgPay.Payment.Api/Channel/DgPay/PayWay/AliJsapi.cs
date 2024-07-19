﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Channel.DgPay.Enumerator;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.DgPay.PayWay
{
    /// <summary>
    /// 斗拱 支付宝 jsapi
    /// </summary>
    public class AliJsapi : DgPayPaymentService
    {
        public AliJsapi(ILogger<AliJsapi> logger, 
            IServiceProvider serviceProvider,
            ISysConfigService sysConfigService,
            ConfigContextQueryService configContextQueryService)
            : base(logger, serviceProvider, sysConfigService, configContextQueryService)
        {
        }

        public override AbstractRS Pay(UnifiedOrderRQ rq, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            string logPrefix = "【斗拱(alipayJs)jsapi支付】";
            AliJsapiOrderRQ bizRQ = (AliJsapiOrderRQ)rq;
            JObject reqParams = new JObject();
            AliJsapiOrderRS res = ApiResBuilder.BuildSuccess<AliJsapiOrderRS>();
            ChannelRetMsg channelRetMsg = new ChannelRetMsg();
            res.ChannelRetMsg = channelRetMsg;

            // 请求参数赋值
            UnifiedParamsSet(reqParams, payOrder, GetNotifyUrl(), GetReturnUrl());

            //斗拱扫一扫支付， 需要传入buyerUserId参数
            /*用户号（微信openid / 支付宝userid / 银联userid）
            payType == "WECHAT"或"ALIPAY"时必传*/
            var alipayData = JObject.FromObject(new { buyer_id = bizRQ.GetChannelUserId() });
            reqParams.Add("alipay_data", alipayData.ToString());//支付宝扩展参数集合
            reqParams.Add("trade_type", DgPayEnum.TransType.A_JSAPI.ToString());//交易类型

            // 发送请求
            JObject resJSON = PackageParamAndReq("/trade/payment/jspay", reqParams, logPrefix, mchAppConfigContext);
            //请求 & 响应成功， 判断业务逻辑
            var data = resJSON.GetValue("data")?.ToObject<JObject>();
            string respCode = data?.GetValue("resp_code").ToString(); //业务响应码
            string respDesc = data?.GetValue("resp_desc").ToString(); //业务响应信息
            string bankCode = null, bankDesc = null, bankMessage = null;
            data?.TryGetString("bank_code", out bankCode); //外部通道返回码
            data?.TryGetString("bank_desc", out bankDesc); //外部通道返回描述
            data?.TryGetString("bank_message", out bankMessage); //外部通道返回描述
            string code = bankCode ?? respCode;
            string msg = (bankMessage ?? bankDesc) ?? respDesc;
            string huifuId = data?.GetValue("huifu_id")?.ToString();
            channelRetMsg.ChannelMchNo = huifuId;
            try
            {
                if ("00000000".Equals(respCode) || "00000100".Equals(respCode))
                {
                    data.TryGetString("hf_seq_id", out string hfSeqId);//全局流水号
                    data.TryGetString("req_seq_id", out string reqSeqId);//请求流水号
                    data.TryGetString("party_order_id", out string partyOrderId);//用户账单上的商户订单号	
                    var payInfo = data.GetValue("pay_info").ToString();
                    var tradeNo = JObject.Parse(payInfo).GetValue("tradeNO").ToString();
                    string _transStat = data.GetValue("trans_stat").ToString();
                    var transStat = DgPayEnum.ConvertTransStat(_transStat);
                    switch (transStat)
                    {
                        case DgPayEnum.TransStat.P:
                            res.AlipayTradeNo = tradeNo;
                            channelRetMsg.ChannelOrderId = hfSeqId;
                            channelRetMsg.ChannelState = ChannelState.WAITING;
                            break;
                        case DgPayEnum.TransStat.F:
                            channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                            channelRetMsg.ChannelErrCode = code;
                            channelRetMsg.ChannelErrMsg = msg;
                            break;
                    }
                }
                else if ("90000000".Equals(respCode))
                {
                    channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                    channelRetMsg.ChannelErrCode = code;
                    channelRetMsg.ChannelErrMsg = msg;
                }
                else
                {
                    channelRetMsg.ChannelState = ChannelState.WAITING;
                    channelRetMsg.IsNeedQuery = true; // 开启轮询查单
                }
            }
            catch (Exception)
            {
                channelRetMsg.ChannelErrCode = code;
                channelRetMsg.ChannelErrMsg = msg;
            }
            return res;
        }

        public override string PreCheck(UnifiedOrderRQ rq, PayOrderDto payOrder)
        {
            AliJsapiOrderRQ bizRQ = (AliJsapiOrderRQ)rq;
            if (string.IsNullOrWhiteSpace(bizRQ.GetChannelUserId()))
            {
                throw new BizException("[buyerUserId]不可为空");
            }

            return null;
        }
    }
}

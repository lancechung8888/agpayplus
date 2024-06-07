﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Params.YsePay;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Channel.YsePay.Enumerator;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.YsePay.PayWay
{
    /// <summary>
    /// 银盛 支付宝 二维码支付
    /// </summary>
    public class AliQr : YsePayPaymentService
    {
        public AliQr(IServiceProvider serviceProvider,
            ISysConfigService sysConfigService,
            ConfigContextQueryService configContextQueryService)
            : base(serviceProvider, sysConfigService, configContextQueryService)
        {
        }

        public override AbstractRS Pay(UnifiedOrderRQ rq, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            string logPrefix = "【银盛(alipay)二维码支付】";
            AliQrOrderRQ bizRQ = (AliQrOrderRQ)rq;
            SortedDictionary<string, string> reqParams = new SortedDictionary<string, string>();
            AliQrOrderRS res = ApiResBuilder.BuildSuccess<AliQrOrderRS>();
            ChannelRetMsg channelRetMsg = new ChannelRetMsg();
            res.ChannelRetMsg = channelRetMsg;

            // 请求参数赋值
            UnifiedParamsSet(reqParams, payOrder, GetNotifyUrl(), GetReturnUrl());

            // 发送请求
            string method = "ysepay.online.qrcodepay", repMethod = "ysepay_online_qrcodepay_response";
            JObject resJSON = PackageParamAndReq(YsePayConfig.QRCODE_GATEWAY, method, repMethod, reqParams, GetNotifyUrl(), logPrefix, mchAppConfigContext);
            //请求 & 响应成功， 判断业务逻辑
            var data = resJSON.GetValue(repMethod)?.ToObject<JObject>();
            string code = data?.GetValue("code").ToString();
            string msg = data?.GetValue("msg").ToString();
            string subCode = data?.GetValue("sub_code").ToString();
            string subMsg = data?.GetValue("sub_msg").ToString();
            channelRetMsg.ChannelMchNo = string.Empty;
            try
            {
                if ("10000".Equals(code))
                {
                    data.TryGetString("trade_no", out string yseTradeNo);//银盛支付交易流水号
                    data.TryGetString("channel_send_sn", out string channelSendSn);//发往渠道流水号
                    string _transStat = data.GetValue("trade_status").ToString();
                    string tradeStatus = data.GetValue("trade_status").ToString();
                    var transStat = YsePayEnum.ConvertTradeStatus(tradeStatus);
                    switch (transStat)
                    {
                        case YsePayEnum.TradeStatus.WAIT_BUYER_PAY:
                        case YsePayEnum.TradeStatus.TRADE_PROCESS:
                        case YsePayEnum.TradeStatus.TRADE_ABNORMALITY:
                        case YsePayEnum.TradeStatus.TRADE_SUCCESS:
                            string qrCode = data.GetValue("qr_code_url").ToString();
                            //二维码地址
                            if (CS.PAY_DATA_TYPE.CODE_IMG_URL.Equals(bizRQ.PayDataType))
                            {
                                res.CodeImgUrl = _sysConfigService.GetDBApplicationConfig().GenScanImgUrl(qrCode);
                            }
                            else
                            {
                                //默认都为跳转地址方式
                                res.CodeUrl = qrCode;
                            }
                            channelRetMsg.ChannelOrderId = yseTradeNo;
                            channelRetMsg.PlatformMchOrderId = channelSendSn;
                            channelRetMsg.ChannelState = ChannelState.WAITING;
                            break;
                        case YsePayEnum.TradeStatus.TRADE_FAILD:
                        case YsePayEnum.TradeStatus.TRADE_FAILED:
                            channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                            channelRetMsg.ChannelErrCode = subCode;
                            channelRetMsg.ChannelErrMsg = subMsg;
                            break;
                    }
                }
                else
                {
                    channelRetMsg.ChannelState = ChannelState.WAITING;
                    channelRetMsg.IsNeedQuery = true; // 开启轮询查单
                }
            }
            catch (Exception)
            {
                channelRetMsg.ChannelErrCode = subCode ?? code;
                channelRetMsg.ChannelErrMsg = subMsg ?? msg;
            }
            return res;
        }

        public override string PreCheck(UnifiedOrderRQ rq, PayOrderDto payOrder)
        {
            return null;
        }
    }
}

﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Params.YsePay;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Channel.YsePay.Enumerator;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.YsePay
{
    /// <summary>
    /// 银盛查单
    /// </summary>
    public class YsePayPayOrderQueryService : IPayOrderQueryService
    {
        private readonly ILogger<YsePayPayOrderQueryService> _logger;
        private readonly YsePayPaymentService ysePayPaymentService;

        public YsePayPayOrderQueryService(ILogger<YsePayPayOrderQueryService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            this.ysePayPaymentService = ActivatorUtilities.CreateInstance<YsePayPaymentService>(serviceProvider);
        }

        public string GetIfCode()
        {
            return CS.IF_CODE.YSEPAY;
        }

        public ChannelRetMsg Query(PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            SortedDictionary<string, string> reqParams = new SortedDictionary<string, string>();
            string logPrefix = $"【银盛({payOrder.WayCode})查单】";

            try
            {
                reqParams.Add("org_req_date", payOrder.CreatedAt.Value.ToString("yyyyMMdd")); //订单号
                reqParams.Add("org_req_seq_id", payOrder.PayOrderId); //订单号

                //封装公共参数 & 签名 & 调起http请求 & 返回响应数据并包装为json格式。
                string method = "ysepay.online.barcodepay", repMethod = "ysepay_online_barcodepay_response";
                JObject resJSON = ysePayPaymentService.PackageParamAndReq(YsePayConfig.SEARCH_GATEWAY, method, repMethod, reqParams, string.Empty, logPrefix, mchAppConfigContext);
                _logger.LogInformation($"查询订单 payorderId:{payOrder.PayOrderId}, 返回结果:{resJSON}");
                if (resJSON == null)
                {
                    return ChannelRetMsg.Waiting(); //支付中
                }
                var channelRetMsg = ChannelRetMsg.Waiting();
                //请求 & 响应成功， 判断业务逻辑
                var data = resJSON.GetValue(repMethod)?.ToObject<JObject>();
                string code = data?.GetValue("code").ToString();
                string msg = data?.GetValue("msg").ToString();
                string subCode = data?.GetValue("sub_code").ToString();
                string subMsg = data?.GetValue("sub_msg").ToString();
                channelRetMsg.ChannelMchNo = string.Empty;
                if ("10000".Equals(code))
                {
                    data.TryGetString("trade_no", out string tradeNo);//银盛支付交易流水号
                    data.TryGetString("channel_recv_sn", out string channelRecvSn);//渠道返回流水号	
                    data.TryGetString("channel_send_sn", out string channelSendSn);//发往渠道流水号
                    /*买家用户号
                    支付宝渠道：买家支付宝用户号buyer_user_id
                    微信渠道：微信平台的sub_openid*/
                    data.TryGetString("buyer_user_id", out string buyerUserId);
                    data.TryGetString("openid", out string openid);
                    string tradeStatus = data.GetValue("trade_status").ToString();
                    var transStat = YsePayEnum.ConvertTradeStatus(tradeStatus);
                    switch (transStat)
                    {
                        case YsePayEnum.TradeStatus.WAIT_BUYER_PAY:
                        case YsePayEnum.TradeStatus.TRADE_PROCESS:
                        case YsePayEnum.TradeStatus.TRADE_ABNORMALITY:
                        case YsePayEnum.TradeStatus.TRADE_SUCCESS:
                            channelRetMsg = ChannelRetMsg.ConfirmSuccess(tradeNo);  //支付成功
                            channelRetMsg.ChannelMchNo = string.Empty;
                            channelRetMsg.ChannelOrderId = tradeNo;
                            channelRetMsg.ChannelUserId = openid ?? buyerUserId;
                            channelRetMsg.PlatformOrderId = channelRecvSn;
                            channelRetMsg.PlatformMchOrderId = channelSendSn;
                            break;
                        case YsePayEnum.TradeStatus.TRADE_FAILD:
                        case YsePayEnum.TradeStatus.TRADE_FAILED:
                            channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                            channelRetMsg.ChannelErrCode = subCode;
                            channelRetMsg.ChannelErrMsg = subMsg;
                            break;
                    }
                }
                else if ("50000".Equals(code) || "3501".Equals(code))
                {
                    channelRetMsg.ChannelState = ChannelState.WAITING;
                }
                else
                {
                    channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;
                    channelRetMsg.ChannelErrCode = subCode ?? code;
                    channelRetMsg.ChannelErrMsg = subMsg ?? msg;
                }
                return channelRetMsg; //支付中
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"查询订单 payorderId:{payOrder.PayOrderId}, 异常:{e.Message}");
                return ChannelRetMsg.Waiting(); //支付中
            }
        }
    }
}

﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Enumerator;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Payment.Api.Channel.AliPay.Kits;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay;
using AGooday.AgPay.Payment.Api.Services;
using AGooday.AgPay.Payment.Api.Utils;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Response;

namespace AGooday.AgPay.Payment.Api.Channel.AliPay.PayWay
{
    public class AliQr : AliPayPaymentService
    {
        /// <summary>
        /// 支付宝 二维码支付
        /// </summary>
        /// <param name="serviceProvider"></param>
        public AliQr(ILogger<AliQr> logger, 
            IServiceProvider serviceProvider,
            ISysConfigService sysConfigService,
            ConfigContextQueryService configContextQueryService)
            : base(logger, serviceProvider, sysConfigService, configContextQueryService)
        {
        }

        public override AbstractRS Pay(UnifiedOrderRQ rq, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            AliQrOrderRQ bizRQ = (AliQrOrderRQ)rq;

            AlipayTradePrecreateRequest req = new AlipayTradePrecreateRequest();
            AlipayTradePrecreateModel model = new AlipayTradePrecreateModel();
            model.OutTradeNo = payOrder.PayOrderId;
            model.Subject = payOrder.Subject; //订单标题
            model.Body = payOrder.Body; //订单描述信息
            model.TotalAmount = AmountUtil.ConvertCent2Dollar(payOrder.Amount);  //支付金额
            req.SetNotifyUrl(GetNotifyUrl()); // 设置异步通知地址
            req.SetBizModel(model);

            //统一放置 isv接口必传信息
            AliPayKit.PutApiIsvInfo(mchAppConfigContext, req, model);

            //调起支付宝 （如果异常， 将直接跑出   ChannelException ）
            AlipayTradePrecreateResponse alipayResp = _configContextQueryService.GetAlipayClientWrapper(mchAppConfigContext).Execute(req);

            // 构造函数响应数据
            AliQrOrderRS res = ApiResBuilder.BuildSuccess<AliQrOrderRS>();
            ChannelRetMsg channelRetMsg = new ChannelRetMsg();
            res.ChannelRetMsg = channelRetMsg;

            //放置 响应数据
            channelRetMsg.ChannelAttach = alipayResp.Body;

            // ↓↓↓↓↓↓ 调起接口成功后业务判断务必谨慎！！ 避免因代码编写bug，导致不能正确返回订单状态信息  ↓↓↓↓↓↓

            //当条码重复发起时，支付宝返回的code = 10003, subCode = null [等待用户支付], 此时需要特殊判断 = = 。
            //处理成功
            if (!alipayResp.IsError)
            {
                //二维码地址
                if (CS.PAY_DATA_TYPE.CODE_IMG_URL.Equals(bizRQ.PayDataType))
                {
                    res.CodeImgUrl = _sysConfigService.GetDBApplicationConfig().GenScanImgUrl(alipayResp.QrCode);
                }
                else
                { 
                    //默认都为跳转地址方式
                    res.CodeUrl = alipayResp.QrCode;
                }

                channelRetMsg.ChannelState = ChannelState.WAITING;
            }
            //其他状态, 表示下单失败
            else
            {
                channelRetMsg.ChannelState = ChannelState.CONFIRM_FAIL;  //支付失败
                channelRetMsg.ChannelErrCode = AliPayKit.AppendErrCode(alipayResp.Code, alipayResp.SubCode);
                channelRetMsg.ChannelErrMsg = AliPayKit.AppendErrMsg(alipayResp.Msg, alipayResp.SubMsg);
            }
            return res;
        }

        public override string PreCheck(UnifiedOrderRQ rq, PayOrderDto payOrder)
        {
            return null;
        }
    }
}

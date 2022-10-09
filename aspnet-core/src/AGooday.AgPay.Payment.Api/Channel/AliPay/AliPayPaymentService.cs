﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS;
using AGooday.AgPay.Payment.Api.RQRS.PayOrder;
using AGooday.AgPay.Payment.Api.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace AGooday.AgPay.Payment.Api.Channel.AliPay
{
    public class AliPayPaymentService : AbstractPaymentService
    {
        public AliPayPaymentService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override string GetIfCode()
        {
            return CS.IF_CODE.ALIPAY;
        }

        public override bool IsSupport(string wayCode)
        {
            return true;
        }

        public override AbstractRS Pay(UnifiedOrderRQ bizRQ, PayOrderDto payOrder, MchAppConfigContext mchAppConfigContext)
        {
            return PayWayUtil.GetRealPaywayService(_serviceProvider, payOrder.WayCode).Pay(bizRQ, payOrder, mchAppConfigContext);
        }

        public override string PreCheck(UnifiedOrderRQ bizRQ, PayOrderDto payOrder)
        {
            return PayWayUtil.GetRealPaywayService(_serviceProvider, payOrder.WayCode).PreCheck(bizRQ, payOrder);
        }
    }
}
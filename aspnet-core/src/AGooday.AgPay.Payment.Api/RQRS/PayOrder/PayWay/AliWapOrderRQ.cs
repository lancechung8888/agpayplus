﻿using AGooday.AgPay.Common.Constants;
using System.ComponentModel.DataAnnotations;

namespace AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay
{
    /// <summary>
    /// 支付方式： ALI_WAP
    /// </summary>
    public class AliWapOrderRQ : CommonPayDataRQ
    {
        /** 构造函数 **/
        public AliWapOrderRQ()
        {
            this.WayCode = CS.PAY_WAY_CODE.ALI_WAP;//默认 ALI_WAP, 避免validate出现问题
        }
    }
}

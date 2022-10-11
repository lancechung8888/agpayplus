﻿using AGooday.AgPay.Common.Constants;
using Newtonsoft.Json;
using System.Drawing.Drawing2D;
using System.Security.Cryptography.Xml;

namespace AGooday.AgPay.Payment.Api.RQRS.PayOrder.PayWay
{
    /// <summary>
    /// 支付方式： YSF_JSAPI
    /// </summary>
    public class YsfJsapiOrderRS : UnifiedOrderRS
    {
        /// <summary>
        /// 调起支付插件的云闪付订单号
        /// </summary>
        public string RedirectUrl { get; set; }

        public override string BuildPayDataType()
        {
            return CS.PAY_DATA_TYPE.YSF_APP;
        }

        public override string BuildPayData()
        {
            return JsonConvert.SerializeObject(new { redirectUrl = RedirectUrl });
        }
    }
}

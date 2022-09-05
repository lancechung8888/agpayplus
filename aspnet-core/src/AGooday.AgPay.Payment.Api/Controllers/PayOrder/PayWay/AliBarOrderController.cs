﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AGooday.AgPay.Payment.Api.Controllers.PayOrder.PayWay
{
    /// <summary>
    /// 支付宝 条码支付
    /// </summary>
    [ApiController]
    public class AliBarOrderController : AbstractPayOrderController
    {
        /// <summary>
        /// 统一下单接口
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/pay/aliBarOrder")]
        public ActionResult AliBarOrder()
        {
            // 统一下单接口;
            // "ALI_BAR";  //支付宝条码支付
            return UnifiedOrder("ALI_BAR", null);
        }
    }
}

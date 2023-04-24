﻿using AGooday.AgPay.Application.DataTransfer;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Application.Interfaces
{
    public interface IPayRateConfigService
    {
        public JObject GetByInfoIdAndIfCodeJson(string configMode, string infoId, string ifCode);
        bool SaveOrUpdate(PayRateConfigSaveDto dto);
    }
}

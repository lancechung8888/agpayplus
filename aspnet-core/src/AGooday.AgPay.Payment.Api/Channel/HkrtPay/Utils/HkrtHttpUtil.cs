﻿using AGooday.AgPay.Common.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Payment.Api.Channel.HkrtPay.Utils
{
    public class HkrtHttpUtil
    {
        private static readonly string DEFAULT_CHARSET = "UTF-8";
        private static readonly int DEFAULT_TIMEOUT = 60; // 60 秒超时

        public static string DoPost(string url, JObject reqParams)
        {
            var client = new AgHttpClient(DEFAULT_TIMEOUT, DEFAULT_CHARSET);
            var request = new AgHttpClient.Request()
            {
                Url = url,
                Method = "POST",
                Content = JsonConvert.SerializeObject(reqParams),
                ContentType = "application/x-www-form-urlencoded"
            };
            var response = client.Send(request);
            string result = response.Content;
            return result;
        }
    }
}
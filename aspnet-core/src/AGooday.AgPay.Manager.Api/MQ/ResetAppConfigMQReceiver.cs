﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Components.MQ.Models;
using Newtonsoft.Json;

namespace AGooday.AgPay.Manager.Api.MQ
{
    /// <summary>
    /// 接收MQ消息
    /// 业务：更新系统配置参数
    /// </summary>
    public class ResetAppConfigMQReceiver : ResetAppConfigMQ.IMQReceiver
    {
        private readonly ILogger<ResetAppConfigMQReceiver> _logger;
        private readonly IServiceProvider provider;

        public ResetAppConfigMQReceiver(ILogger<ResetAppConfigMQReceiver> logger, IServiceProvider provider)
        {
            _logger = logger;
            this.provider = provider;
        }

        public void Receive(ResetAppConfigMQ.MsgPayload payload)
        {
            using (var scope = provider.CreateScope())
            {
                var sysConfigService = scope.ServiceProvider.GetRequiredService<ISysConfigService>();
                _logger.LogInformation("成功接收更新系统配置的订阅通知, Msg={Msg}", JsonConvert.SerializeObject(payload));
                sysConfigService.InitDBConfig(payload.GroupKey);
                _logger.LogInformation("系统配置静态属性已重置");
            }
        }
    }
}

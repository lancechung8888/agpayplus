﻿using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Components.MQ.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace AGooday.AgPay.Merchant.Api.MQ
{
    /// <summary>
    /// 接收MQ消息
    /// 业务：清除商户登录信息
    /// </summary>
    public class CleanMchLoginAuthCacheMQReceiver : CleanMchLoginAuthCacheMQ.IMQReceiver
    {
        private readonly ILogger<CleanMchLoginAuthCacheMQReceiver> _logger;
        private readonly int defaultDB;
        private readonly IDatabase redis;
        private readonly IServer redisServer;

        public CleanMchLoginAuthCacheMQReceiver(ILogger<CleanMchLoginAuthCacheMQReceiver> logger, RedisUtil client)
        {
            _logger = logger;
            defaultDB = client.GetDefaultDB();
            redis = client.GetDatabase();
            redisServer = client.GetServer();
        }

        public void Receive(CleanMchLoginAuthCacheMQ.MsgPayload payload)
        {
            _logger.LogInformation("成功接收删除商户用户登录的订阅通知, Msg={Msg}", JsonConvert.SerializeObject(payload));
            // 字符串转List<Long>
            List<long> userIdList = payload.UserIdList;
            // 删除redis用户缓存
            if (userIdList == null || userIdList.Count == 0)
            {
                _logger.LogInformation("用户ID为空");
                return;
            }
            foreach (long sysUserId in userIdList)
            {
                var cacheKeyList = redisServer.Keys(defaultDB, CS.GetCacheKeyToken(sysUserId, "*"));
                if (cacheKeyList == null || !cacheKeyList.Any())
                {
                    continue;
                }
                foreach (string cacheKey in cacheKeyList)
                {
                    // 删除用户Redis信息
                    redis.KeyDelete(cacheKey);
                    continue;
                }
            }
            _logger.LogInformation("无权限登录用户信息已清除");
        }
    }
}

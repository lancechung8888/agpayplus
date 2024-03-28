﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Enumerator;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Payment.Api.Channel;
using AGooday.AgPay.Payment.Api.Models;
using AGooday.AgPay.Payment.Api.RQRS.Msg;
using AGooday.AgPay.Payment.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AGooday.AgPay.Payment.Api.Controllers.Transfer
{
    public class TransferNoticeController : Controller
    {
        private readonly ILogger<TransferNoticeController> _logger;
        protected readonly Func<string, ITransferNoticeService> _transferNoticeServiceFactory;
        private readonly ITransferOrderService _transferOrderService;
        private readonly PayMchNotifyService _payMchNotifyService;
        private readonly ConfigContextQueryService _configContextQueryService;

        public TransferNoticeController(ILogger<TransferNoticeController> logger,
            Func<string, ITransferNoticeService> transferNoticeServiceFactory,
            ITransferOrderService transferOrderService,
            ConfigContextQueryService configContextQueryService,
            PayMchNotifyService payMchNotifyService)
        {
            _logger = logger;
            _transferNoticeServiceFactory = transferNoticeServiceFactory;
            _transferOrderService = transferOrderService;
            _configContextQueryService = configContextQueryService;
            _payMchNotifyService = payMchNotifyService;
        }

        [HttpPost]
        [Route("/api/transfer/notify/{ifCode}")]
        [Route("/api/transfer/notify/{ifCode}/{transferId}")]
        public ActionResult DoNotify(string ifCode, string transferId)
        {
            string urlOrderId = transferId;
            string logPrefix = $"进入[{ifCode}]转账回调：urlOrderId：[{urlOrderId}]";
            _logger.LogInformation($"===== {logPrefix} =====");

            try
            {
                // 参数有误
                if (string.IsNullOrEmpty(ifCode))
                {
                    return BadRequest("ifCode is empty");
                }

                // 查询转账接口是否存在
                ITransferNoticeService transferNotifyService = _transferNoticeServiceFactory(ifCode);

                // 支付通道转账接口实现不存在
                if (transferNotifyService == null)
                {
                    _logger.LogError($"{logPrefix}, transfer interface not exists");
                    return BadRequest($"[{ifCode}] transfer interface not exists");
                }

                // 解析转账单号和请求参数
                Dictionary<string, object> mutablePair = transferNotifyService.ParseParams(Request, urlOrderId);
                if (mutablePair == null)
                {
                    _logger.LogError($"{logPrefix}, mutablePair is null");
                    throw new BizException("解析数据异常！"); // 需要实现类自行抛出ResponseException, 不应该在这抛此异常。
                }

                // 解析到转账单号
                transferId = mutablePair.First().Key;
                _logger.LogInformation($"{logPrefix}, 解析数据为：transferId:{transferId}, params:{mutablePair.First().Key}");

                // 获取转账单号和转账单数据
                TransferOrderDto transferOrder = _transferOrderService.GetById(transferId);

                // 转账单不存在
                if (transferOrder == null)
                {
                    _logger.LogError($"{logPrefix}, 转账单不存在. transferId={transferId}");
                    return transferNotifyService.DoNotifyOrderNotExists(Request);
                }

                // 查询出商户应用的配置信息
                MchAppConfigContext mchAppConfigContext = _configContextQueryService.QueryMchInfoAndAppInfo(transferOrder.MchNo, transferOrder.AppId);

                // 调起接口的回调判断
                ChannelRetMsg notifyResult = transferNotifyService.DoNotice(Request, mutablePair.First().Value, transferOrder, mchAppConfigContext);

                // 返回null表明出现异常，无需处理通知下游等操作。
                if (notifyResult == null || notifyResult.ChannelState == null || notifyResult.ResponseEntity == null)
                {
                    _logger.LogError($"{logPrefix}, 处理回调事件异常  notifyResult data error, notifyResult = {notifyResult}");
                    throw new BizException("处理回调事件异常！"); // 需要实现类自行抛出ResponseException, 不应该在这抛此异常。
                }

                // 转账单是【转账中状态】
                if (transferOrder.State == (byte)TransferOrderState.STATE_ING)
                {
                    if (notifyResult.ChannelState == ChannelState.CONFIRM_SUCCESS)
                    {
                        // 转账成功
                        _transferOrderService.UpdateIng2Success(transferId, notifyResult.ChannelOrderId);
                        _payMchNotifyService.TransferOrderNotify(_transferOrderService.GetById(transferId));
                    }
                    else if (notifyResult.ChannelState == ChannelState.CONFIRM_FAIL)
                    {
                        // 转账失败
                        _transferOrderService.UpdateIng2Fail(transferId, notifyResult.ChannelOrderId, notifyResult.ChannelUserId, notifyResult.ChannelErrCode);
                        _payMchNotifyService.TransferOrderNotify(_transferOrderService.GetById(transferId));
                    }
                }

                _logger.LogInformation($"===== {logPrefix}, 转账单通知完成。 transferId={transferId}, parseState = {notifyResult.ChannelState} =====");

                return notifyResult.ResponseEntity;
            }
            catch (BizException e)
            {
                _logger.LogError(e, $"{logPrefix}, transferId={transferId}, BizException");
                return BadRequest(e.Message);
            }
            catch (ResponseException e)
            {
                _logger.LogError(e, $"{logPrefix}, transferId={transferId}, ResponseException");
                return e.ResponseEntity;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{logPrefix}, transferId={transferId}, 系统异常");
                return BadRequest(e.Message);
            }
        }
    }
}

﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Models;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Application.Interfaces
{
    public interface ITransferOrderService : IAgPayService<TransferOrderDto>
    {
        /// <summary>
        /// 查询商户订单
        /// </summary>
        /// <param name="mchNo"></param>
        /// <param name="mchOrderNo"></param>
        /// <param name="transferId"></param>
        /// <returns></returns>
        TransferOrderDto QueryMchOrder(string mchNo, string mchOrderNo, string transferId);
        PaginatedList<TransferOrderDto> GetPaginatedData(TransferOrderQueryDto dto);
        JObject Statistics(TransferOrderQueryDto dto);
        /// <summary>
        /// 更新转账订单状态 【转账订单生成】 --》 【转账中】
        /// </summary>
        /// <param name="transferId"></param>
        /// <returns></returns>
        bool UpdateInit2Ing(string transferId);
        /// <summary>
        /// 更新转账订单状态 【转账中】 --》 【转账成功】
        /// </summary>
        /// <param name="transferId"></param>
        /// <param name="channelOrderNo"></param>
        /// <returns></returns>
        bool UpdateIng2Success(string transferId, string channelOrderNo);
        /// <summary>
        /// 更新转账订单状态 【转账中】 --》 【转账失败】
        /// </summary>
        /// <param name="transferId"></param>
        /// <param name="channelOrderNo"></param>
        /// <param name="channelErrCode"></param>
        /// <param name="channelErrMsg"></param>
        /// <returns></returns>
        bool UpdateIng2Fail(string transferId, string channelOrderNo, string channelErrCode, string channelErrMsg);
        /// <summary>
        /// 更新转账订单状态 【转账中】 --》 【转账成功/转账失败】
        /// </summary>
        /// <param name="transferId"></param>
        /// <param name="state"></param>
        /// <param name="channelOrderId"></param>
        /// <param name="channelErrCode"></param>
        /// <param name="channelErrMsg"></param>
        /// <returns></returns>
        bool UpdateIng2SuccessOrFail(string transferId, byte updateState, string channelOrderNo, string channelErrCode, string channelErrMsg);
        bool IsExistOrderByMchOrderNo(string mchNo, string mchOrderNo);
    }
}

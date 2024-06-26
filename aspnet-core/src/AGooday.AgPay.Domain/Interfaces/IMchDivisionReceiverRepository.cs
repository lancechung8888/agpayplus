﻿using AGooday.AgPay.Domain.Models;

namespace AGooday.AgPay.Domain.Interfaces
{
    public interface IMchDivisionReceiverRepository : IAgPayRepository<MchDivisionReceiver, long>
    {
        bool IsExistUseReceiverGroup(long receiverGroupId);
    }
}

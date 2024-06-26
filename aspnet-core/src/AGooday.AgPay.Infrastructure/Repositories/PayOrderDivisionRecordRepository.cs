﻿using AGooday.AgPay.Common.Enumerator;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AGooday.AgPay.Infrastructure.Context;

namespace AGooday.AgPay.Infrastructure.Repositories
{
    public class PayOrderDivisionRecordRepository : AgPayRepository<PayOrderDivisionRecord, long>, IPayOrderDivisionRecordRepository
    {
        public PayOrderDivisionRecordRepository(AgPayDbContext context)
            : base(context)
        {
        }

        public long SumSuccessDivisionAmount(string payOrderId)
        {
            return DbSet.Where(w => w.PayOrderId.Equals(payOrderId) && w.State.Equals((byte)PayOrderDivisionRecordState.STATE_SUCCESS))
                .Sum(s => s.CalDivisionAmount);
        }
    }
}

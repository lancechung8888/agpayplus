﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AGooday.AgPay.Application.Services
{
    public class MchStoreService : IMchStoreService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly IMchStoreRepository _mchStoreRepository;
        private readonly IMchInfoRepository _mchInfoRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public MchStoreService(IMchStoreRepository mchStoreRepository, IMchInfoRepository mchInfoRepository, IMapper mapper, IMediatorHandler bus)
        {
            _mchStoreRepository = mchStoreRepository;
            _mchInfoRepository = mchInfoRepository;
            _mapper = mapper;
            Bus = bus;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool Add(MchStoreDto dto)
        {
            var m = _mapper.Map<MchStore>(dto);
            _mchStoreRepository.Add(m);
            return _mchStoreRepository.SaveChanges(out int _);
        }

        public bool Remove(long recordId)
        {
            _mchStoreRepository.Remove(recordId);
            return _mchStoreRepository.SaveChanges(out _);
        }

        public bool Update(MchStoreDto dto)
        {
            var renew = _mapper.Map<MchStore>(dto);
            //var old = _mchStoreRepository.GetById(dto.StoreId);
            renew.UpdatedAt = DateTime.Now;
            _mchStoreRepository.Update(renew);
            return _mchStoreRepository.SaveChanges(out int _);
        }

        public MchStoreDto GetById(long recordId)
        {
            var entity = _mchStoreRepository.GetById(recordId);
            var dto = _mapper.Map<MchStoreDto>(entity);
            return dto;
        }
        public MchStoreDto GetById(long recordId, string mchNo)
        {
            var entity = _mchStoreRepository.GetAll().Where(w => w.MchNo.Equals(mchNo) && w.StoreId.Equals(recordId)).FirstOrDefault();
            return _mapper.Map<MchStoreDto>(entity);
        }
        public IEnumerable<MchStoreDto> GetAll()
        {
            var mchStores = _mchStoreRepository.GetAll();
            return _mapper.Map<IEnumerable<MchStoreDto>>(mchStores);
        }

        public PaginatedList<MchStoreListDto> GetPaginatedData(MchStoreQueryDto dto, string agentNo = null)
        {
            var mchStores = _mchStoreRepository.GetAll()
                .Join(_mchInfoRepository.GetAll(),
                ms => ms.MchNo, mi => mi.MchNo,
                (ms, mi) => new { ms, mi })
                .Where(w => (string.IsNullOrWhiteSpace(dto.MchNo) || w.ms.MchNo.Equals(dto.MchNo))
                && (dto.StoreId.Equals(0) || w.ms.StoreId.Equals(dto.StoreId))
                && (string.IsNullOrWhiteSpace(dto.StoreName) || w.ms.StoreName.Contains(dto.StoreName))
                && (string.IsNullOrWhiteSpace(agentNo) || w.mi.AgentNo.Equals(agentNo))
                ).AsNoTracking().ToList().Select(s =>
                {
                    var item = _mapper.Map<MchStoreListDto>(s.ms);
                    item.MchName = s.mi.MchName;
                    return item;
                }).OrderByDescending(o => o.CreatedAt);

            var records = PaginatedList<MchStoreListDto>.Create(mchStores, dto.PageNumber, dto.PageSize);
            return records;
        }
    }
}
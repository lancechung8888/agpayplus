﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AGooday.AgPay.Application.Services
{
    /// <summary>
    /// 商户支付通道表 服务实现类
    /// </summary>
    public class MchPayPassageService : IMchPayPassageService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly IPayRateConfigService _payRateConfigService;

        private readonly IMchPayPassageRepository _mchPayPassageRepository;
        private readonly IPayInterfaceDefineRepository _payInterfaceDefineRepository;
        private readonly IPayInterfaceConfigRepository _payInterfaceConfigRepository;
        private readonly IPayRateConfigRepository _payRateConfigRepository;
        private readonly IPayRateLevelConfigRepository _payRateLevelConfigRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public MchPayPassageService(IMapper mapper, IMediatorHandler bus,
            IPayRateConfigService payRateConfigService,
            IMchPayPassageRepository mchPayPassageRepository,
            IPayInterfaceDefineRepository payInterfaceDefineRepository,
            IPayInterfaceConfigRepository payInterfaceConfigRepository,
            IPayRateConfigRepository payRateConfigRepository, 
            IPayRateLevelConfigRepository payRateLevelConfigRepository)
        {
            _mapper = mapper;
            Bus = bus;
            _payRateConfigService = payRateConfigService;
            _mchPayPassageRepository = mchPayPassageRepository;
            _payInterfaceDefineRepository = payInterfaceDefineRepository;
            _payInterfaceConfigRepository = payInterfaceConfigRepository;
            _payRateConfigRepository = payRateConfigRepository;
            _payRateLevelConfigRepository = payRateLevelConfigRepository;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Add(MchPayPassageDto dto)
        {
            var m = _mapper.Map<MchPayPassage>(dto);
            _mchPayPassageRepository.Add(m);
            _mchPayPassageRepository.SaveChanges();
        }

        public void Remove(long recordId)
        {
            _mchPayPassageRepository.Remove(recordId);
            _mchPayPassageRepository.SaveChanges();
        }

        public void Update(MchPayPassageDto dto)
        {
            var m = _mapper.Map<MchPayPassage>(dto);
            _mchPayPassageRepository.Update(m);
            _mchPayPassageRepository.SaveChanges();
        }

        public MchPayPassageDto GetById(long recordId)
        {
            var entity = _mchPayPassageRepository.GetById(recordId);
            var dto = _mapper.Map<MchPayPassageDto>(entity);
            return dto;
        }

        public IEnumerable<MchPayPassageDto> GetMchPayPassageByAppId(string mchNo, string appId)
        {
            var mchPayPassages = _mchPayPassageRepository.GetAll()
                .Where(w => w.MchNo.Equals(mchNo) && w.AppId.Equals(appId) && w.State.Equals(CS.PUB_USABLE));
            return _mapper.Map<IEnumerable<MchPayPassageDto>>(mchPayPassages);
        }

        public IEnumerable<MchPayPassageDto> GetAll()
        {
            var mchPayPassages = _mchPayPassageRepository.GetAll();
            return _mapper.Map<IEnumerable<MchPayPassageDto>>(mchPayPassages);
        }

        public IEnumerable<MchPayPassageDto> GetByAppId(string appId, List<string> wayCodes)
        {
            var mchPayPassages = _mchPayPassageRepository.GetAll().Where(w => w.AppId.Equals(appId)
            && (wayCodes.Count.Equals(0) || wayCodes.Contains(w.WayCode)));
            return _mapper.Map<IEnumerable<MchPayPassageDto>>(mchPayPassages);
        }

        public PaginatedList<AvailablePayInterfaceDto> SelectAvailablePayInterfaceList(string wayCode, string appId, string infoType, byte mchType, int pageNumber, int pageSize)
        {
            var result = SelectAvailablePayInterfaceList(wayCode, appId, infoType, mchType);
            var records = PaginatedList<AvailablePayInterfaceDto>.Create(result, pageNumber, pageSize);
            return records;
        }

        /// <summary>
        /// 根据支付方式查询可用的支付接口列表
        /// </summary>
        /// <param name="wayCode"></param>
        /// <param name="appId"></param>
        /// <param name="infoType"></param>
        /// <param name="mchType"></param>
        /// <returns></returns>
        public IEnumerable<AvailablePayInterfaceDto> SelectAvailablePayInterfaceList(string wayCode, string appId, string infoType, byte mchType)
        {
            //var result = _payInterfaceDefineRepository.GetAll()
            //    .Join(_payInterfaceDefineRepository.GetAll<PayInterfaceConfig>(),
            //    pid => pid.IfCode, pic => pic.IfCode,
            //    (pid, pic) => new { pid, pic })
            //    .Where(w => w.pid.State.Equals(CS.YES) && w.pic.State.Equals(CS.YES)
            //    && EF.Functions.JsonContains(w.pid.WayCodes, new { wayCode = wayCode })//&& w.pid.WayCodes.Contains(wayCode) 
            //    && w.pic.InfoType.Equals(infoType) && w.pic.InfoId.Equals(appId)
            //    && ((mchType.Equals(CS.MCH_TYPE_NORMAL) && w.pid.IsMchMode.Equals(CS.YES)) || (mchType.Equals(CS.MCH_TYPE_ISVSUB) && w.pid.IsIsvMode.Equals(CS.YES)))
            //    && !string.IsNullOrWhiteSpace(w.pic.IfParams.Trim()))
            //    .Select(s => new AvailablePayInterfaceDto()
            //    {
            //        IfCode = s.pid.IfCode,
            //        IfName = s.pid.IfName,
            //        ConfigPageType = s.pid.ConfigPageType,
            //        Icon = s.pid.Icon,
            //        BgColor = s.pid.BgColor,
            //        IfParams = s.pic.IfParams,
            //        IfRate = s.pic.IfRate * 100,
            //    });
            var configType = CS.CONFIG_TYPE.MCHRATE;
            var payRateConfigs = _payRateConfigRepository.GetByInfoId(configType, infoType, appId);
            var ifCodes = payRateConfigs.Where(w => w.WayCode.Equals(wayCode)).Select(s => s.IfCode).Distinct().ToList();
            var result = _payInterfaceDefineRepository.SelectAvailablePayInterfaceList<AvailablePayInterfaceDto>(wayCode, appId, infoType, mchType)
                .Where(w => ifCodes.Contains(w.IfCode));

            if (result != null)
            {
                var mchPayPassages = _mchPayPassageRepository.GetAll().Where(w => w.AppId.Equals(appId) && w.WayCode.Equals(wayCode));
                foreach (var item in result)
                {
                    item.IfRate = item.IfRate ?? item.IfRate * 100; 
                    item.PayWayFee = _payRateConfigService.GetPayRateConfigItem(configType, infoType, appId, item.IfCode, wayCode);
                    var payPassage = mchPayPassages.Where(w => w.IfCode.Equals(item.IfCode)).FirstOrDefault();
                    if (payPassage != null)
                    {
                        item.PassageId = payPassage.Id;
                        item.State = (sbyte)payPassage.State;
                        item.Rate = payPassage.Rate * 100;
                    }
                }
            }
            return result;
        }

        public void SetMchPassage(string mchNo, string appId, string wayCode, string ifCode, byte state)
        {
            var mchPayPassages = _mchPayPassageRepository.GetAll()
                .Where(w => w.MchNo.Equals(mchNo) && w.AppId.Equals(appId) && w.WayCode.Equals(wayCode));
            var mchPayPassage = mchPayPassages.FirstOrDefault(w => w.IfCode.Equals(ifCode));
            if (mchPayPassage == null)
            {
                mchPayPassage = new MchPayPassage()
                {
                    MchNo = mchNo,
                    AppId = appId,
                    IfCode = ifCode,
                    WayCode = wayCode,
                    Rate = 0,
                    State = state,
                    CreatedAt = DateTime.Now,
                };
                _mchPayPassageRepository.Add(mchPayPassage);
            }
            else
            {
                mchPayPassage.State = state;
                _mchPayPassageRepository.Update(mchPayPassage);
            }
            foreach (var item in mchPayPassages.Where(w => !w.IfCode.Equals(ifCode)))
            {
                item.State = state.Equals(CS.YES) ? CS.NO : CS.YES;
                _mchPayPassageRepository.Update(item);
            }
            _mchPayPassageRepository.SaveChanges();
        }

        public void SaveOrUpdateBatchSelf(List<MchPayPassageDto> mchPayPassages, string mchNo)
        {
            var _smchPayPassages = _mchPayPassageRepository.GetAll().AsNoTracking()
                .Where(w => w.MchNo.Equals(mchNo) && mchPayPassages.Select(s => s.Id).Contains(w.Id));
            foreach (var payPassage in mchPayPassages)
            {
                if (payPassage.State == CS.NO && payPassage.Id == null)
                {
                    continue;
                }
                // 商户系统配置通道，添加商户号参数
                if (!string.IsNullOrWhiteSpace(mchNo))
                {
                    payPassage.MchNo = mchNo;
                }
                payPassage.Rate = payPassage.Rate / 100;
                var _payPassage = _smchPayPassages.Where(w => w.Id.Equals(payPassage.Id)).FirstOrDefault();
                payPassage.CreatedAt = _payPassage?.CreatedAt ?? DateTime.Now;
                payPassage.UpdatedAt = payPassage.UpdatedAt ?? DateTime.Now;
                var m = _mapper.Map<MchPayPassage>(payPassage);
                _mchPayPassageRepository.SaveOrUpdate(m, payPassage.Id);
            }
            _mchPayPassageRepository.SaveChanges();
        }

        /// <summary>
        /// 根据应用ID 和 支付方式， 查询出商户可用的支付接口
        /// </summary>
        /// <param name="mchNo"></param>
        /// <param name="appId"></param>
        /// <param name="wayCode"></param>
        /// <returns></returns>
        public MchPayPassageDto FindMchPayPassage(string mchNo, string appId, string wayCode, long amount, string bankCardType = null)
        {
            var configType = CS.CONFIG_TYPE.MCHRATE;
            var infoType = CS.INFO_TYPE.MCH_APP;
            var payRateConfigs = _payRateConfigRepository.GetByInfoId(configType, infoType, appId);
            var ifCodes = payRateConfigs.Where(w => w.WayCode.Equals(wayCode)).Select(s => s.IfCode).Distinct().ToList();

            var entity = _mchPayPassageRepository.GetAll().Where(w => w.State.Equals(CS.YES)
            && w.MchNo.Equals(mchNo)
            && w.AppId.Equals(appId)
            && ifCodes.Contains(w.IfCode)
            && w.WayCode.Equals(wayCode)).FirstOrDefault();

            if (entity == null)
            {
                return null;
            }

            var payRateConfig = payRateConfigs.FirstOrDefault(w => w.IfCode.Equals(entity.IfCode) && w.WayCode.Equals(wayCode));
            if (payRateConfig.FeeRate.Equals(CS.FEE_TYPE_SINGLE))
            {
                entity.Rate = payRateConfig.FeeRate.Value;
            }
            if (payRateConfig.FeeType.Equals(CS.FEE_TYPE_LEVEL))
            {
                var payRateLevelConfigs = _payRateLevelConfigRepository.GetByRateConfigId(payRateConfig.Id);
                if (payRateConfig.LevelMode.Equals(CS.LEVEL_MODE_NORMAL))
                {
                    var payRateLevelConfig = payRateLevelConfigs.FirstOrDefault(w => string.IsNullOrEmpty(w.BankCardType) && w.MinAmount < amount && w.MaxAmount <= amount);
                    if (payRateLevelConfig == null)
                    {
                        return null;
                    }
                    entity.Rate = payRateLevelConfig.FeeRate.Value;
                }

                if (payRateConfig.LevelMode.Equals(CS.LEVEL_MODE_UNIONPAY))
                {
                    var payRateLevelConfig = payRateLevelConfigs.FirstOrDefault(w => w.BankCardType.Equals(bankCardType) && w.MinAmount < amount && w.MaxAmount <= amount);
                    if (payRateLevelConfig == null)
                    {
                        return null;
                    }
                    entity.Rate = payRateLevelConfig.FeeRate.Value;
                }
            }
            var dto = _mapper.Map<MchPayPassageDto>(entity);
            return dto;
        }

        public bool IsExistMchPayPassageUseWayCode(string wayCode)
        {
            return _mchPayPassageRepository.IsExistMchPayPassageUseWayCode(wayCode);
        }
    }
}

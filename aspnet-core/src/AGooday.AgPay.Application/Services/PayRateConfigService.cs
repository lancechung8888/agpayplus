﻿using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Exceptions;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static AGooday.AgPay.Application.DataTransfer.PayRateConfigSaveDto;

namespace AGooday.AgPay.Application.Services
{
    /// <summary>
    /// 支付费率配置记录表 服务实现类
    /// </summary>
    public class PayRateConfigService : IPayRateConfigService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly IIsvInfoRepository _isvInfoRepository;
        private readonly IAgentInfoRepository _agentInfoRepository;
        private readonly IMchInfoRepository _mchInfoRepository;
        private readonly IMchAppRepository _mchAppRepository;
        private readonly IPayWayRepository _payWayRepository;
        private readonly IPayInterfaceDefineRepository _payInterfaceDefineRepository;
        private readonly IPayRateConfigRepository _payRateConfigRepository;
        private readonly IPayRateLevelConfigRepository _payRateLevelConfigRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public PayRateConfigService(IMapper mapper, IMediatorHandler bus,
            IPayRateConfigRepository payRateConfigRepository,
            IPayRateLevelConfigRepository payRateLevelConfigRepository,
            IIsvInfoRepository isvInfoRepository,
            IAgentInfoRepository agentInfoRepository,
            IMchInfoRepository mchInfoRepository,
            IMchAppRepository mchAppRepository,
            IPayWayRepository payWayRepository,
            IPayInterfaceDefineRepository payInterfaceDefineRepository)
        {
            _mapper = mapper;
            Bus = bus;
            _payRateConfigRepository = payRateConfigRepository;
            _payRateLevelConfigRepository = payRateLevelConfigRepository;
            _isvInfoRepository = isvInfoRepository;
            _agentInfoRepository = agentInfoRepository;
            _mchInfoRepository = mchInfoRepository;
            _mchAppRepository = mchAppRepository;
            _payWayRepository = payWayRepository;
            _payInterfaceDefineRepository = payInterfaceDefineRepository;
        }

        public PaginatedList<PayWayDto> GetPayWaysByInfoId(PayWayUsableQueryDto dto)
        {
            string infoType = string.Empty;
            var wayCodes = new List<string>();

            var payIfDefine = _payInterfaceDefineRepository.GetById(dto.IfCode);
            var payIfWayCodes = JsonConvert.DeserializeObject<object[]>(payIfDefine.WayCodes).Select(obj => (string)((dynamic)obj).wayCode).ToList();

            switch (dto.ConfigMode)
            {
                case CS.CONFIG_MODE_MGR_ISV:
                    infoType = CS.INFO_TYPE_ISV;
                    wayCodes = payIfWayCodes;
                    break;
                case CS.CONFIG_MODE_MGR_AGENT:
                case CS.CONFIG_MODE_AGENT_SELF:
                case CS.CONFIG_MODE_AGENT_SUBAGENT:
                    infoType = CS.INFO_TYPE_AGENT;
                    var agent = _agentInfoRepository.GetById(dto.InfoId);
                    wayCodes = GetPayWayCodes(dto.IfCode, agent.IsvNo, agent.Pid, payIfWayCodes);
                    break;
                case CS.CONFIG_MODE_MGR_MCH:
                case CS.CONFIG_MODE_AGENT_MCH:
                case CS.CONFIG_MODE_MCH_SELF_APP1:
                case CS.CONFIG_MODE_MCH_SELF_APP2:
                    infoType = CS.INFO_TYPE_MCH_APP;
                    var mchApp = _mchAppRepository.GetById(dto.InfoId);
                    var mchInfo = _mchInfoRepository.GetById(mchApp.MchNo);
                    wayCodes = payIfWayCodes;
                    if (mchInfo.Type.Equals(CS.MCH_TYPE_ISVSUB))
                    {
                        wayCodes = GetPayWayCodes(dto.IfCode, mchInfo.IsvNo, mchInfo.AgentNo, payIfWayCodes);
                    }
                    break;
                default:
                    break;
            }
            var payWays = _payWayRepository.GetAll().Where(w => wayCodes.Contains(w.WayCode))
                .OrderByDescending(o => o.WayCode).ThenByDescending(o => o.CreatedAt);
            var records = PaginatedList<PayWay>.Create<PayWayDto>(payWays.AsNoTracking(), _mapper, dto.PageNumber, dto.PageSize);
            return records;
        }

        private List<string> GetPayWayCodes(string ifCode, string isvNo, string agentNo, List<string> wayCodes)
        {
            // 服务商开通支付方式
            var isvWayCodes = _payRateConfigRepository.GetByInfoIdAndIfCode(CS.CONFIG_TYPE_ISVCOST, CS.INFO_TYPE_ISV, isvNo, ifCode)
                .Where(w => wayCodes.Contains(w.WayCode)).Select(s => s.WayCode).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(agentNo))
            {
                return GetAgentPayWayCodes(ifCode, isvWayCodes, agentNo);
            }
            return isvWayCodes;
        }

        private List<string> GetAgentPayWayCodes(string ifCode, List<string> wayCodes, string agentNo)
        {
            // 代理商开通支付方式
            var agentWayCodes = _payRateConfigRepository.GetByInfoIdAndIfCode(CS.CONFIG_TYPE_AGENTRATE, CS.INFO_TYPE_AGENT, agentNo, ifCode)
                .Where(w => wayCodes.Contains(w.WayCode)).Select(s => s.WayCode).Distinct().ToList();
            var agent = _agentInfoRepository.GetById(agentNo);
            if (!string.IsNullOrWhiteSpace(agent.Pid))
            {
                return GetAgentPayWayCodes(ifCode, agentWayCodes, agent.Pid);
            }
            else
            {
                return agentWayCodes;
            }
        }

        public Dictionary<string, Dictionary<string, PayRateConfigDto>> GetByInfoIdAndIfCode(string configMode, string infoId, string ifCode)
        {
            string infoType = string.Empty;
            Dictionary<string, Dictionary<string, PayRateConfigDto>> rateConfig = new Dictionary<string, Dictionary<string, PayRateConfigDto>>();
            switch (configMode)
            {
                case CS.CONFIG_MODE_MGR_ISV:
                    infoType = CS.INFO_TYPE_ISV;
                    rateConfig.Add(CS.CONFIG_TYPE_ISVCOST, GetPayRateConfig(CS.CONFIG_TYPE_ISVCOST, infoType, infoId, ifCode));
                    rateConfig.Add(CS.CONFIG_TYPE_AGENTDEF, GetPayRateConfig(CS.CONFIG_TYPE_AGENTDEF, infoType, infoId, ifCode));
                    rateConfig.Add(CS.CONFIG_TYPE_MCHAPPLYDEF, GetPayRateConfig(CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, infoId, ifCode));
                    break;
                case CS.CONFIG_MODE_MGR_AGENT:
                case CS.CONFIG_MODE_AGENT_SELF:
                case CS.CONFIG_MODE_AGENT_SUBAGENT:
                    infoType = CS.INFO_TYPE_AGENT;
                    var agent = _agentInfoRepository.GetById(infoId);
                    rateConfig.Add(CS.CONFIG_TYPE_AGENTDEF, GetPayRateConfig(CS.CONFIG_TYPE_AGENTDEF, infoType, infoId, ifCode));
                    rateConfig.Add(CS.CONFIG_TYPE_AGENTRATE, GetPayRateConfig(CS.CONFIG_TYPE_AGENTRATE, infoType, infoId, ifCode));
                    rateConfig.Add(CS.CONFIG_TYPE_MCHAPPLYDEF, GetPayRateConfig(CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, infoId, ifCode));
                    GetReadOnlyRate(ifCode, rateConfig, agent.IsvNo, agent.Pid, CS.CONFIG_TYPE_AGENTDEF);
                    break;
                case CS.CONFIG_MODE_MGR_MCH:
                case CS.CONFIG_MODE_AGENT_MCH:
                case CS.CONFIG_MODE_MCH_SELF_APP1:
                case CS.CONFIG_MODE_MCH_SELF_APP2:
                    infoType = CS.INFO_TYPE_MCH_APP;
                    var mchApp = _mchAppRepository.GetById(infoId);
                    var mchInfo = _mchInfoRepository.GetById(mchApp.MchNo);
                    rateConfig.Add(CS.CONFIG_TYPE_MCHRATE, GetPayRateConfig(CS.CONFIG_TYPE_MCHRATE, infoType, infoId, ifCode));
                    if (mchInfo.Type.Equals(CS.MCH_TYPE_ISVSUB))
                    {
                        GetReadOnlyRate(ifCode, rateConfig, mchInfo.IsvNo, mchInfo.AgentNo, CS.CONFIG_TYPE_MCHAPPLYDEF);
                    }
                    break;
                default:
                    break;
            }
            return rateConfig;
        }

        private void GetReadOnlyRate(string ifCode, Dictionary<string, Dictionary<string, PayRateConfigDto>> rateConfig, string isvNo, string agentNo, string configType)
        {
            // 服务商底价
            rateConfig.Add(CS.CONFIG_TYPE_READONLYISVCOST, GetPayRateConfig(CS.CONFIG_TYPE_ISVCOST, CS.INFO_TYPE_ISV, isvNo, ifCode));

            // 上级代理商费率
            if (!string.IsNullOrWhiteSpace(agentNo))
            {
                // 上级代理商费率
                rateConfig.Add(CS.CONFIG_TYPE_READONLYPARENTAGENT, GetPayRateConfig(CS.CONFIG_TYPE_AGENTRATE, CS.INFO_TYPE_AGENT, agentNo, ifCode));
                // 上级默认费率
                rateConfig.Add(CS.CONFIG_TYPE_READONLYPARENTDEFRATE, GetPayRateConfig(configType, CS.INFO_TYPE_AGENT, agentNo, ifCode));
            }
            else
            {
                rateConfig.Add(CS.CONFIG_TYPE_READONLYPARENTDEFRATE, GetPayRateConfig(configType, CS.INFO_TYPE_ISV, isvNo, ifCode));
            }
        }

        private Dictionary<string, PayRateConfigDto> GetPayRateConfig(string configType, string infoType, string infoId, string ifCode)
        {
            Dictionary<string, PayRateConfigDto> keyValues = new Dictionary<string, PayRateConfigDto>();
            var payRateConfigs = GetPayRateConfigs(configType, infoType, infoId, ifCode);
            foreach (var payRateConfig in payRateConfigs)
            {
                keyValues.Add(payRateConfig.WayCode, payRateConfig);
            }
            return keyValues;
        }

        public JObject GetByInfoIdAndIfCodeJson(string configMode, string infoId, string ifCode)
        {
            JObject result = new JObject();
            string infoType = string.Empty;
            switch (configMode)
            {
                case CS.CONFIG_MODE_MGR_ISV:
                    infoType = CS.INFO_TYPE_ISV;
                    result.Add(CS.CONFIG_TYPE_ISVCOST, GetPayRateConfigJson(CS.CONFIG_TYPE_ISVCOST, infoType, infoId, ifCode));
                    result.Add(CS.CONFIG_TYPE_AGENTDEF, GetPayRateConfigJson(CS.CONFIG_TYPE_AGENTDEF, infoType, infoId, ifCode));
                    result.Add(CS.CONFIG_TYPE_MCHAPPLYDEF, GetPayRateConfigJson(CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, infoId, ifCode));
                    break;
                case CS.CONFIG_MODE_MGR_AGENT:
                case CS.CONFIG_MODE_AGENT_SELF:
                case CS.CONFIG_MODE_AGENT_SUBAGENT:
                    infoType = CS.INFO_TYPE_AGENT;
                    var agent = _agentInfoRepository.GetById(infoId);
                    result.Add(CS.CONFIG_TYPE_AGENTDEF, GetPayRateConfigJson(CS.CONFIG_TYPE_AGENTDEF, infoType, infoId, ifCode));
                    result.Add(CS.CONFIG_TYPE_AGENTRATE, GetPayRateConfigJson(CS.CONFIG_TYPE_AGENTRATE, infoType, infoId, ifCode));
                    result.Add(CS.CONFIG_TYPE_MCHAPPLYDEF, GetPayRateConfigJson(CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, infoId, ifCode));
                    GetReadOnlyRateJson(ifCode, result, agent.IsvNo, agent.Pid, CS.CONFIG_TYPE_AGENTDEF);
                    break;
                case CS.CONFIG_MODE_MGR_MCH:
                case CS.CONFIG_MODE_AGENT_MCH:
                case CS.CONFIG_MODE_MCH_SELF_APP1:
                case CS.CONFIG_MODE_MCH_SELF_APP2:
                    infoType = CS.INFO_TYPE_MCH_APP;
                    var mchApp = _mchAppRepository.GetById(infoId);
                    var mchInfo = _mchInfoRepository.GetById(mchApp.MchNo);
                    result.Add(CS.CONFIG_TYPE_MCHRATE, GetPayRateConfigJson(CS.CONFIG_TYPE_MCHRATE, infoType, infoId, ifCode));
                    if (mchInfo.Type.Equals(CS.MCH_TYPE_ISVSUB))
                    {
                        GetReadOnlyRateJson(ifCode, result, mchInfo.IsvNo, mchInfo.AgentNo, CS.CONFIG_TYPE_MCHAPPLYDEF);
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        private void GetReadOnlyRateJson(string ifCode, JObject result, string isvNo, string agentNo, string configType)
        {
            // 服务商底价
            result.Add(CS.CONFIG_TYPE_READONLYISVCOST, GetPayRateConfigJson(CS.CONFIG_TYPE_ISVCOST, CS.INFO_TYPE_ISV, isvNo, ifCode));

            if (!string.IsNullOrWhiteSpace(agentNo))
            {
                // 上级代理商费率
                result.Add(CS.CONFIG_TYPE_READONLYPARENTAGENT, GetPayRateConfigJson(CS.CONFIG_TYPE_AGENTRATE, CS.INFO_TYPE_AGENT, agentNo, ifCode));
                // 上级默认费率
                result.Add(CS.CONFIG_TYPE_READONLYPARENTDEFRATE, GetPayRateConfigJson(configType, CS.INFO_TYPE_AGENT, agentNo, ifCode));
            }
            else
            {
                result.Add(CS.CONFIG_TYPE_READONLYPARENTDEFRATE, GetPayRateConfigJson(configType, CS.INFO_TYPE_ISV, isvNo, ifCode));
            }
        }

        private JObject GetPayRateConfigJson(string configType, string infoType, string infoId, string ifCode)
        {
            JObject result = new JObject();
            var payRateConfigs = GetPayRateConfigs(configType, infoType, infoId, ifCode);
            foreach (var item in payRateConfigs)
            {
                JObject payRateConfig = new JObject();
                payRateConfig.Add("wayCode", item.WayCode);
                payRateConfig.Add("state", item.State);
                payRateConfig.Add("feeType", item.FeeType);
                payRateConfig.Add("levelMode", item.LevelMode);
                payRateConfig.Add("applymentSupport", item.ApplymentSupport);
                if (item.FeeType.Equals(CS.FEE_TYPE_SINGLE))
                {
                    payRateConfig.Add("feeRate", item.FeeRate);
                }
                if (item.FeeType.Equals(CS.FEE_TYPE_LEVEL))
                {
                    JArray array = new JArray();
                    foreach (var levelitem in item.PayRateLevelConfigs.GroupBy(g => g.BankCardType))
                    {
                        JObject payRateLevelConfig = new JObject();
                        payRateLevelConfig.Add("minFee", levelitem.Min(m => m.MinFee));
                        payRateLevelConfig.Add("maxFee", levelitem.Max(m => m.MaxFee));
                        if (string.IsNullOrWhiteSpace(levelitem.Key))
                        {
                            payRateLevelConfig.Add("bankCardType", levelitem.Key);
                        }
                        payRateLevelConfig.Add("levelList", JArray.FromObject(levelitem.Select(s => new
                        {
                            minAmount = s.MinAmount,
                            maxAmount = s.MaxAmount,
                            feeRate = s.FeeRate
                        })));
                        array.Add(payRateLevelConfig);
                    }
                    payRateConfig.Add(item.LevelMode, array);
                }
                result.Add(item.WayCode, payRateConfig);
            }
            return result;
        }

        private List<PayRateConfigItem> GetPayRateConfigItems(string configType, string infoType, string infoId, string ifCode)
        {
            var payRateConfigs = GetPayRateConfigs(configType, infoType, infoId, ifCode);
            var result = payRateConfigs.Select(item =>
            {
                var r = new PayRateConfigItem()
                {
                    WayCode = item.WayCode,
                    State = item.State,
                    FeeType = item.FeeType,
                    LevelMode = item.LevelMode,
                    ApplymentSupport = item.ApplymentSupport,
                    FeeRate = item.FeeRate,
                };
                r.NORMAL = item.PayRateLevelConfigs.Where(w => string.IsNullOrEmpty(w.BankCardType)).ToList()
                .GroupBy(g => g.BankCardType)
                .Select(s => new Levels()
                {
                    MinFee = s.Min(m => m.MinFee),
                    MaxFee = s.Min(m => m.MaxFee),
                    BankCardType = s.Key,
                    LevelList = s.Select(l => new LevelList
                    {
                        MinAmount = l.MinAmount,
                        MaxAmount = l.MaxAmount,
                        FeeRate = l.FeeRate
                    }).ToList()
                }).ToList();
                r.UNIONPAY = item.PayRateLevelConfigs.Where(w => !string.IsNullOrEmpty(w.BankCardType)).ToList()
                .GroupBy(g => g.BankCardType)
                .Select(s => new Levels()
                {
                    MinFee = s.Min(m => m.MinFee),
                    MaxFee = s.Min(m => m.MaxFee),
                    BankCardType = s.Key,
                    LevelList = s.Select(l => new LevelList
                    {
                        MinAmount = l.MinAmount,
                        MaxAmount = l.MaxAmount,
                        FeeRate = l.FeeRate
                    }).ToList()
                }).ToList();
                return r;
            }).ToList();

            return result;
        }

        public List<PayRateConfigDto> GetPayRateConfigs(string configType, string infoType, string infoId, string ifCode)
        {
            var payRateConfigs = _payRateConfigRepository.GetByInfoIdAndIfCode(configType, infoType, infoId, ifCode);
            var result = _mapper.Map<List<PayRateConfigDto>>(payRateConfigs);
            foreach (var item in result)
            {
                var payRateLevelConfigs = _payRateLevelConfigRepository.GetByRateConfigId(item.Id);
                item.PayRateLevelConfigs = _mapper.Map<List<PayRateLevelConfigDto>>(payRateLevelConfigs);
            }

            return result;
        }

        public PayRateConfigItem GetPayRateConfigItem(string configType, string infoType, string infoId, string ifCode, string wayCode)
        {
            var payRateConfig = _payRateConfigRepository.GetByUniqueKey(configType, infoType, infoId, ifCode, wayCode);

            var payRateLevelConfigs = _payRateLevelConfigRepository.GetByRateConfigId(payRateConfig.Id);

            var result = new PayRateConfigItem()
            {
                WayCode = payRateConfig.WayCode,
                State = payRateConfig.State,
                FeeType = payRateConfig.FeeType,
                LevelMode = payRateConfig.LevelMode,
                ApplymentSupport = payRateConfig.ApplymentSupport,
                FeeRate = payRateConfig.FeeRate,
            };
            result.NORMAL = payRateLevelConfigs.Where(w => string.IsNullOrEmpty(w.BankCardType)).ToList()
                .GroupBy(g => g.BankCardType)
                .Select(s => new Levels()
                {
                    MinFee = s.Min(m => m.MinFee),
                    MaxFee = s.Min(m => m.MaxFee),
                    BankCardType = s.Key,
                    LevelList = s.Select(l => new LevelList
                    {
                        MinAmount = l.MinAmount,
                        MaxAmount = l.MaxAmount,
                        FeeRate = l.FeeRate
                    }).ToList()
                }).ToList();
            result.UNIONPAY = payRateLevelConfigs.Where(w => !string.IsNullOrEmpty(w.BankCardType)).ToList()
                .GroupBy(g => g.BankCardType)
                .Select(s => new Levels()
                {
                    MinFee = s.Min(m => m.MinFee),
                    MaxFee = s.Min(m => m.MaxFee),
                    BankCardType = s.Key,
                    LevelList = s.Select(l => new LevelList
                    {
                        MinAmount = l.MinAmount,
                        MaxAmount = l.MaxAmount,
                        FeeRate = l.FeeRate
                    }).ToList()
                }).ToList();
            return result;
        }

        public bool SaveOrUpdate(PayRateConfigSaveDto dto)
        {
            var checkResult = PayRateConfigCheck(dto);
            if (!checkResult.IsPassed)
            {
                throw new BizException(checkResult.Message);
            }
            string infoId = dto.InfoId;
            var ifCode = dto.IfCode;
            var delPayWayCodes = dto.DelPayWayCodes;
            string infoType;
            switch (dto.ConfigMode)
            {
                case CS.CONFIG_MODE_MGR_ISV:
                    infoType = CS.INFO_TYPE_ISV;
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_ISVCOST, infoType, delPayWayCodes, dto.ISVCOST);
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_AGENTDEF, infoType, delPayWayCodes, dto.AGENTDEF);
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, delPayWayCodes, dto.MCHAPPLYDEF);
                    break;
                case CS.CONFIG_MODE_MGR_AGENT:
                case CS.CONFIG_MODE_AGENT_SELF:
                case CS.CONFIG_MODE_AGENT_SUBAGENT:
                    infoType = CS.INFO_TYPE_AGENT;
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_AGENTRATE, infoType, delPayWayCodes, dto.AGENTRATE);
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_AGENTDEF, infoType, delPayWayCodes, dto.AGENTDEF);
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_MCHAPPLYDEF, infoType, delPayWayCodes, dto.MCHAPPLYDEF);
                    break;
                case CS.CONFIG_MODE_MGR_MCH:
                case CS.CONFIG_MODE_AGENT_MCH:
                case CS.CONFIG_MODE_MCH_SELF_APP1:
                case CS.CONFIG_MODE_MCH_SELF_APP2:
                    infoType = CS.INFO_TYPE_MCH_APP;
                    SaveOrUpdate(infoId, ifCode, CS.CONFIG_TYPE_MCHRATE, infoType, delPayWayCodes, dto.MCHRATE);
                    break;
                default:
                    break;
            }

            return true;
        }

        private void SaveOrUpdate(string infoId, string ifCode, string configType, string infoType, List<string> delPayWayCodes, List<PayRateConfigItem> items)
        {
            var now = DateTime.Now;
            DelPayWayCodeRateConfig(infoId, ifCode, configType, infoType, delPayWayCodes);
            foreach (var item in items)
            {
                var entity = _payRateConfigRepository.GetByUniqueKey(configType, infoType, infoId, ifCode, item.WayCode);
                if (entity == null)
                {
                    entity = new PayRateConfig
                    {
                        ConfigType = configType,
                        InfoType = infoType,
                        InfoId = infoId,
                        IfCode = ifCode,
                        WayCode = item.WayCode,
                        FeeType = item.FeeType,
                        LevelMode = item.LevelMode,
                        FeeRate = item.FeeRate,
                        ApplymentSupport = item.ApplymentSupport,
                        State = item.State,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    _payRateConfigRepository.Add(entity);
                }
                else
                {
                    var payRateLevelConfigs = _payRateLevelConfigRepository.GetByRateConfigId(entity.Id);
                    foreach (var payRateLevelConfig in payRateLevelConfigs)
                    {
                        _payRateLevelConfigRepository.Remove(payRateLevelConfig.Id);
                    }

                    entity.FeeType = item.FeeType;
                    entity.LevelMode = item.LevelMode;
                    entity.FeeRate = item.FeeRate;
                    entity.ApplymentSupport = item.ApplymentSupport;
                    entity.State = item.State;
                    entity.UpdatedAt = now;
                    _payRateConfigRepository.Update(entity);
                }
                _payRateConfigRepository.SaveChanges();

                if (item.FeeType.Equals(CS.FEE_TYPE_LEVEL))
                {
                    foreach (var level in (item.LevelMode.Equals(CS.LEVEL_MODE_NORMAL) ? item.NORMAL : item.UNIONPAY))
                    {
                        foreach (var levelitem in level.LevelList)
                        {
                            var payRateLevelConfig = new PayRateLevelConfig
                            {
                                RateConfigId = entity.Id,
                                BankCardType = level.BankCardType,
                                MinFee = level.MinFee,
                                MaxFee = level.MaxFee,
                                MinAmount = levelitem.MinAmount,
                                MaxAmount = levelitem.MaxAmount,
                                FeeRate = levelitem.FeeRate,
                                State = item.State,
                                CreatedAt = now,
                                UpdatedAt = now,
                            };
                            _payRateLevelConfigRepository.Add(payRateLevelConfig);
                        }
                    }
                }

                _payRateLevelConfigRepository.SaveChanges();
            }
        }

        private void DelPayWayCodeRateConfig(string infoId, string ifCode, string configType, string infoType, List<string> delPayWayCodes)
        {
            foreach (var wayCode in delPayWayCodes)
            {
                var entity = _payRateConfigRepository.GetByUniqueKey(configType, infoType, infoId, ifCode, wayCode);
                if (entity != null)
                {
                    _payRateConfigRepository.Remove(entity.Id);

                    _payRateConfigRepository.SaveChanges();

                    var payRateLevelConfigs = _payRateLevelConfigRepository.GetByRateConfigId(entity.Id);
                    foreach (var payRateLevelConfig in payRateLevelConfigs)
                    {
                        _payRateLevelConfigRepository.Remove(payRateLevelConfig.Id);
                    }

                    _payRateLevelConfigRepository.SaveChanges();
                }
            }
        }

        private (bool IsPassed, string Message) PayRateConfigCheck(PayRateConfigSaveDto dto)
        {
            if (dto.NoCheckRuleFlag != CS.YES)
            {
                string infoId = dto.InfoId;
                var ifCode = dto.IfCode;
                List<PayRateConfigItem> ISVCOST = null, AGENTDEF = null, MCHAPPLYDEF = null, AGENTRATE = null, MCHRATE = null, PARENTRATE = null;
                switch (dto.ConfigMode)
                {
                    case CS.CONFIG_MODE_MGR_ISV:
                        ISVCOST = dto.ISVCOST; // 服务商底价费率
                        AGENTDEF = dto.AGENTDEF; // 代理商默认费率
                        MCHAPPLYDEF = dto.MCHAPPLYDEF; // 商户进件默认费率
                        for (int i = 0; i < ISVCOST.Count; i++)
                        {
                            var mainFee = ISVCOST[i];
                            var wayCode = mainFee.WayCode;
                            if (mainFee.FeeType.Equals(CS.FEE_TYPE_SINGLE))
                            {
                                var isvCostFeeRate = mainFee.FeeRate;
                                var agentDefFeeRate = GetFeeRate(AGENTDEF, i);
                                var mchApplyDefFeeRate = GetFeeRate(MCHAPPLYDEF, i);
                                if (agentDefFeeRate < isvCostFeeRate)
                                {
                                    //return (false, $"代理商默认费率异常： [{wayCode}]设置费率{{{(agentDefFeeRate * 100)}%}} 需要【大于等于】【服务商底价费率】的配置值：{{{(isvCostFeeRate * 100)}%}}");
                                    return (false, GetFeeRateErrorMessage("代理商默认费率", "服务商底价费率", wayCode, agentDefFeeRate.Value, isvCostFeeRate.Value));
                                }
                                if (mchApplyDefFeeRate < agentDefFeeRate)
                                {
                                    //return (false, $"商户进件默认费率异常： [{wayCode}]的设置费率{{{(mchApplyDefFeeRate * 100)}%}} 需要【大于等于】【代理商默认费率】的配置值：{{{(agentDefFeeRate * 100)}%}}");
                                    return (false, GetFeeRateErrorMessage("商户进件默认费率", "代理商默认费率", wayCode, mchApplyDefFeeRate.Value, agentDefFeeRate.Value));
                                }
                            }
                            if (mainFee.FeeType.Equals(CS.FEE_TYPE_LEVEL))
                            {
                                var levels = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_NORMAL) ? mainFee.NORMAL : mainFee.UNIONPAY);
                                for (int j = 0; j < levels.Count; j++)
                                {
                                    var level = levels[j];
                                    for (int k = 0; k < level.LevelList.Count; k++)
                                    {
                                        var isvCostFeeRate = level.LevelList[k].FeeRate;
                                        var agentDefFeeRate = GetFeeRate(AGENTDEF, i, j, k);
                                        var mchApplyDefFeeRate = GetFeeRate(MCHAPPLYDEF, j, k);
                                        var modeName = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_UNIONPAY) ? (level.BankCardType.Equals(CS.BANK_CARD_TYPE_DEBIT) ? "借记卡" : (level.BankCardType.Equals(CS.BANK_CARD_TYPE_CREDIT) ? "贷记卡" : "")) : "");
                                        if (agentDefFeeRate < isvCostFeeRate)
                                        {
                                            //return (false, $"代理商默认费率异常： [{wayCode}]{modeName}的第[{k}]阶梯设置费率{{{(agentDefFeeRate * 100)}%}} 需要【大于等于】【服务商底价费率】的阶梯配置值：{{{(isvCostFeeRate * 100)}%}}");
                                            return (false, GetFeeRateErrorMessage("代理商默认费率", "服务商底价费率", wayCode, agentDefFeeRate.Value, isvCostFeeRate.Value, modeName, k));
                                        }
                                        if (mchApplyDefFeeRate < agentDefFeeRate)
                                        {
                                            //return (false, $"商户进件默认费率异常： [{wayCode}]{modeName}的第[{k}]阶梯设置费率{{{(mchApplyDefFeeRate * 100)}%}} 需要【大于等于】【代理商默认费率】的阶梯配置值：{{{(agentDefFeeRate * 100)}%}}");
                                            return (false, GetFeeRateErrorMessage("商户进件默认费率", "代理商默认费率", wayCode, mchApplyDefFeeRate.Value, agentDefFeeRate.Value, modeName, k));
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case CS.CONFIG_MODE_MGR_AGENT:
                    case CS.CONFIG_MODE_AGENT_SELF:
                    case CS.CONFIG_MODE_AGENT_SUBAGENT:
                        var agent = _agentInfoRepository.GetById(infoId);
                        AGENTRATE = dto.AGENTRATE; // 当前代理商费率
                        AGENTDEF = dto.AGENTDEF; // 下级代理商默认费率
                        MCHAPPLYDEF = dto.MCHAPPLYDEF; // 代理商子商户进件默认
                        PARENTRATE = GetParentRate(ifCode, agent.IsvNo, agent.Pid, CS.CONFIG_TYPE_AGENTDEF);
                        for (int i = 0; i < AGENTRATE.Count; i++)
                        {
                            var mainFee = AGENTRATE[i];
                            var wayCode = mainFee.WayCode;
                            if (mainFee.FeeType.Equals(CS.FEE_TYPE_SINGLE))
                            {
                                var parentFeeRate = GetFeeRate(PARENTRATE, i);
                                var agentRateFeeRate = mainFee.FeeRate;
                                var agentDefFeeRate = GetFeeRate(AGENTDEF, i);
                                var mchApplyDefFeeRate = GetFeeRate(MCHAPPLYDEF, i);
                                if (agentRateFeeRate < parentFeeRate)
                                {
                                    // return (false, $"代理商费率异常： [{wayCode}]设置费率{{{(agentDefFeeRate * 100)}%}} 需要【大于等于】【{(string.IsNullOrWhiteSpace(agent.Pid) ? "服务商底价" : "上级代理商费率")}费率】的配置值：{{{(parentFeeRate * 100)}%}}");
                                    var thanName = string.IsNullOrWhiteSpace(agent.Pid) ? "服务商底价" : "上级代理商费率";
                                    return (false, GetFeeRateErrorMessage("代理商费率", thanName, wayCode, agentRateFeeRate.Value, parentFeeRate.Value));
                                }
                                if (agentDefFeeRate < agentRateFeeRate)
                                {
                                    //return (false, $"代理商默认费率异常： [{wayCode}]设置费率{{{(agentDefFeeRate * 100)}%}} 需要【大于等于】【代理商费率】的配置值：{{{(agentRateFeeRate * 100)}%}}");
                                    return (false, GetFeeRateErrorMessage("代理商默认费率", "代理商费率", wayCode, agentDefFeeRate.Value, agentRateFeeRate.Value));
                                }
                                if (mchApplyDefFeeRate < agentDefFeeRate)
                                {
                                    //return (false, $"商户进件默认费率异常： [{wayCode}]的设置费率{{{(mchApplyDefFeeRate * 100)}%}} 需要【大于等于】【代理商默认费率】的配置值：{{{(agentDefFeeRate * 100)}%}}");
                                    return (false, GetFeeRateErrorMessage("商户进件默认费率", "代理商默认费率", wayCode, mchApplyDefFeeRate.Value, agentDefFeeRate.Value));
                                }
                            }
                            if (mainFee.FeeType.Equals(CS.FEE_TYPE_LEVEL))
                            {
                                var levels = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_NORMAL) ? mainFee.NORMAL : mainFee.UNIONPAY);
                                for (int j = 0; j < levels.Count; j++)
                                {
                                    var level = levels[j];
                                    for (int k = 0; k < level.LevelList.Count; k++)
                                    {
                                        var parentFeeRate = GetFeeRate(PARENTRATE, i, j, k);
                                        var agentRateFeeRate = level.LevelList[k].FeeRate;
                                        var agentDefFeeRate = GetFeeRate(AGENTDEF, i, j, k);
                                        var mchApplyDefFeeRate = GetFeeRate(AGENTDEF, j);
                                        var modeName = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_UNIONPAY) ? (level.BankCardType.Equals(CS.BANK_CARD_TYPE_DEBIT) ? "借记卡" : (level.BankCardType.Equals(CS.BANK_CARD_TYPE_CREDIT) ? "贷记卡" : "")) : "");
                                        if (agentRateFeeRate < parentFeeRate)
                                        {
                                            //return (false, $"代理商费率异常： [{wayCode}]{modeName}的第[{k}]阶梯设置费率{{{(agentRateFeeRate * 100)}%}} 需要【大于等于】【{(string.IsNullOrWhiteSpace(agent.Pid) ? "服务商底价" : "上级代理商费率")}】的阶梯配置值：{{{(parentFeeRate * 100)}%}}");
                                            var thanName = string.IsNullOrWhiteSpace(agent.Pid) ? "服务商底价" : "上级代理商费率";
                                            return (false, GetFeeRateErrorMessage("代理商费率", thanName, wayCode, agentRateFeeRate.Value, parentFeeRate.Value, modeName, k));
                                        }
                                        if (agentDefFeeRate < agentRateFeeRate)
                                        {
                                            //return (false, $"代理商默认费率异常： [{wayCode}]{modeName}的第[{k}]阶梯设置费率{{{(agentDefFeeRate * 100)}%}} 需要【大于等于】【代理商费率】的阶梯配置值：{{{(agentRateFeeRate * 100)}%}}");
                                            return (false, GetFeeRateErrorMessage("代理商默认费率", "代理商费率", wayCode, agentDefFeeRate.Value, agentRateFeeRate.Value, modeName, k));
                                        }
                                        if (mchApplyDefFeeRate < agentDefFeeRate)
                                        {
                                            //return (false, $"商户进件默认费率异常： [{wayCode}]{modeName}的第[{k}]阶梯的设置费率{{{(mchApplyDefFeeRate * 100)}%}} 需要【大于等于】【代理商默认费率】的阶梯配置值：{{{(agentDefFeeRate * 100)}%}}");
                                            return (false, GetFeeRateErrorMessage("商户进件默认费率", "代理商默认费率", wayCode, mchApplyDefFeeRate.Value, agentDefFeeRate.Value, modeName, k));
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case CS.CONFIG_MODE_MGR_MCH:
                    case CS.CONFIG_MODE_AGENT_MCH:
                    case CS.CONFIG_MODE_MCH_SELF_APP1:
                    case CS.CONFIG_MODE_MCH_SELF_APP2:
                        var mchApp = _mchAppRepository.GetById(infoId);
                        var mchInfo = _mchInfoRepository.GetById(mchApp.MchNo);
                        MCHRATE = dto.MCHRATE; // 商户费率
                        if (mchInfo.Type.Equals(CS.MCH_TYPE_ISVSUB))
                        {
                            PARENTRATE = GetParentRate(ifCode, mchInfo.IsvNo, mchInfo.AgentNo, CS.CONFIG_TYPE_MCHAPPLYDEF);
                            for (int i = 0; i < MCHRATE.Count; i++)
                            {
                                var mainFee = MCHRATE[i];
                                var wayCode = mainFee.WayCode;
                                if (mainFee.FeeType.Equals(CS.FEE_TYPE_SINGLE))
                                {
                                    var parentFeeRate = GetFeeRate(PARENTRATE, i);
                                    var mchRateFeeRate = mainFee.FeeRate;
                                    if (mchRateFeeRate < parentFeeRate)
                                    {
                                        //return (false, $"商家费率异常： [{wayCode}]设置费率{{{(mchRateFeeRate * 100)}%}} 需要【大于等于】【{(string.IsNullOrWhiteSpace(mchInfo.AgentNo) ? "服务商底价" : "上级代理商费率")}】的配置值：{{{(parentFeeRate * 100)}%}}");
                                        var thanName = string.IsNullOrWhiteSpace(mchInfo.AgentNo) ? "服务商底价" : "上级代理商费率";
                                        return (false, GetFeeRateErrorMessage("商家费率", thanName, wayCode, mchRateFeeRate.Value, parentFeeRate.Value));
                                    }
                                }
                                if (mainFee.FeeType.Equals(CS.FEE_TYPE_LEVEL))
                                {
                                    var levels = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_NORMAL) ? mainFee.NORMAL : mainFee.UNIONPAY);
                                    for (int j = 0; j < levels.Count; j++)
                                    {
                                        var level = levels[j];
                                        var modeName = (mainFee.LevelMode.Equals(CS.LEVEL_MODE_UNIONPAY) ? (level.BankCardType.Equals(CS.BANK_CARD_TYPE_DEBIT) ? "借记卡" : (level.BankCardType.Equals(CS.BANK_CARD_TYPE_CREDIT) ? "贷记卡" : "")) : "");
                                        for (int k = 0; k < level.LevelList.Count; k++)
                                        {
                                            var parentFeeRate = GetFeeRate(PARENTRATE, i, j, k);
                                            var mchRateFeeRate = level.LevelList[k].FeeRate;
                                            if (mchRateFeeRate < parentFeeRate)
                                            {
                                                //return (false, $"代理商费率异常： [{wayCode}]{modeName}的第[{k}]阶梯设置费率{{{(mchRateFeeRate * 100)}%}} 需要【大于等于】【上级底价费率】的阶梯配置值：{{{(parentFeeRate * 100)}%}}");
                                                return (false, GetFeeRateErrorMessage("代理商费率", "上级底价费率", wayCode, mchRateFeeRate.Value, parentFeeRate.Value, modeName, k));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            return (true, string.Empty);
        }

        private string GetFeeRateErrorMessage(string name, string thanName, string wayCode, decimal feeRate, decimal thanFeeRate, string modeName = "", int? levelIndex = null)
        {
            return $"{name}异常： [{wayCode}]{(levelIndex == null ? "" : $"{modeName}的第[{++levelIndex}]阶梯")}设置费率{{{(feeRate * 100)}%}} 需要【大于等于】【{thanName}】的{(levelIndex == null ? "" : "阶梯")}配置值：{{{(thanFeeRate * 100)}%}}";
        }

        private List<PayRateConfigItem> GetParentRate(string ifCode, string isvNo, string agentNo, string configType)
        {
            List<PayRateConfigItem> ISVCOST = null, READONLYPARENTAGENT = null, READONLYPARENTDEFRATE = null, PARENTRATE = null;

            // 服务商底价
            ISVCOST = GetPayRateConfigItems(CS.CONFIG_TYPE_ISVCOST, CS.INFO_TYPE_ISV, isvNo, ifCode);
            if (!string.IsNullOrWhiteSpace(agentNo))
            {
                // 上级代理商费率
                READONLYPARENTAGENT = GetPayRateConfigItems(CS.CONFIG_TYPE_AGENTRATE, CS.INFO_TYPE_AGENT, agentNo, ifCode);
                // 上级默认费率
                READONLYPARENTDEFRATE = GetPayRateConfigItems(configType, CS.INFO_TYPE_AGENT, agentNo, ifCode);
            }
            else
            {
                READONLYPARENTDEFRATE = GetPayRateConfigItems(configType, CS.INFO_TYPE_ISV, isvNo, ifCode);
            }
            PARENTRATE = READONLYPARENTDEFRATE == null ? (READONLYPARENTAGENT == null ? (ISVCOST == null ? null : ISVCOST) : READONLYPARENTAGENT) : READONLYPARENTDEFRATE;

            return PARENTRATE;
        }

        private decimal? GetFeeRate(List<PayRateConfigItem> configItem, int configItemIndex, int? levelModeIndex = null, int? levelIndex = null)
        {
            decimal? feeRate = null;
            var isvcost = configItem[configItemIndex];
            if (isvcost.FeeType.Equals(CS.FEE_TYPE_SINGLE))
            {
                feeRate = isvcost.FeeRate;
            }
            if (isvcost.FeeType.Equals(CS.FEE_TYPE_LEVEL) && levelModeIndex != null && levelIndex != null)
            {
                var levels = (isvcost.LevelMode.Equals(CS.LEVEL_MODE_NORMAL) ? isvcost.NORMAL : isvcost.UNIONPAY);
                var level = levels[levelModeIndex.Value];
                feeRate = level.LevelList[levelIndex.Value].FeeRate;
            }
            return feeRate;
        }
    }
}
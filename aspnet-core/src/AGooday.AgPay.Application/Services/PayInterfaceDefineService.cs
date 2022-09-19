﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Domain.Commands.SysUsers;
using AGooday.AgPay.Domain.Core.Bus;
using AGooday.AgPay.Domain.Interfaces;
using AGooday.AgPay.Domain.Models;
using AGooday.AgPay.Infrastructure.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Application.Services
{
    public class PayInterfaceDefineService : IPayInterfaceDefineService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly IPayInterfaceDefineRepository _payInterfaceDefineRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public PayInterfaceDefineService(IPayInterfaceDefineRepository payInterfaceDefineRepository, IMapper mapper, IMediatorHandler bus)
        {
            _payInterfaceDefineRepository = payInterfaceDefineRepository;
            _mapper = mapper;
            Bus = bus;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Add(PayInterfaceDefineDto dto)
        {
            var m = _mapper.Map<PayInterfaceDefine>(dto);
            _payInterfaceDefineRepository.Add(m);
            _payInterfaceDefineRepository.SaveChanges();
        }

        public void Remove(string recordId)
        {
            _payInterfaceDefineRepository.Remove(recordId);
            _payInterfaceDefineRepository.SaveChanges();
        }

        public void Update(PayInterfaceDefineDto dto)
        {
            var m = _mapper.Map<PayInterfaceDefine>(dto);
            _payInterfaceDefineRepository.Update(m);
            _payInterfaceDefineRepository.SaveChanges();
        }

        public PayInterfaceDefineDto GetById(string recordId)
        {
            var entity = _payInterfaceDefineRepository.GetById(recordId);
            var dto = _mapper.Map<PayInterfaceDefineDto>(entity);
            return dto;
        }

        public IEnumerable<PayInterfaceDefineDto> GetAll()
        {
            var payInterfaceDefines = _payInterfaceDefineRepository.GetAll();
            return _mapper.Map<IEnumerable<PayInterfaceDefineDto>>(payInterfaceDefines);
        }
    }
}

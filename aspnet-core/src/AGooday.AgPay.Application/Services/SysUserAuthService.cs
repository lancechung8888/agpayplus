﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.ViewModels;
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
    /// <summary>
    /// 操作员认证表 服务实现类
    /// </summary>
    public class SysUserAuthService : ISysUserAuthService
    {
        // 注意这里是要IoC依赖注入的，还没有实现
        private readonly ISysUserAuthRepository _sysUserAuthRepository;
        private readonly ISysUserRepository _sysUserRepository;
        // 用来进行DTO
        private readonly IMapper _mapper;
        // 中介者 总线
        private readonly IMediatorHandler Bus;

        public SysUserAuthService(IMapper mapper, IMediatorHandler bus, 
            ISysUserAuthRepository sysUserAuthRepository, 
            ISysUserRepository sysUserRepository)
        {
            _mapper = mapper;
            Bus = bus;
            _sysUserAuthRepository = sysUserAuthRepository;
            _sysUserRepository = sysUserRepository;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Add(SysUserAuthVM vm)
        {
            var m = _mapper.Map<SysUserAuth>(vm);
            _sysUserAuthRepository.Add(m);
            _sysUserAuthRepository.SaveChanges();
        }

        public void Remove(long recordId)
        {
            _sysUserAuthRepository.Remove(recordId);
            _sysUserAuthRepository.SaveChanges();
        }

        public void Update(SysUserAuthVM vm)
        {
            var m = _mapper.Map<SysUserAuth>(vm);
            _sysUserAuthRepository.Update(m);
            _sysUserAuthRepository.SaveChanges();
        }

        public SysUserAuthVM GetById(long recordId)
        {
            var entity = _sysUserAuthRepository.GetById(recordId);
            var vm = _mapper.Map<SysUserAuthVM>(entity);
            return vm;
        }

        public IEnumerable<SysUserAuthVM> GetAll()
        {
            var sysUserAuths = _sysUserAuthRepository.GetAll();
            return _mapper.Map<IEnumerable<SysUserAuthVM>>(sysUserAuths);
        }

        public SysUserAuthVM SelectByLogin(string identifier, byte identityType, string sysType)
        {
            var entity = _sysUserAuthRepository.GetAll()
                .Join(_sysUserRepository.GetAll(),
                ua => ua.UserId, ur => ur.SysUserId,
                (ua, ur) => ua)
                .Where(w => w.IdentityType == identityType && w.Identifier.Equals(identifier) && w.SysType.Equals(sysType))
                .FirstOrDefault();
            return _mapper.Map<SysUserAuthVM>(entity);
        }
    }
}

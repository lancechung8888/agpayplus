﻿using AGooday.AgPay.Application.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Application.Interfaces
{
    public interface ISysUserAuthService : IDisposable
    {
        void Add(SysUserAuthVM vm);
        void Remove(long recordId);
        void Update(SysUserAuthVM vm);
        SysUserAuthVM GetById(long recordId);
        IEnumerable<SysUserAuthVM> GetAll();
        SysUserAuthVM SelectByLogin(string identifier, byte identityType, string sysType);
    }
}

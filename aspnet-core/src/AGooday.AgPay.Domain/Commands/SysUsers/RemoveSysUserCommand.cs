﻿using AGooday.AgPay.Domain.Core.Commands;
using AGooday.AgPay.Domain.Validations.SysUsers;

namespace AGooday.AgPay.Domain.Commands.SysUsers
{
    public class RemoveSysUserCommand : Command
    {
        /// <summary>
        /// 系统用户ID
        /// </summary>
        public long SysUserId { get; set; }

        /// <summary>
        /// 当前系统用户ID
        /// </summary>
        public long CurrentSysUserId { get; set; }

        /// <summary>
        /// 所属系统： MGR-运营平台, AGENT-代理商中心, MCH-商户中心
        /// </summary>
        public string SysType { get; set; }

        public override bool IsValid()
        {
            ValidationResult = new RemoveSysUserCommandValidation().Validate(this);
            return ValidationResult.IsValid;
        }
    }
}

﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Services;
using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using AGooday.AgPay.Common.Utils;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Manager.Api.Authorization;
using AGooday.AgPay.Manager.Api.Attributes;

namespace AGooday.AgPay.Manager.Api.Controllers.SysUser
{
    /// <summary>
    /// 用户角色关联关系
    /// </summary>
    [Route("/api/sysUserRoleRelas")]
    [ApiController, Authorize]
    public class SysUserRoleRelaController : CommonController
    {
        private readonly ILogger<SysUserRoleRelaController> _logger;
        private readonly ISysUserRoleRelaService _sysUserRoleRelaService;

        public SysUserRoleRelaController(ILogger<SysUserRoleRelaController> logger, RedisUtil client,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            _logger = logger;
            _sysUserRoleRelaService = sysUserRoleRelaService;
        }

        /// <summary>
        /// 用户角色列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route(""), NoLog]
        [PermissionAuth(PermCode.MGR.ENT_UR_USER_UPD_ROLE)]
        public ApiRes List([FromQuery] SysUserRoleRelaQueryDto dto)
        {
            var data = _sysUserRoleRelaService.GetPaginatedData(dto);
            return ApiRes.Ok(new { Records = data.ToList(), Total = data.TotalCount, Current = data.PageIndex, HasNext = data.HasNext });
        }

        /// <summary>
        /// 重置用户角色关联信息
        /// </summary>
        /// <param name="sysUserId"></param>
        /// <returns></returns>
        [HttpPost, Route("relas/{sysUserId}"), MethodLog("更改用户角色信息")]
        [PermissionAuth(PermCode.MGR.ENT_UR_USER_UPD_ROLE)]
        public ApiRes Relas(long sysUserId, List<string> entIds)
        {
            if (entIds.Count() > 0)
            {
                _sysUserRoleRelaService.SaveUserRole(sysUserId, entIds);
                RefAuthentication(new List<long> { sysUserId });
            }
            return ApiRes.Ok();
        }
    }
}

﻿using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Merchant.Api.Attributes;
using AGooday.AgPay.Merchant.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AGooday.AgPay.Merchant.Api.Controllers.SysUser
{
    /// <summary>
    /// 权限 菜单 管理
    /// </summary>
    [Route("/api/sysEnts")]
    [ApiController, Authorize, NoLog]
    public class SysEntController : CommonController
    {
        private readonly ILogger<SysEntController> _logger;
        private readonly ISysEntitlementService _sysEntService;

        public SysEntController(ILogger<SysEntController> logger, RedisUtil client,
            ISysUserService sysUserService,
            ISysEntitlementService sysEntService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            _logger = logger;
            _sysEntService = sysEntService;
        }

        /// <summary>
        /// 查询权限集合
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("showTree")]
        [PermissionAuth(PermCode.MCH.ENT_UR_ROLE_DIST)]
        public ApiRes ShowTree()
        {
            //查询全部数据
            var sysEnt = _sysEntService.GetBySysType(CS.SYS_TYPE.MCH, null);

            //递归转换为树状结构
            //JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            //{
            //    Formatting = Formatting.Indented,
            //    ContractResolver = new CamelCasePropertyNamesContractResolver()
            //};
            var jsonArray = JArray.FromObject(sysEnt);
            var leftMenuTree = new TreeDataBuilder(jsonArray, "entId", "pid", "children", "entSort", true).BuildTreeObject();
            return ApiRes.Ok(leftMenuTree);
        }
    }
}

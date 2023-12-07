using AGooday.AgPay.Application.DataTransfer;
using AGooday.AgPay.Application.Interfaces;
using AGooday.AgPay.Application.Permissions;
using AGooday.AgPay.Common.Constants;
using AGooday.AgPay.Common.Models;
using AGooday.AgPay.Common.Utils;
using AGooday.AgPay.Domain.Core.Notifications;
using AGooday.AgPay.Merchant.Api.Attributes;
using AGooday.AgPay.Merchant.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace AGooday.AgPay.Merchant.Api.Controllers.SysUser
{
    /// <summary>
    /// 操作员列表
    /// </summary>
    [ApiController, Authorize]
    [Route("api/sysUsers")]
    public class SysUserController : CommonController
    {
        private readonly ILogger<SysUserController> _logger;
        private readonly ISysUserService _sysUserService;
        private readonly IMemoryCache _cache;
        // 将领域通知处理程序注入Controller
        private readonly DomainNotificationHandler _notifications;

        public SysUserController(ILogger<SysUserController> logger, IMemoryCache cache, INotificationHandler<DomainNotification> notifications, RedisUtil client,
            ISysUserService sysUserService,
            ISysRoleEntRelaService sysRoleEntRelaService,
            ISysUserRoleRelaService sysUserRoleRelaService)
            : base(logger, client, sysUserService, sysRoleEntRelaService, sysUserRoleRelaService)
        {
            _logger = logger;
            _sysUserService = sysUserService;
            _cache = cache;
            _notifications = (DomainNotificationHandler)notifications;
        }

        /// <summary>
        /// 操作员信息列表
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet, Route(""), NoLog]
        [PermissionAuth(PermCode.MCH.ENT_UR_USER_LIST)]
        public ApiPageRes<SysUserListDto> List([FromQuery] SysUserQueryDto dto)
        {
            dto.SysType = CS.SYS_TYPE.MCH;
            dto.BelongInfoId = GetCurrentMchNo();
            long? currentUserId = null;//GetCurrentUserId();
            var data = _sysUserService.GetPaginatedData(dto, currentUserId);
            return ApiPageRes<SysUserListDto>.Pages(data);
        }

        /// <summary>
        /// 添加操作员信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost, Route(""), MethodLog("添加操作员信息")]
        [PermissionAuth(PermCode.MCH.ENT_UR_USER_ADD)]
        public async Task<ApiRes> AddAsync(SysUserCreateDto dto)
        {
            //_cache.Remove("ErrorData");
            dto.IsAdmin = CS.NO;
            dto.SysType = CS.SYS_TYPE.MCH;
            dto.BelongInfoId = GetCurrentMchNo();
            dto.CreatedAt = DateTime.Now;
            dto.UpdatedAt = DateTime.Now;
            await _sysUserService.CreateAsync(dto);
            //var errorData = _cache.Get("ErrorData");
            //if (errorData == null)
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
                return ApiRes.Ok();
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 删除操作员
        /// </summary>
        /// <param name="recordId">系统用户ID</param>
        /// <returns></returns>
        [HttpDelete, Route("{recordId}"), MethodLog("删除操作员")]
        [PermissionAuth(PermCode.MCH.ENT_UR_USER_DELETE)]
        public async Task<ApiRes> DeleteAsync(long recordId)
        {
            var currentUserId = 0;
            //判断是否删除商户默认超管
            var dbRecord = _sysUserService.GetById(recordId, GetCurrentMchNo());
            if (dbRecord != null && dbRecord.SysType == CS.SYS_TYPE.MCH && dbRecord.IsAdmin == CS.YES)
            {
                return ApiRes.CustomFail("系统不允许删除商户默认用户！");
            }
            await _sysUserService.RemoveAsync(recordId, currentUserId, CS.SYS_TYPE.MCH);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
            {
                //如果用户被删除，需要更新redis数据
                RefAuthentication(new List<long> { recordId });
                return ApiRes.Ok();
            }
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 更新操作员信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut, Route("{recordId}"), MethodLog("更新操作员信息")]
        [PermissionAuth(PermCode.MCH.ENT_UR_USER_EDIT)]
        public async Task<ApiRes> UpdateAsync(long recordId, SysUserModifyDto dto)
        {
            dto.SysType = CS.SYS_TYPE.MCH;
            var dbRecord = _sysUserService.GetById(recordId, GetCurrentMchNo());
            if (dbRecord == null)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }
            if (!dto.SysUserId.HasValue || dto.SysUserId.Value <= 0)
            {
                var sysUser = _sysUserService.GetByKeyAsNoTracking(recordId);
                sysUser.State = dto.State.Value;
                CopyUtil.CopyProperties(sysUser, dto);
            }
            await _sysUserService.ModifyAsync(dto);
            // 是否存在消息通知
            if (!_notifications.HasNotifications())
            {
                if (dto.ResetPass.HasValue && dto.ResetPass.Value)
                {
                    // 删除用户redis缓存信息
                    DelAuthentication(new List<long> { dto.SysUserId.Value });
                }
                if (dto.State.HasValue && dto.State.Value.Equals(CS.PUB_DISABLE))
                {
                    //如果用户被禁用，需要更新redis数据
                    RefAuthentication(new List<long> { dto.SysUserId.Value });
                }
                return ApiRes.Ok();
            }
            else
                return ApiRes.CustomFail(_notifications.GetNotifications().Select(s => s.Value).ToArray());
        }

        /// <summary>
        /// 查看操作员信息
        /// </summary>
        /// <param name="recordId">系统用户ID</param>
        /// <returns></returns>
        [HttpGet, Route("{recordId}"), NoLog]
        [PermissionAuth(PermCode.MCH.ENT_UR_USER_EDIT)]
        public ApiRes Detail(long recordId)
        {
            var sysUser = _sysUserService.GetById(recordId);
            if (sysUser == null)
            {
                return ApiRes.Fail(ApiCode.SYS_OPERATION_FAIL_SELETE);
            }
            if (!sysUser.BelongInfoId.Equals(GetCurrentMchNo()))
            {
                return ApiRes.Fail(ApiCode.SYS_PERMISSION_ERROR);
            }
            return ApiRes.Ok(sysUser);
        }
    }
}
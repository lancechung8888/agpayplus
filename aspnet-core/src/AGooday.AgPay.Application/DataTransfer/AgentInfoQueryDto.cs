﻿using AGooday.AgPay.Common.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AGooday.AgPay.Application.DataTransfer
{
    /// <summary>
    /// 代理商信息表
    /// </summary>
    public class AgentInfoQueryDto : PageQuery
    {
        /// <summary>
        /// 代理商号
        /// </summary>
        public string AgentNo { get; set; }

        /// <summary>
        /// 代理商名称
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>
        /// 初始用户ID（创建代理商时，允许代理商登录的用户）
        /// </summary>
        public string LoginUsername { get; set; }

        /// <summary>
        /// 代理商类型: 1-个人, 2-企业
        /// </summary>
        [BindNever]
        public byte AgentType { get; set; }

        /// <summary>
        /// 上级代理商号
        /// </summary>
        public string Pid { get; set; }

        /// <summary>
        /// 服务商号
        /// </summary>
        public string IsvNo { get; set; }

        /// <summary>
        /// 联系人姓名
        /// </summary>
        [BindNever]
        public string ContactName { get; set; }

        /// <summary>
        /// 联系人手机号
        /// </summary>
        [BindNever]
        public string ContactTel { get; set; }

        /// <summary>
        /// 联系人邮箱
        /// </summary>
        [BindNever]
        public string ContactEmail { get; set; }

        /// <summary>
        /// 代理商状态: 0-停用, 1-正常
        /// </summary>
        public byte? State { get; set; }
    }
}

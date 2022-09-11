﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGooday.AgPay.Domain.Models
{
    /// <summary>
    /// 商户通知记录表
    /// </summary>
    [Table("t_mch_notify_record")]
    public class MchNotifyRecord
    {
        /// <summary>
        /// 商户通知记录ID
        /// </summary>
        [Key, Required, Column("notify_id", TypeName = "bigint")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]//自增列
        public long NotifyId { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        [Required, Column("order_id", TypeName = "varchar(64)")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单类型:1-支付,2-退款
        /// </summary>
        [Required, Column("order_type", TypeName = "tinyint(6)")]
        public byte OrderType { get; set; }

        /// <summary>
        /// 商户订单号
        /// </summary>
        [Required, Column("mch_order_no", TypeName = "varchar(64)")]
        public string MchOrderNo { get; set; }

        /// <summary>
        /// 商户号
        /// </summary>
        [Required, Column("mch_no", TypeName = "varchar(64)")]
        public string MchNo { get; set; }

        /// <summary>
        /// 服务商号
        /// </summary>
        [Column("isv_no", TypeName = "varchar(64)")]
        public string IsvNo { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        [Required, Column("app_id", TypeName = "varchar(64)")]
        public string AppId { get; set; }

        /// <summary>
        /// 通知地址
        /// </summary>
        [Required, Column("notify_url", TypeName = "varchar(128)")]
        public string NotifyUrl { get; set; }

        /// <summary>
        /// 通知响应结果
        /// </summary>
        [Column("res_result", TypeName = "text")]
        public string ResResult { get; set; }

        /// <summary>
        /// 通知次数
        /// </summary>
        [Required, Column("notify_count")]
        public int NotifyCount { get; set; }

        /// <summary>
        /// 最大通知次数, 默认6次
        /// </summary>
        [Required, Column("notify_count_limit")]
        public int NotifyCountLimit { get; set; }

        /// <summary>
        /// 通知状态,1-通知中,2-通知成功,3-通知失败
        /// </summary>
        [Required, Column("state", TypeName = "tinyint(6)")]
        public byte State { get; set; }

        /// <summary>
        /// 最后一次通知时间
        /// </summary>
        [Required, Column("last_notify_time")]
        public DateTime LastNotifyTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required, Column("created_at", TypeName = "timestamp")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Required, Column("updated_at", TypeName = "timestamp")]
        public DateTime UpdatedAt { get; set; }
    }
}

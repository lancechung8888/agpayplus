﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AGooday.AgPay.Domain.Models
{
    /// <summary>
    /// 阶梯费率信息表
    /// </summary>
    [Comment("阶梯费率信息表")]
    [Table("t_level_rate_config")]
    public class LevelRateConfig
    {
        /// <summary>
        /// ID
        /// </summary>
        [Comment("ID")]
        [Key, Required, Column("id", TypeName = "bigint")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]//自增列
        public long Id { get; set; }

        /// <summary>
        /// 支付费率配置ID
        /// </summary>
        [Comment("支付费率配置ID")]
        [Required, Column("rate_config_id", TypeName = "bigint")]
        public long RateConfigId { get; set; }

        /// <summary>
        /// 银行卡类型: DEBIT-借记卡（储蓄卡）, CREDIT-贷记卡（信用卡）
        /// </summary>
        [Comment("银行卡类型: DEBIT-借记卡（储蓄卡）, CREDIT-贷记卡（信用卡）")]
        [Required, Column("bank_card_type", TypeName = "varchar(20)")]
        public string BankCardType { get; set; }

        /// <summary>
        /// 最小金额: 计算时大于此值
        /// </summary>
        [Comment("最小金额: 计算时大于此值")]
        [Required, Column("min_amount", TypeName = "int")]
        public int MinAmount { get; set; }

        /// <summary>
        /// 最大金额: 计算时小于或等于此值
        /// </summary>
        [Comment("最大金额: 计算时小于或等于此值")]
        [Required, Column("max_amount", TypeName = "int")]
        public int MaxAmount { get; set; }

        /// <summary>
        /// 保底费用
        /// </summary>
        [Comment("保底费用")]
        [Required, Column("min_fee", TypeName = "int")]
        public int MinFee { get; set; }

        /// <summary>
        /// 封顶费用
        /// </summary>
        [Comment("封顶费用")]
        [Required, Column("max_fee", TypeName = "int")]
        public int MaxFee { get; set; }

        /// <summary>
        /// 支付方式费率
        /// </summary>
        [Comment("支付方式费率")]
        [Column("fee_rate", TypeName = "decimal(20,6)")]
        public decimal? FeeRate { get; set; }

        /// <summary>
        /// 状态: 0-停用, 1-启用
        /// </summary>
        [Comment("状态: 0-停用, 1-启用")]
        [Required, Column("state", TypeName = "tinyint(6)")]
        public byte State { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Comment("创建时间")]
        [Required, Column("created_at", TypeName = "timestamp(6)")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Comment("更新时间")]
        [Required, Column("updated_at", TypeName = "timestamp(6)")]
        public DateTime UpdatedAt { get; set; }
    }
}

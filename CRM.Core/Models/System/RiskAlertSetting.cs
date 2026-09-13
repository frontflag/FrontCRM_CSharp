using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.System;

[Table("risk_alert_setting")]
public class RiskAlertSetting
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public string Id { get; set; } = RiskAlertSettingIds.Default;

    [Column("inventory_amount_usd_max", TypeName = "numeric(18,2)")]
    public decimal InventoryAmountUsdMax { get; set; }

    [Column("stock_age_days_max")]
    public int StockAgeDaysMax { get; set; } = RiskAlertLimits.DefaultAgeDays;

    [Column("receivable_amount_usd_max", TypeName = "numeric(18,2)")]
    public decimal ReceivableAmountUsdMax { get; set; }

    [Column("customer_receivable_usd_max", TypeName = "numeric(18,2)")]
    public decimal CustomerReceivableUsdMax { get; set; }

    [Column("so_receivable_age_days_max")]
    public int SoReceivableAgeDaysMax { get; set; } = RiskAlertLimits.DefaultAgeDays;

    [Column("create_time")]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [Column("modify_time")]
    public DateTime ModifyTime { get; set; } = DateTime.UtcNow;
}

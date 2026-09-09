using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance;

[Table("commission_dynamic")]
public class CommissionDynamic : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("role_type")]
    public short RoleType { get; set; }

    [Column("stock_out_item_id")]
    [StringLength(36)]
    public string StockOutItemId { get; set; } = string.Empty;

    [Column("user_id")]
    [StringLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Column("user_name")]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Column("user_level")]
    public short UserLevel { get; set; }

    [Column("version_id")]
    [StringLength(36)]
    public string? VersionId { get; set; }

    [Column("rate_points", TypeName = "numeric(5,2)")]
    public decimal RatePoints { get; set; }

    [Column("gp_usd", TypeName = "numeric(18,2)")]
    public decimal GpUsd { get; set; }

    [Column("period_gp_usd", TypeName = "numeric(18,2)")]
    public decimal PeriodGpUsd { get; set; }

    [Column("commission_usd", TypeName = "numeric(18,2)")]
    public decimal CommissionUsd { get; set; }

    [Column("sell_order_id")]
    [StringLength(36)]
    public string? SellOrderId { get; set; }

    [Column("sell_order_code")]
    [StringLength(32)]
    public string? SellOrderCode { get; set; }

    [Column("sell_order_item_id")]
    [StringLength(36)]
    public string? SellOrderItemId { get; set; }

    [Column("sell_order_item_code")]
    [StringLength(64)]
    public string? SellOrderItemCode { get; set; }

    [Column("purchase_order_id")]
    [StringLength(36)]
    public string? PurchaseOrderId { get; set; }

    [Column("purchase_order_code")]
    [StringLength(32)]
    public string? PurchaseOrderCode { get; set; }

    [Column("stock_out_id")]
    [StringLength(36)]
    public string StockOutId { get; set; } = string.Empty;

    [Column("stock_out_code")]
    [StringLength(32)]
    public string StockOutCode { get; set; } = string.Empty;

    [Column("stock_out_date")]
    public DateOnly? StockOutDate { get; set; }

    [Column("receipt_date")]
    public DateOnly ReceiptDate { get; set; }

    [Column("pool_date")]
    public DateOnly PoolDate { get; set; }

    [Column("calc_month")]
    [StringLength(7)]
    public string CalcMonth { get; set; } = string.Empty;

    [Column("entry_kind")]
    public short EntryKind { get; set; }

    [Column("calc_date")]
    public DateOnly CalcDate { get; set; }

    [Column("calc_at")]
    public DateTime CalcAt { get; set; }
}

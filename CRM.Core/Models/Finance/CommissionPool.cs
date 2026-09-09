using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance;

[Table("commission_pool")]
public class CommissionPool : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("stock_out_item_id")]
    [StringLength(36)]
    public string StockOutItemId { get; set; } = string.Empty;

    [Column("stock_out_id")]
    [StringLength(36)]
    public string StockOutId { get; set; } = string.Empty;

    [Column("stock_out_code")]
    [StringLength(32)]
    public string StockOutCode { get; set; } = string.Empty;

    [Column("stock_out_date")]
    public DateOnly? StockOutDate { get; set; }

    [Column("sales_user_id")]
    [StringLength(36)]
    public string? SalesUserId { get; set; }

    [Column("purchase_user_id")]
    [StringLength(36)]
    public string? PurchaseUserId { get; set; }

    [Column("gp_usd", TypeName = "numeric(18,2)")]
    public decimal GpUsd { get; set; }

    [Column("receipt_progress_status")]
    public short ReceiptProgressStatus { get; set; }

    [Column("receipt_date")]
    public DateOnly? ReceiptDate { get; set; }

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

    [Column("purchase_order_item_id")]
    [StringLength(36)]
    public string? PurchaseOrderItemId { get; set; }

    [Column("purchase_order_item_code")]
    [StringLength(64)]
    public string? PurchaseOrderItemCode { get; set; }

    /// <summary>0 未提成 / 1 已提成。</summary>
    [Column("sales_commission_status")]
    public short SalesCommissionStatus { get; set; }

    /// <summary>0 未提成 / 1 已提成 / 2 不适用。</summary>
    [Column("purchase_commission_status")]
    public short PurchaseCommissionStatus { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

/// <summary>进项发票与入库明细核销流水（账本真相）。</summary>
[Table("finance_purchase_invoice_write_off")]
public class FinancePurchaseInvoiceWriteOff : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("FinancePurchaseInvoiceWriteOffId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("finance_purchase_invoice_id")]
    public string FinancePurchaseInvoiceId { get; set; } = string.Empty;

    /// <summary>预留；MVP 恒为 null。</summary>
    [StringLength(36)]
    [Column("finance_purchase_invoice_item_id")]
    public string? FinancePurchaseInvoiceItemId { get; set; }

    [Required]
    [StringLength(36)]
    [Column("stock_in_item_id")]
    public string StockInItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("stock_in_id")]
    public string StockInId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("purchase_order_item_id")]
    public string? PurchaseOrderItemId { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>币别 1 RMB / 2 USD / 3 EUR</summary>
    public byte Currency { get; set; } = 1;

    [StringLength(36)]
    [Column("operator_user_id")]
    public string? OperatorUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

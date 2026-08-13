using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

/// <summary>销项发票与应收款匹配流水（账本真相：发票头 ↔ finance_receivable_id）。</summary>
[Table("finance_sell_invoice_write_off")]
public class FinanceSellInvoiceWriteOff : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("FinanceSellInvoiceWriteOffId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("finance_sell_invoice_id")]
    public string FinanceSellInvoiceId { get; set; } = string.Empty;

    /// <summary>预留；MVP 恒为 null。</summary>
    [StringLength(36)]
    [Column("finance_sell_invoice_item_id")]
    public string? FinanceSellInvoiceItemId { get; set; }

    [Required]
    [StringLength(36)]
    [Column("finance_receivable_id")]
    public string FinanceReceivableId { get; set; } = string.Empty;

    /// <summary>冗余自应收，便于展示出库。</summary>
    [StringLength(36)]
    [Column("stock_out_id")]
    public string? StockOutId { get; set; }

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

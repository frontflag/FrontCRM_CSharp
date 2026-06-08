using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Finance;

[Table("finance_customer_advance_ledger")]
public class FinanceCustomerAdvanceLedger : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("FinanceCustomerAdvanceLedgerId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("finance_customer_advance_id")]
    public string FinanceCustomerAdvanceId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    public short Currency { get; set; } = 1;

    /// <summary>10入账 20冲应收 30超额转预收 40退款</summary>
    [Column("ledger_type")]
    public short LedgerType { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(36)]
    [Column("finance_receipt_id")]
    public string? FinanceReceiptId { get; set; }

    [StringLength(36)]
    [Column("finance_receipt_item_id")]
    public string? FinanceReceiptItemId { get; set; }

    [StringLength(36)]
    [Column("finance_receivable_id")]
    public string? FinanceReceivableId { get; set; }

    [StringLength(36)]
    [Column("finance_receivable_write_off_id")]
    public string? FinanceReceivableWriteOffId { get; set; }

    [StringLength(36)]
    [Column("sell_order_id")]
    public string? SellOrderId { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [StringLength(36)]
    [Column("operator_user_id")]
    public string? OperatorUserId { get; set; }
}

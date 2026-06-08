using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;

namespace CRM.Core.Models.Finance;

/// <summary>应收款与收款明细/预收池核销记录。</summary>
[Table("finance_receivable_write_off")]
public class FinanceReceivableWriteOff : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("FinanceReceivableWriteOffId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("finance_receivable_id")]
    public string FinanceReceivableId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("finance_receipt_id")]
    public string? FinanceReceiptId { get; set; }

    [StringLength(36)]
    [Column("finance_receipt_item_id")]
    public string? FinanceReceiptItemId { get; set; }

    /// <summary>10=收款明细 20=预收池</summary>
    [Column("write_off_source")]
    public short WriteOffSource { get; set; } = FinanceReceivableWriteOffSourceCode.ReceiptItem;

    [StringLength(36)]
    [Column("finance_customer_advance_ledger_id")]
    public string? FinanceCustomerAdvanceLedgerId { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(36)]
    [Column("operator_user_id")]
    public string? OperatorUserId { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

[Table("finance_freight_forwarder_payment")]
public class FinanceFreightForwarderPayment : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("FinanceFfPaymentId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string FinanceReceiptId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string FreightForwarderCompanyId { get; set; } = string.Empty;

    [Column(TypeName = "numeric(18,2)")]
    public decimal PaymentAmount { get; set; }

    public byte PaymentCurrency { get; set; } = 1;

    public short PaymentMode { get; set; } = 1;

    [StringLength(36)]
    public string? CompanyBankId { get; set; }

    [StringLength(36)]
    public string? FfCompanyBankId { get; set; }

    [StringLength(100)]
    public string? BankSlipNo { get; set; }

    public DateTime? PaymentDate { get; set; }

    [StringLength(36)]
    public string? PaymentUserId { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [NotMapped]
    public string? FreightForwarderCompanyName { get; set; }

    [NotMapped]
    public string? FfCompanyBankName { get; set; }

    [NotMapped]
    public string? CompanyBankName { get; set; }

    [NotMapped]
    public string? PaymentUserName { get; set; }
}

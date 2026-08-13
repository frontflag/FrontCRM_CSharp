using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

[Table("finance_customer_advance")]
public class FinanceCustomerAdvance : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("FinanceCustomerAdvanceId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("customer_name")]
    public string? CustomerName { get; set; }

    /// <summary>客户英文名称（列表/导出填充，不落库）</summary>
    [NotMapped]
    public string? CustomerEnglishName { get; set; }

    public short Currency { get; set; } = 1;

    [Column("balance", TypeName = "numeric(18,2)")]
    public decimal Balance { get; set; }

    [Column("total_in", TypeName = "numeric(18,2)")]
    public decimal TotalIn { get; set; }

    [Column("total_applied", TypeName = "numeric(18,2)")]
    public decimal TotalApplied { get; set; }

    [Column("total_refund", TypeName = "numeric(18,2)")]
    public decimal TotalRefund { get; set; }

    [StringLength(36)]
    [Column("sales_user_id")]
    public string? SalesUserId { get; set; }

    [StringLength(36)]
    public string? CreateByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Quote;

[Table("customer_quote")]
public class CustomerQuote : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("CustomerQuoteId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("group_id")]
    public string GroupId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    [Column("customer_quote_code")]
    public string CustomerQuoteCode { get; set; } = string.Empty;

    [Column("version_no")]
    public int VersionNo { get; set; } = 1;

    [Column("status")]
    public short Status { get; set; } = CustomerQuoteStatus.Unsent;

    [StringLength(36)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    [StringLength(36)]
    [Column("customer_contact_id")]
    public string? CustomerContactId { get; set; }

    [StringLength(100)]
    [Column("contact_name")]
    public string? ContactName { get; set; }

    [StringLength(200)]
    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [StringLength(36)]
    [Column("sales_user_id")]
    public string? SalesUserId { get; set; }

    [Column("profit_factor", TypeName = "numeric(8,2)")]
    public decimal ProfitFactor { get; set; } = 1.00m;

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("sent_by_email")]
    public bool SentByEmail { get; set; }

    [StringLength(36)]
    [Column("previous_version_id")]
    public string? PreviousVersionId { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [NotMapped]
    public string? CustomerName { get; set; }

    [NotMapped]
    public string? SalesUserName { get; set; }

    [NotMapped]
    public string DisplayCode => $"{CustomerQuoteCode}-{VersionNo}";

    public virtual ICollection<CustomerQuoteItem> Items { get; set; } = new List<CustomerQuoteItem>();
}

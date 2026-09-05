using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Quote;

[Table("customer_quote_item")]
public class CustomerQuoteItem : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("CustomerQuoteItemId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("customer_quote_id")]
    public string CustomerQuoteId { get; set; } = string.Empty;

    [Column("line_no")]
    public int LineNo { get; set; }

    [Required]
    [StringLength(36)]
    [Column("source_quote_item_id")]
    public string SourceQuoteItemId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("source_quote_id")]
    public string? SourceQuoteId { get; set; }

    [StringLength(36)]
    [Column("rfq_item_id")]
    public string? RfqItemId { get; set; }

    [StringLength(200)]
    [Column("mpn")]
    public string? Mpn { get; set; }

    [StringLength(200)]
    [Column("brand")]
    public string? Brand { get; set; }

    [Column("quantity", TypeName = "numeric(18,4)")]
    public decimal Quantity { get; set; }

    [Column("purchase_price", TypeName = "numeric(18,6)")]
    public decimal PurchasePrice { get; set; }

    [Column("purchase_currency")]
    public short PurchaseCurrency { get; set; } = 1;

    [Column("send_price", TypeName = "numeric(18,6)")]
    public decimal SendPrice { get; set; }

    [Column("send_currency")]
    public short SendCurrency { get; set; } = 1;

    [Column("is_locked")]
    public bool IsLocked { get; set; }

    [StringLength(200)]
    [Column("customer_mpn")]
    public string? CustomerMpn { get; set; }

    [StringLength(200)]
    [Column("customer_brand")]
    public string? CustomerBrand { get; set; }

    [StringLength(200)]
    [Column("lead_time")]
    public string? LeadTime { get; set; }

    [StringLength(100)]
    [Column("date_code")]
    public string? DateCode { get; set; }

    [StringLength(500)]
    [Column("remark")]
    public string? Remark { get; set; }

    [StringLength(32)]
    [Column("source_quote_code")]
    public string? SourceQuoteCode { get; set; }

    [Column("source_quote_date")]
    public DateTime? SourceQuoteDate { get; set; }

    [StringLength(36)]
    [Column("purchase_user_id")]
    public string? PurchaseUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [NotMapped]
    public string? PurchaseUserName { get; set; }

    public virtual CustomerQuote? CustomerQuote { get; set; }
}

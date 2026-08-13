using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Finance;

/// <summary>应收款（销售出库批次维度，一出库单头一行）。</summary>
[Table("finance_receivable")]
public class FinanceReceivable : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("FinanceReceivableId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [StringLength(16)]
    public string? ReceivableCode { get; set; }

    [Required]
    [StringLength(36)]
    [Column("stock_out_id")]
    public string StockOutId { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string StockOutCode { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("sell_order_id")]
    public string SellOrderId { get; set; } = string.Empty;

    [StringLength(32)]
    [Column("sell_order_code")]
    public string? SellOrderCode { get; set; }

    [Required]
    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string SellOrderItemId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("customer_id")]
    public string CustomerId { get; set; } = string.Empty;

    [StringLength(200)]
    [Column("customer_name")]
    public string? CustomerName { get; set; }

    /// <summary>客户英文名（详情/列表 API 填充，不落库）</summary>
    [NotMapped]
    public string? CustomerEnglishName { get; set; }

    [StringLength(36)]
    [Column("sales_user_id")]
    public string? SalesUserId { get; set; }

    [StringLength(200)]
    public string? PN { get; set; }

    [StringLength(200)]
    public string? Brand { get; set; }

    [Column("outbound_qty", TypeName = "numeric(18,4)")]
    public decimal OutboundQty { get; set; }

    [Column("unit_price", TypeName = "numeric(18,6)")]
    public decimal UnitPrice { get; set; }

    /// <summary>币别 1=RMB 2=USD 3=EUR</summary>
    public short Currency { get; set; } = 1;

    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }

    [Column("verified_done", TypeName = "numeric(18,2)")]
    public decimal VerifiedDone { get; set; }

    [Column("verified_to_be", TypeName = "numeric(18,2)")]
    public decimal VerifiedToBe { get; set; }

    /// <summary>核销状态 0未核销 1部分核销 2核销完成</summary>
    [Column("verification_status")]
    public short VerificationStatus { get; set; }

    /// <summary>已匹配开票金额（票↔应收）</summary>
    [Column("invoice_match_done", TypeName = "numeric(18,2)")]
    public decimal InvoiceMatchDone { get; set; }

    /// <summary>待匹配开票金额</summary>
    [Column("invoice_match_to_be", TypeName = "numeric(18,2)")]
    public decimal InvoiceMatchToBe { get; set; }

    /// <summary>开票匹配状态 0未匹配 1部分 2完成</summary>
    [Column("invoice_match_status")]
    public short InvoiceMatchStatus { get; set; }

    /// <summary>开票匹配币别（可选冗余）</summary>
    [Column("invoice_match_currency")]
    public short? InvoiceMatchCurrency { get; set; }

    [Column("stock_out_date")]
    public DateTime? StockOutDate { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [StringLength(36)]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    public string? ModifyByUserId { get; set; }
}

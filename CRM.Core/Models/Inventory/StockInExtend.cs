using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory;

/// <summary>入库单主单级扩展（1:1 stockin），维护明细行序号水位。</summary>
[Table("stock_in_extend")]
public class StockInExtend : ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("StockInId")]
    public string StockInId { get; set; } = string.Empty;

    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    /// <summary>整单已匹配进项（Σ 明细）</summary>
    [Column("invoice_match_done", TypeName = "numeric(18,2)")]
    public decimal InvoiceMatchDone { get; set; }

    /// <summary>整单待匹配进项（Σ 明细）</summary>
    [Column("invoice_match_to_be", TypeName = "numeric(18,2)")]
    public decimal InvoiceMatchToBe { get; set; }

    /// <summary>整单进项匹配状态 0/1/2</summary>
    [Column("invoice_match_status")]
    public short InvoiceMatchStatus { get; set; }

    /// <summary>整单匹配币别缓存</summary>
    [Column("invoice_match_currency")]
    public byte? InvoiceMatchCurrency { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

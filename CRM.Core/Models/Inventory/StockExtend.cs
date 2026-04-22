using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Inventory;

/// <summary>库存分桶主表扩展（1:1 <see cref="StockInfo"/>），维护在库明细 <c>stock_item_code</c> 行序号水位。</summary>
[Table("stock_extend")]
public class StockExtend
{
    [Key]
    [StringLength(36)]
    [Column("StockId")]
    public string StockId { get; set; } = string.Empty;

    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }
}

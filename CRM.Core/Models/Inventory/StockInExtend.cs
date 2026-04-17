using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Inventory;

/// <summary>入库单主单级扩展（1:1 stockin），维护明细行序号水位。</summary>
[Table("stockinextend")]
public class StockInExtend
{
    [Key]
    [StringLength(36)]
    [Column("StockInId")]
    public string StockInId { get; set; } = string.Empty;

    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }
}

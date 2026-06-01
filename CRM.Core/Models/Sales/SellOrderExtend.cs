using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Sales;

/// <summary>销售订单主单级扩展（1:1 sellorder），维护明细行序号水位。</summary>
[Table("sellorderextend")]
public class SellOrderExtend : ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("SellOrderId")]
    public string SellOrderId { get; set; } = string.Empty;

    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

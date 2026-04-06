using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Purchase;

/// <summary>采购订单主单级扩展（1:1 purchaseorder），维护明细行序号水位。</summary>
[Table("purchaseorderextend")]
public class PurchaseOrderExtend
{
    [Key]
    [StringLength(36)]
    [Column("PurchaseOrderId")]
    public string PurchaseOrderId { get; set; } = string.Empty;

    [Column("last_item_line_seq")]
    public int LastItemLineSeq { get; set; }

    [Column("CreateTime")]
    public DateTime CreateTime { get; set; }

    [Column("ModifyTime")]
    public DateTime? ModifyTime { get; set; }
}

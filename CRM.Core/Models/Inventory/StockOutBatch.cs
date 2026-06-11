using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Inventory;

/// <summary>
/// 出库批次：关联装箱单，引用入库批次全局编号并记录出库数量。
/// </summary>
[Table("stock_out_batch")]
public class StockOutBatch : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    [Column("packing_id")]
    public string PackingId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Column("global_batch_no")]
    public string GlobalBatchNo { get; set; } = string.Empty;

    [Column("out_qty")]
    public int OutQty { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

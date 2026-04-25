using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Models;

namespace CRM.Core.Models.Inventory;

/// <summary>手工移库主表 <c>stocktransfer_manual</c>。</summary>
[Table("stocktransfer_manual")]
public class StockTransferManual : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("StockTransferManualId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(32)]
    public string TransferCode { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string FromWarehouseId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string ToWarehouseId { get; set; } = string.Empty;

    public short Status { get; set; }

    [StringLength(500)]
    public string? Remark { get; set; }

    public DateTime? ConfirmedTime { get; set; }

    [StringLength(36)]
    public string? ConfirmedByUserId { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    public ICollection<StockTransferItemManual> Items { get; set; } = new List<StockTransferItemManual>();
}

/// <summary>手工移库明细 <c>stocktransfer_item_manual</c>。</summary>
[Table("stocktransfer_item_manual")]
public class StockTransferItemManual : BaseGuidEntity
{
    [Key]
    [StringLength(36)]
    [Column("StockTransferItemManualId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(36)]
    public string StockTransferManualId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    public string SourceStockItemId { get; set; } = string.Empty;

    [StringLength(36)]
    public string? TargetStockItemId { get; set; }

    public int Qty { get; set; }

    [ForeignKey(nameof(StockTransferManualId))]
    public StockTransferManual? StockTransferManual { get; set; }
}

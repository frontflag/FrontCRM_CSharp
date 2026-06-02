using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Customs;

/// <summary>待报关列表：与销售出库通知 1:1，无独立业务单号。</summary>
[Table("customs_pendlist")]
public class CustomsPendlist : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("id")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>销售出库通知 <c>stockout_notify.ID</c>（StockOutType=10）。</summary>
    [Required]
    [StringLength(36)]
    [Column("sales_stockout_notify_id")]
    public string SalesStockOutNotifyId { get; set; } = string.Empty;

    [Required]
    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string SellOrderItemId { get; set; } = string.Empty;

    public int Qty { get; set; }

    public short Status { get; set; } = CustomsPendlistStatusCode.Open;

    /// <summary>报关出库通知 <c>stockout_notify.ID</c>（StockOutType=20）。</summary>
    [StringLength(36)]
    [Column("customs_stockout_notify_id")]
    public string? CustomsStockOutNotifyId { get; set; }

    /// <summary>创建时快照主境外仓（辅助列表）。</summary>
    [StringLength(36)]
    [Column("overseas_warehouse_id")]
    public string? OverseasWarehouseId { get; set; }

    [StringLength(36)]
    [Column("create_by_user_id")]
    public string? CreateByUserId { get; set; }

    [StringLength(36)]
    [Column("modify_by_user_id")]
    public string? ModifyByUserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

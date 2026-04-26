using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory;

/// <summary>
/// 入库明细扩展：与 <see cref="StockInItem"/> 一对一（主键同明细 <c>ItemId</c>）。
/// 冗余 <see cref="StockInItem.StockInId"/> 便于按单查询；采销订单行主键与业务编号按入库明细行存储。
/// </summary>
[Table("stock_in_item_extend")]
public class StockInItemExtend : BaseGuidEntity, ISoftDeletable
{
    [Key]
    [StringLength(36)]
    [Column("StockInItemId")]
    public override string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>入库单主键（与 <c>stockinitem.StockInId</c> 一致）。</summary>
    [Required]
    [StringLength(36)]
    [Column("StockInId")]
    public string StockInId { get; set; } = string.Empty;

    [StringLength(36)]
    [Column("sell_order_item_id")]
    public string? SellOrderItemId { get; set; }

    [StringLength(64)]
    [Column("sell_order_item_code")]
    public string? SellOrderItemCode { get; set; }

    [StringLength(36)]
    [Column("purchase_order_item_id")]
    public string? PurchaseOrderItemId { get; set; }

    [StringLength(64)]
    [Column("purchase_order_item_code")]
    public string? PurchaseOrderItemCode { get; set; }

    /// <summary>入库明细（主键与 <see cref="Id"/> / <c>StockInItemId</c> 相同）。</summary>
    [JsonIgnore]
    public virtual StockInItem? StockInItem { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

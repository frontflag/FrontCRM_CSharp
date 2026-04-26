using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory
{
    [Table("warehouseinfo")]
    public class WarehouseInfo : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(32)]
        public string WarehouseCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string WarehouseName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        /// <summary>地域类型 RegionType：10=境内 20=境外</summary>
        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        public short Status { get; set; } = 1;
    }

    [Table("warehousezone")]
    public class WarehouseZone : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        [StringLength(32)]
        public string ZoneCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string ZoneName { get; set; } = string.Empty;

        public short Status { get; set; } = 1;
    }

    [Table("warehouselocation")]
    public class WarehouseLocation : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(36)]
        public string ZoneId { get; set; } = string.Empty;

        [StringLength(32)]
        public string LocationCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string LocationName { get; set; } = string.Empty;

        public short Status { get; set; } = 1;
    }

    [Table("warehouseshelf")]
    public class WarehouseShelf : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(36)]
        public string LocationId { get; set; } = string.Empty;

        [StringLength(32)]
        public string ShelfCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string ShelfName { get; set; } = string.Empty;

        public short Status { get; set; } = 1;
    }

    [Table("stockledger")]
    public class InventoryLedger : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(20)]
        public string BizType { get; set; } = string.Empty; // STOCK_IN/STOCK_OUT/COUNT_ADJUST

        [StringLength(36)]
        public string BizId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? BizLineId { get; set; }

        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? LocationId { get; set; }

        [StringLength(50)]
        public string? BatchNo { get; set; }

        public int QtyIn { get; set; }

        public int QtyOut { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>采购订单明细业务编号（写入时自 <c>stock</c> 冗余）</summary>
        [StringLength(64)]
        public string? PurchaseOrderItemCode { get; set; }

        /// <summary>采购订单明细主键</summary>
        [StringLength(36)]
        public string? PurchaseOrderItemId { get; set; }

        /// <summary>销售订单明细业务编号</summary>
        [StringLength(64)]
        public string? SellOrderItemCode { get; set; }

        /// <summary>销售订单明细主键</summary>
        [StringLength(36)]
        public string? SellOrderItemId { get; set; }

        [StringLength(500)]
        public string? Remark { get; set; }

        /// <summary>移库：源仓（与 <c>STOCK_TRANS</c> 合并流水配合）。</summary>
        [StringLength(36)]
        [Column("from_warehouse_id")]
        public string? FromWarehouseId { get; set; }

        /// <summary>移库：目标仓。</summary>
        [StringLength(36)]
        [Column("to_warehouse_id")]
        public string? ToWarehouseId { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        [StringLength(36)]
        [Column("customs_declaration_id")]
        public string? CustomsDeclarationId { get; set; }

        [StringLength(36)]
        [Column("stock_transfer_id")]
        public string? StockTransferId { get; set; }

        [StringLength(36)]
        [Column("source_stock_item_id")]
        public string? SourceStockItemId { get; set; }

        [StringLength(36)]
        [Column("target_stock_item_id")]
        public string? TargetStockItemId { get; set; }
    }

    [Table("pickingtask")]
    public class PickingTask : BaseGuidEntity, ISoftDeletable
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(32)]
        public string TaskCode { get; set; } = string.Empty;

        [StringLength(36)]
        public string StockOutRequestId { get; set; } = string.Empty;

        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        [StringLength(36)]
        public string OperatorId { get; set; } = string.Empty;

        /// <summary>1待拣货 2拣货中 100已完成 -1已取消</summary>
        public short Status { get; set; } = 1;

        [StringLength(500)]
        public string? Remark { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        public ICollection<PickingTaskItem> Items { get; set; } = new List<PickingTaskItem>();
    }

    [Table("pickingtaskitem")]
    public class PickingTaskItem : BaseGuidEntity, ISoftDeletable
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(36)]
        public string PickingTaskId { get; set; } = string.Empty;

        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? StockId { get; set; }

        /// <summary>在库明细 <c>stockitem.StockItemId</c>；新流程拣货必填。</summary>
        [StringLength(36)]
        [Column("stock_item_id")]
        public string? StockItemId { get; set; }

        [StringLength(50)]
        public string? BatchNo { get; set; }

        [StringLength(36)]
        public string? LocationId { get; set; }

        public int PlanQty { get; set; }

        public int PickedQty { get; set; }

        /// <summary>
        /// true：来自备货库存补充（销售关联采购类型之外的备货批次，且物料型号/品牌与销单行一致）；false：来自与出库通知销售明细关联采购单类型一致的库存。
        /// </summary>
        public bool IsStockingSupplement { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        public PickingTask? PickingTask { get; set; }
    }

    [Table("inventorycountplan")]
    public class InventoryCountPlan : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(7)]
        public string PlanMonth { get; set; } = string.Empty; // yyyy-MM

        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        [StringLength(36)]
        public string CreatorId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? SubmitterId { get; set; }

        /// <summary>1草稿 10盘点中 100已完成 -1已取消</summary>
        public short Status { get; set; } = 1;

        [StringLength(500)]
        public string? Remark { get; set; }

        public ICollection<InventoryCountItem> Items { get; set; } = new List<InventoryCountItem>();
    }

    [Table("inventorycountitem")]
    public class InventoryCountItem : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("Id")]
        public override string Id { get; set; } = string.Empty;

        [StringLength(36)]
        public string PlanId { get; set; } = string.Empty;

        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        [StringLength(36)]
        public string? LocationId { get; set; }

        public int BookQty { get; set; }

        public int CountQty { get; set; }

        public int DiffQty { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal BookAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal CountAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal DiffAmount { get; set; }

        public InventoryCountPlan? Plan { get; set; }
    }
}


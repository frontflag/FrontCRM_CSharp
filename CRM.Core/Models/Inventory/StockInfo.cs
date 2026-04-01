using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 库存主表
    /// </summary>
    [Table("stock")]
    public class StockInfo : BaseGuidEntity
    {
        /// <summary>
        /// 库存ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("StockId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 物料ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        /// <summary>
        /// 仓库ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        /// <summary>
        /// 库位ID
        /// </summary>
        [StringLength(36)]
        public string? LocationId { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [StringLength(50)]
        public string? BatchNo { get; set; }

        /// <summary>
        /// 生产日期
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 过期日期
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// 库存数量（旧字段，等价于当前库存）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 可用数量（旧字段，等价于可用库存）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal AvailableQuantity { get; set; } = 0.0000m;

        /// <summary>
        /// 锁定数量（旧字段，用于占用）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal LockedQuantity { get; set; } = 0.0000m;

        /// <summary>
        /// 总入库数量（累计入库量，文档中的 Qty）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Qty { get; set; } = 0.0000m;

        /// <summary>
        /// 已出库数量（文档中的 QtyStockOut）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOut { get; set; } = 0.0000m;

        /// <summary>
        /// 拣货占用数量（文档中的 QtyOccupy）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyOccupy { get; set; } = 0.0000m;

        /// <summary>
        /// 销售预占数量（文档中的 QtySales）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtySales { get; set; } = 0.0000m;

        /// <summary>
        /// 当前库存数量（QtyRepertory = Qty - QtyStockOut）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyRepertory { get; set; } = 0.0000m;

        /// <summary>
        /// 可用库存数量（QtyRepertoryAvailable = QtyRepertory - QtyOccupy - QtySales）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyRepertoryAvailable { get; set; } = 0.0000m;

        /// <summary>
        /// 状态 (1:正常 0:冻结)
        /// </summary>
        public short Status { get; set; } = 1;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 入库单主表
    /// </summary>
    [Table("stockin")]
    public class StockIn : BaseGuidEntity
    {
        /// <summary>
        /// 入库单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("StockInId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 入库单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string StockInCode { get; set; } = string.Empty;

        /// <summary>
        /// 入库类型 (1:采购入库 2:退货入库 3:调拨入库 4:其他入库)
        /// </summary>
        public short StockInType { get; set; } = 1;

        /// <summary>
        /// 来源单号
        /// </summary>
        [StringLength(32)]
        public string? SourceCode { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        /// <summary>
        /// 供应商ID
        /// </summary>
        [StringLength(36)]
        public string? VendorId { get; set; }

        /// <summary>
        /// 入库日期
        /// </summary>
        public DateTime StockInDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 来源单ID（例如采购订单ID）
        /// </summary>
        [StringLength(36)]
        public string? SourceId { get; set; }

        /// <summary>
        /// 入库总数
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal TotalQuantity { get; set; } = 0.0000m;

        /// <summary>
        /// 入库金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 状态 (0:草稿 1:待入库 2:已入库 3:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 质检状态 (0:未质检 1:合格 2:不合格)
        /// </summary>
        public short InspectStatus { get; set; } = 0;

        /// <summary>
        /// 创建人
        /// </summary>
        [StringLength(36)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 审核人
        /// </summary>
        [StringLength(36)]
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ApprovedTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<StockInItem> Items { get; set; } = new List<StockInItem>();
    }

    /// <summary>
    /// 入库单明细表
    /// </summary>
    [Table("stockinitem")]
    public class StockInItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 入库单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string StockInId { get; set; } = string.Empty;

        /// <summary>
        /// 物料ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        /// <summary>
        /// 入库数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 订单数量（来源订单应收数量）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal OrderQty { get; set; } = 0.0000m;

        /// <summary>
        /// 累计已入库数量（多次部分入库）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyReceived { get; set; } = 0.0000m;

        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal Price { get; set; } = 0.000000m;

        /// <summary>
        /// 金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// 库位ID
        /// </summary>
        [StringLength(36)]
        public string? LocationId { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [StringLength(50)]
        public string? BatchNo { get; set; }

        /// <summary>
        /// 生产日期
        /// </summary>
        public DateTime? ProductionDate { get; set; }

        /// <summary>
        /// 过期日期
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// 是否质检合格
        /// </summary>
        public bool IsQualified { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("StockInId")]
        public virtual StockIn? StockIn { get; set; }
    }

    /// <summary>
    /// 出库单主表
    /// </summary>
    [Table("stockout")]
    public class StockOut : BaseGuidEntity
    {
        /// <summary>
        /// 出库单ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("StockOutId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 出库单号
        /// </summary>
        [Required]
        [StringLength(32)]
        public string StockOutCode { get; set; } = string.Empty;

        /// <summary>
        /// 出库类型 (1:销售出库 2:退货出库 3:调拨出库 4:其他出库)
        /// </summary>
        public short StockOutType { get; set; } = 1;

        /// <summary>
        /// 来源单号
        /// </summary>
        [StringLength(32)]
        public string? SourceCode { get; set; }

        /// <summary>
        /// 来源单ID
        /// </summary>
        [StringLength(36)]
        public string? SourceId { get; set; }

        /// <summary>
        /// 仓库ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string WarehouseId { get; set; } = string.Empty;

        /// <summary>
        /// 客户ID
        /// </summary>
        [StringLength(36)]
        public string? CustomerId { get; set; }

        /// <summary>
        /// 出库日期
        /// </summary>
        public DateTime StockOutDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 出库总数
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal TotalQuantity { get; set; } = 0.0000m;

        /// <summary>
        /// 出库金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalAmount { get; set; } = 0.00m;

        /// <summary>
        /// 状态 (0:草稿 1:待出库 2:已出库 3:已取消)
        /// </summary>
        public short Status { get; set; } = 0;

        /// <summary>
        /// 拣货人
        /// </summary>
        [StringLength(36)]
        public string? PickerId { get; set; }

        /// <summary>
        /// 拣货完成时间
        /// </summary>
        public DateTime? PickedTime { get; set; }

        /// <summary>
        /// 出库确认人
        /// </summary>
        [StringLength(36)]
        public string? ConfirmedBy { get; set; }

        /// <summary>
        /// 出库确认时间
        /// </summary>
        public DateTime? ConfirmedTime { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        public virtual ICollection<StockOutItem> Items { get; set; } = new List<StockOutItem>();
    }

    /// <summary>
    /// 出库单明细表
    /// </summary>
    [Table("stockoutitem")]
    public class StockOutItem : BaseGuidEntity
    {
        /// <summary>
        /// 明细ID (主键)
        /// </summary>
        [Key]
        [StringLength(36)]
        [Column("ItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 出库单ID (外键)
        /// </summary>
        [Required]
        [StringLength(36)]
        public string StockOutId { get; set; } = string.Empty;

        /// <summary>
        /// 物料ID
        /// </summary>
        [Required]
        [StringLength(36)]
        public string MaterialId { get; set; } = string.Empty;

        /// <summary>
        /// 出库数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 订单应出数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal OrderQty { get; set; } = 0.0000m;

        /// <summary>
        /// 计划出库数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal PlanQty { get; set; } = 0.0000m;

        /// <summary>
        /// 拣货占用数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal PickQty { get; set; } = 0.0000m;

        /// <summary>
        /// 实际出库数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal ActualQty { get; set; } = 0.0000m;

        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal Price { get; set; } = 0.000000m;

        /// <summary>
        /// 金额
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; } = 0.00m;

        /// <summary>
        /// 库位ID
        /// </summary>
        [StringLength(36)]
        public string? LocationId { get; set; }

        /// <summary>
        /// 库存记录ID（对应 StockInfo.Id）
        /// </summary>
        [StringLength(36)]
        public string? StockId { get; set; }

        /// <summary>
        /// 仓库ID（冗余，方便查询）
        /// </summary>
        [StringLength(36)]
        public string? WarehouseId { get; set; }

        /// <summary>
        /// 批次号
        /// </summary>
        [StringLength(50)]
        public string? BatchNo { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("StockOutId")]
        public virtual StockOut? StockOut { get; set; }
    }
}

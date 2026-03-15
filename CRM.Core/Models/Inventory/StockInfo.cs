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
        /// 库存数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; } = 0.0000m;

        /// <summary>
        /// 可用数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal AvailableQuantity { get; set; } = 0.0000m;

        /// <summary>
        /// 锁定数量
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal LockedQuantity { get; set; } = 0.0000m;

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
        /// 单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Price { get; set; } = 0.0000m;

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
        /// 单价
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal Price { get; set; } = 0.0000m;

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
        /// 备注
        /// </summary>
        [StringLength(500)]
        public string? Remark { get; set; }

        // 导航属性
        [ForeignKey("StockOutId")]
        public virtual StockOut? StockOut { get; set; }
    }
}

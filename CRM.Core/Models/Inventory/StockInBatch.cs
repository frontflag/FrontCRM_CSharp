using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 入库批次：关联入库明细，系统生成全局唯一编号 <c>PC-xxxxxxxx</c>，供出库批次核销关联。
    /// </summary>
    [Table("stock_in_batch")]
    public class StockInBatch : BaseGuidEntity, ISoftDeletable
    {
        [Key]
        [StringLength(36)]
        [Column("id")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(36)]
        [Column("stock_in_item_id")]
        public string StockInItemId { get; set; } = string.Empty;

        /// <summary>批次全局唯一编号，格式 PC- + 8 位十进制流水。</summary>
        [Required]
        [StringLength(20)]
        [Column("global_batch_no")]
        public string GlobalBatchNo { get; set; } = string.Empty;

        /// <summary>批次维度，见字典 <c>InventoryBatchDimension</c>。</summary>
        [StringLength(32)]
        [Column("batch_dimension")]
        public string? BatchDimension { get; set; }

        /// <summary>批次记录单位，见字典 <c>InventoryBatchRecordUnit</c>。</summary>
        [StringLength(32)]
        [Column("batch_unit")]
        public string? BatchUnit { get; set; }

        /// <summary>单位编号（如 SN 号、盘号等）。</summary>
        [StringLength(128)]
        [Column("unit_no")]
        public string? UnitNo { get; set; }

        [Column("batch_qty")]
        public int BatchQty { get; set; }

        [StringLength(64)]
        [Column("dc")]
        public string? Dc { get; set; }

        [StringLength(200)]
        [Column("package_origin")]
        public string? PackageOrigin { get; set; }

        [StringLength(200)]
        [Column("wafer_origin")]
        public string? WaferOrigin { get; set; }

        [StringLength(128)]
        [Column("lot")]
        public string? Lot { get; set; }

        [StringLength(200)]
        [Column("serial_number")]
        public string? SerialNumber { get; set; }

        [StringLength(128)]
        [Column("firmware_version")]
        public string? FirmwareVersion { get; set; }

        [StringLength(128)]
        [Column("part_code")]
        public string? PartCode { get; set; }

        [StringLength(1000)]
        [Column("remark")]
        public string? Remark { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}

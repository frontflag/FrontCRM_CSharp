using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Interfaces;
using CRM.Core.Models;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 入库批次记录：关联入库单/明细，记录 LOT、SN、产地与数量等批次维度信息。
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
        [Column("stock_in_id")]
        public string StockInId { get; set; } = string.Empty;

        [Required]
        [StringLength(36)]
        [Column("stock_in_item_id")]
        public string StockInItemId { get; set; } = string.Empty;

        [StringLength(64)]
        [Column("stock_in_item_code")]
        public string? StockInItemCode { get; set; }

        /// <summary>型号</summary>
        [StringLength(200)]
        [Column("material_model")]
        public string? MaterialModel { get; set; }

        /// <summary>DC</summary>
        [StringLength(64)]
        [Column("dc")]
        public string? Dc { get; set; }

        /// <summary>封装产地</summary>
        [StringLength(200)]
        [Column("package_origin")]
        public string? PackageOrigin { get; set; }

        /// <summary>晶圆产地</summary>
        [StringLength(200)]
        [Column("wafer_origin")]
        public string? WaferOrigin { get; set; }

        [StringLength(128)]
        [Column("lot")]
        public string? Lot { get; set; }

        [Column("lot_qty_in")]
        public int LotQtyIn { get; set; }

        [Column("lot_qty_out")]
        public int LotQtyOut { get; set; }

        /// <summary>产地</summary>
        [StringLength(200)]
        [Column("origin")]
        public string? Origin { get; set; }

        /// <summary>SN 号</summary>
        [StringLength(200)]
        [Column("serial_number")]
        public string? SerialNumber { get; set; }

        [Column("sn_qty_in")]
        public int SnQtyIn { get; set; }

        [Column("sn_qty_out")]
        public int SnQtyOut { get; set; }

        /// <summary>固件版本号</summary>
        [StringLength(128)]
        [Column("firmware_version")]
        public string? FirmwareVersion { get; set; }

        [StringLength(1000)]
        [Column("remark")]
        public string? Remark { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
    }
}

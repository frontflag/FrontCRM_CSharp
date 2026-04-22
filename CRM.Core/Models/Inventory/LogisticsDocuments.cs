using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 到货通知（单表：一条记录 = 采购明细上的一次到货批次）
    /// </summary>
    [Table("stockinnotify")]
    public class StockInNotify : BaseGuidEntity
    {
        [StringLength(32)]
        public string NoticeCode { get; set; } = string.Empty;

        [StringLength(36)]
        public string PurchaseOrderId { get; set; } = string.Empty;

        [StringLength(32)]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        /// <summary>采购订单明细 Id</summary>
        [StringLength(36)]
        public string PurchaseOrderItemId { get; set; } = string.Empty;

        /// <summary>销售订单明细 Id（冗余，来自采购明细）</summary>
        [StringLength(36)]
        public string? SellOrderItemId { get; set; }

        [StringLength(36)]
        public string? VendorId { get; set; }

        [StringLength(64)]
        public string? VendorName { get; set; }

        /// <summary>供应商编号（展示用，从采购单关联填充，不落库）</summary>
        [NotMapped]
        public string? VendorCode { get; set; }

        [StringLength(64)]
        public string? PurchaseUserName { get; set; }

        /// <summary>1新建 10未到货 20到货待检 30已质检 100已入库</summary>
        public short Status { get; set; } = 10;

        /// <summary>预计到货日期</summary>
        public DateTime? ExpectedArrivalDate { get; set; }

        /// <summary>地域类型 RegionType：10=境内 20=境外（与仓库档案共用枚举）</summary>
        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        [StringLength(128)]
        public string? Pn { get; set; }

        [StringLength(64)]
        public string? Brand { get; set; }

        /// <summary>本批次预期到货数量</summary>
        public int ExpectQty { get; set; }

        /// <summary>本批次实收数量（入库流程回写）</summary>
        public int ReceiveQty { get; set; }

        /// <summary>本批次质检通过数量汇总</summary>
        public int PassedQty { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal Cost { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ExpectTotal { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiveTotal { get; set; }

        /// <summary>兼容旧前端：明细弹窗 items[]，由服务填充为单元素</summary>
        [NotMapped]
        public ICollection<StockInNotifyItemSnapshot>? Items { get; set; }
    }

    /// <summary>
    /// 到货通知行快照（非表，仅序列化/API 兼容）
    /// </summary>
    public class StockInNotifyItemSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string StockInNotifyId { get; set; } = string.Empty;
        public string PurchaseOrderItemId { get; set; } = string.Empty;
        public string? Pn { get; set; }
        public string? Brand { get; set; }
        public int Qty { get; set; }
        public int ArrivedQty { get; set; }
        public int PassedQty { get; set; }
    }

    [Table("qcinfo")]
    public class QCInfo : BaseGuidEntity
    {
        [StringLength(32)]
        public string QcCode { get; set; } = string.Empty;

        [StringLength(36)]
        public string StockInNotifyId { get; set; } = string.Empty;

        [StringLength(32)]
        public string StockInNotifyCode { get; set; } = string.Empty;

        /// <summary>-1未通过 10部分通过 100已通过</summary>
        public short Status { get; set; } = 10;

        /// <summary>-1拒收 1未入库 10部分入库 100全部入库</summary>
        public short StockInStatus { get; set; } = 1;

        public int PassQty { get; set; }
        public int RejectQty { get; set; }

        [StringLength(36)]
        public string? StockInId { get; set; }

        /// <summary>质检填写的计划入库日；从质检列表生成入库单时作为 <see cref="StockIn.StockInDate"/> 来源。</summary>
        public DateTime? StockInPlanDate { get; set; }

        [NotMapped]
        public string? VendorName { get; set; }

        [NotMapped]
        public string? PurchaseOrderCode { get; set; }

        [NotMapped]
        public string? SalesOrderCode { get; set; }

        [NotMapped]
        public string? Model { get; set; }

        [NotMapped]
        public string? Brand { get; set; }

        [StringLength(36)]
        [Column("create_by_user_id")]
        public string? CreateByUserId { get; set; }

        /// <summary>列表/详情展示用，由服务层根据 <see cref="CreateByUserId"/> 解析。</summary>
        [NotMapped]
        public string? CreateUserName { get; set; }

        [StringLength(36)]
        [Column("modify_by_user_id")]
        public string? ModifyByUserId { get; set; }

        public ICollection<QCItem> Items { get; set; } = new List<QCItem>();
    }

    [Table("qcitem")]
    public class QCItem : BaseGuidEntity
    {
        [StringLength(36)]
        public string QcInfoId { get; set; } = string.Empty;

        /// <summary>对应单表到货通知行 Id（原 StockInNotifyItemId）</summary>
        [StringLength(36)]
        public string ArrivalStockInNotifyId { get; set; } = string.Empty;

        public int ArrivedQty { get; set; }
        public int PassedQty { get; set; }
        public int RejectQty { get; set; }

        public QCInfo? QcInfo { get; set; }
    }
}

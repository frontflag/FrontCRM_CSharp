using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;

namespace CRM.Core.Models.Inventory
{
    /// <summary>
    /// 到货通知（表 <c>stockin_notify</c>：单表模型，一条记录 = 采购订单明细上的一次到货批次）。
    /// 主键列名为 <c>UserId</c>（历史命名）。
    /// </summary>
    [Table("stockin_notify")]
    public class StockInNotify : BaseGuidEntity, ISoftDeletable
    {
        /// <summary>到货通知业务单号（模块 ArrivalNotice 流水）。</summary>
        [StringLength(32)]
        public string NoticeCode { get; set; } = string.Empty;

        /// <summary>所属采购订单主键（<c>purchaseorder</c>）。</summary>
        [StringLength(36)]
        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>采购订单号冗余（创建时从主单复制，减少列表联表）。</summary>
        [StringLength(32)]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        /// <summary>所属采购订单明细主键；同一采购行可有多条到货通知（分批到货）。</summary>
        [StringLength(36)]
        public string PurchaseOrderItemId { get; set; } = string.Empty;

        /// <summary>关联销售订单明细主键（冗余自采购明细，用于销售侧追溯与扩展表回算）。</summary>
        [StringLength(36)]
        public string? SellOrderItemId { get; set; }

        /// <summary>供应商主键（创建时从采购订单复制）。</summary>
        [StringLength(36)]
        public string? VendorId { get; set; }

        /// <summary>供应商名称冗余（展示用）。</summary>
        [StringLength(64)]
        public string? VendorName { get; set; }

        /// <summary>供应商编号（展示用，由服务从采购单关联填充，不落库）。</summary>
        [NotMapped]
        public string? VendorCode { get; set; }

        /// <summary>采购业务员名称冗余（来自采购订单，展示用）。</summary>
        [StringLength(64)]
        public string? PurchaseUserName { get; set; }

        /// <summary>
        /// 到货通知状态：1=新建（遗留），10=未到货，20=到货待检，30=已质检，100=已入库；
        /// 由质检/入库及 <c>purchaseorderitemextend</c> 同步回写。
        /// </summary>
        public short Status { get; set; } = 10;

        /// <summary>预计到货日期；创建时默认取采购明细或采购主单交货日。</summary>
        public DateTime? ExpectedArrivalDate { get; set; }

        /// <summary>地域类型，见 <see cref="RegionTypeCode"/>：10=境内，20=境外。</summary>
        [Column("RegionType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;

        /// <summary>入库业务类型，见 <see cref="StockInTypeCode"/>；下游 <c>stock_in.StockInType</c> 可取自此字段。</summary>
        [Column("StockInType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockInType { get; set; } = StockInTypeCode.Purchase;

        /// <summary>物料型号（PN）快照，创建时取自采购明细。</summary>
        [StringLength(128)]
        public string? Pn { get; set; }

        /// <summary>品牌快照，创建时取自采购明细。</summary>
        [StringLength(64)]
        public string? Brand { get; set; }

        /// <summary>本批次预期到货数量；创建时不得超过采购行 <c>QtyStockInNotifyNot</c>。</summary>
        public int ExpectQty { get; set; }

        /// <summary>本批次实收到货数量；质检/收货流程回写，参与「剩余可通知」余量计算。</summary>
        public int ReceiveQty { get; set; }

        /// <summary>本批次质检通过数量汇总。</summary>
        public int PassedQty { get; set; }

        /// <summary>采购单价快照（创建时取自采购明细 <c>cost</c>）。</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal Cost { get; set; }

        /// <summary>预期到货金额（创建时按 <c>ExpectQty×Cost</c> 四舍五入）。</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ExpectTotal { get; set; }

        /// <summary>实收金额，随收货/入库流程回写。</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiveTotal { get; set; }

        /// <summary>兼容旧前端：明细弹窗 <c>items[]</c>，由服务填充为单元素快照。</summary>
        [NotMapped]
        public ICollection<StockInNotifyItemSnapshot>? Items { get; set; }

        /// <summary>报关到货：从报关明细发起（StockInType=20）。</summary>
        [StringLength(36)]
        [Column("customs_declaration_item_id")]
        public string? CustomsDeclarationItemId { get; set; }

        /// <summary>软删除标记；为 true 时全局查询过滤器排除。</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
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
    public class QCInfo : BaseGuidEntity, ISoftDeletable
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

        /// <summary>入库类型，见 <see cref="StockInTypeCode"/>（默认采购入库；创建时通常继承到货通知）。</summary>
        [Column("StockInType")]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short StockInType { get; set; } = StockInTypeCode.Purchase;

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

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        public ICollection<QCItem> Items { get; set; } = new List<QCItem>();
    }

    [Table("qcitem")]
    public class QCItem : BaseGuidEntity, ISoftDeletable
    {
        [StringLength(36)]
        public string QcInfoId { get; set; } = string.Empty;

        /// <summary>对应单表到货通知行 Id（原 StockInNotifyItemId）</summary>
        [StringLength(36)]
        public string ArrivalStockInNotifyId { get; set; } = string.Empty;

        public int ArrivedQty { get; set; }
        public int PassedQty { get; set; }
        public int RejectQty { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        public QCInfo? QcInfo { get; set; }
    }
}

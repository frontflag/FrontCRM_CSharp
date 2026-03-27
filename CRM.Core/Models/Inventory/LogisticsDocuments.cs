using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Inventory
{
    [Table("stockinnotify")]
    public class StockInNotify : BaseGuidEntity
    {
        [StringLength(32)]
        public string NoticeCode { get; set; } = string.Empty;

        [StringLength(36)]
        public string PurchaseOrderId { get; set; } = string.Empty;

        [StringLength(32)]
        public string PurchaseOrderCode { get; set; } = string.Empty;

        [StringLength(36)]
        public string? VendorId { get; set; }

        [StringLength(64)]
        public string? VendorName { get; set; }

        [StringLength(64)]
        public string? PurchaseUserName { get; set; }

        /// <summary>1新建 10未到货 20到货待检 30已质检 100已入库</summary>
        public short Status { get; set; } = 10;

        /// <summary>预计到货日期（通知物流人员关注到期接收）</summary>
        public DateTime? ExpectedArrivalDate { get; set; }

        public ICollection<StockInNotifyItem> Items { get; set; } = new List<StockInNotifyItem>();
    }

    [Table("stockinnotifyitem")]
    public class StockInNotifyItem : BaseGuidEntity
    {
        [StringLength(36)]
        public string StockInNotifyId { get; set; } = string.Empty;

        [StringLength(36)]
        public string PurchaseOrderItemId { get; set; } = string.Empty;

        [StringLength(128)]
        public string? Pn { get; set; }

        [StringLength(64)]
        public string? Brand { get; set; }

        public decimal Qty { get; set; }
        public decimal ArrivedQty { get; set; }
        public decimal PassedQty { get; set; }

        public StockInNotify? StockInNotify { get; set; }
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

        public decimal PassQty { get; set; }
        public decimal RejectQty { get; set; }

        [StringLength(36)]
        public string? StockInId { get; set; }

        /// <summary>展示字段：供应商名称（由到货通知带出）</summary>
        [NotMapped]
        public string? VendorName { get; set; }

        /// <summary>展示字段：采购订单号（由到货通知带出）</summary>
        [NotMapped]
        public string? PurchaseOrderCode { get; set; }

        /// <summary>展示字段：销售订单号（由采购明细关联销售明细推导）</summary>
        [NotMapped]
        public string? SalesOrderCode { get; set; }

        /// <summary>展示字段：型号（由到货通知明细聚合）</summary>
        [NotMapped]
        public string? Model { get; set; }

        public ICollection<QCItem> Items { get; set; } = new List<QCItem>();
    }

    [Table("qcitem")]
    public class QCItem : BaseGuidEntity
    {
        [StringLength(36)]
        public string QcInfoId { get; set; } = string.Empty;

        [StringLength(36)]
        public string StockInNotifyItemId { get; set; } = string.Empty;

        public decimal ArrivedQty { get; set; }
        public decimal PassedQty { get; set; }
        public decimal RejectQty { get; set; }

        public QCInfo? QcInfo { get; set; }
    }
}

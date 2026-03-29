using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public interface ILogisticsService
    {
        Task<IReadOnlyList<StockInNotify>> GetArrivalNoticesAsync();
        Task<StockInNotify> CreateArrivalNoticeAsync(CreateArrivalNoticeRequest request);
        Task<AutoGenerateArrivalNoticeResult> AutoGenerateArrivalNoticesAsync();
        Task UpdateArrivalNoticeStatusAsync(string id, short status);

        Task<IReadOnlyList<QCInfo>> GetQcsAsync(QcQueryRequest? request = null);
        Task<QCInfo> CreateQcAsync(CreateQcRequest request);
        Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request);
        Task BindQcStockInAsync(string id, string stockInId);
        Task HandleStockInCompletedAsync(string stockInId, string? purchaseOrderId);
    }

    public class CreateArrivalNoticeRequest
    {
        /// <summary>采购明细 Id（单表到货通知必填）</summary>
        public string PurchaseOrderItemId { get; set; } = string.Empty;

        /// <summary>本批次预期到货数量</summary>
        public decimal ExpectQty { get; set; }

        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>预计到货日期（可选，缺省用采购明细/订单交货日）</summary>
        public DateTime? ExpectedArrivalDate { get; set; }
    }

    public class CreateQcRequest
    {
        public string StockInNotifyId { get; set; } = string.Empty;
    }

    public class UpdateQcResultRequest
    {
        /// <summary>pass | partial | reject</summary>
        public string Result { get; set; } = "pass";
        public decimal PassQty { get; set; }
        public decimal RejectQty { get; set; }
    }

    public class QcQueryRequest
    {
        public string? Model { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? SalesOrderCode { get; set; }
    }

    public class AutoGenerateArrivalNoticeResult
    {
        public int PurchaseOrdersScanned { get; set; }
        public int CreatedCount { get; set; }
        public int ExistingCount { get; set; }
    }
}

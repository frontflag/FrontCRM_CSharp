using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    public interface ILogisticsService
    {
        Task<IReadOnlyList<StockInNotify>> GetArrivalNoticesAsync();
        Task<StockInNotify> CreateArrivalNoticeAsync(CreateArrivalNoticeRequest request);
        Task<AutoGenerateArrivalNoticeResult> AutoGenerateArrivalNoticesAsync();
        Task UpdateArrivalNoticeStatusAsync(string id, short status);

        Task<IReadOnlyList<QCInfo>> GetQcsAsync();
        Task<QCInfo> CreateQcAsync(CreateQcRequest request);
        Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request);
        Task BindQcStockInAsync(string id, string stockInId);
        Task HandleStockInCompletedAsync(string stockInId, string? purchaseOrderId);
    }

    public class CreateArrivalNoticeRequest
    {
        public string PurchaseOrderId { get; set; } = string.Empty;

        /// <summary>预计到货日期（通知物流；可选，批量同步时可缺省由 PO 交货日带出）</summary>
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

    public class AutoGenerateArrivalNoticeResult
    {
        public int PurchaseOrdersScanned { get; set; }
        public int CreatedCount { get; set; }
        public int ExistingCount { get; set; }
    }
}

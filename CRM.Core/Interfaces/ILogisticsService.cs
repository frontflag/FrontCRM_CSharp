using System.Text.Json.Serialization;
using CRM.Core.Constants;
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
        Task<QCInfo> CreateQcAsync(CreateQcRequest request, string? actingUserId = null);
        Task<QCInfo> UpdateQcResultAsync(string id, UpdateQcResultRequest request, string? actingUserId = null);
        Task BindQcStockInAsync(string id, string stockInId, string? actingUserId = null);
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

        /// <summary>地域类型 RegionType：10=境内 20=境外（与仓库档案共用）</summary>
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public short RegionType { get; set; } = RegionTypeCode.Domestic;
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

        /// <summary>
        /// 为 <c>true</c> 时同步写入 <see cref="StockInPlanDate"/>（含置空）。不传或为 <c>false</c> 则不修改原值（兼容旧客户端）。
        /// </summary>
        public bool? HasStockInPlanDate { get; set; }

        /// <summary>计划入库日期（UTC 或可解析为 UTC 的 ISO 8601）。</summary>
        public DateTime? StockInPlanDate { get; set; }
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

using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 入库单列表行（含来源单号、供应商名称等展示字段）
    /// </summary>
    public class StockInListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string StockInCode { get; set; } = string.Empty;
        public short StockInType { get; set; }
        /// <summary>来源单号（采购单号/质检单号等，非内部 GUID）</summary>
        public string? SourceDisplayNo { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        public string? VendorId { get; set; }
        public string? VendorName { get; set; }
        /// <summary>采购订单号（由头表采购明细关联的采购单）</summary>
        public string? PurchaseOrderCode { get; set; }

        /// <summary>销售订单号（由采购订单明细关联推导，可能多单逗号拼接）</summary>
        public string? SalesOrderCode { get; set; }
        /// <summary>明细汇总：物料型号（多行去重后逗号分隔）</summary>
        public string? MaterialModelSummary { get; set; }
        /// <summary>明细汇总：品牌（多行去重后逗号分隔）</summary>
        public string? MaterialBrandSummary { get; set; }
        public DateTime StockInDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        /// <summary>币别编码（与采购订单明细 <c>currency</c> 一致）；无法从来源解析时为 null</summary>
        public short? CurrencyCode { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
        /// <summary>创建人展示名（由 CreatedBy 用户 Id 解析）</summary>
        public string? CreateUserName { get; set; }
    }

    /// <summary>
    /// 入库服务接口
    /// </summary>
    public interface IStockInService
    {
        Task<StockIn> CreateAsync(CreateStockInRequest request, string? actingUserId = null);
        Task<StockIn?> GetByIdAsync(string id);
        Task<IReadOnlyList<StockInListItemDto>> GetListAsync(StockInQueryRequest? request = null);
        Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request, string? actingUserId = null);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);
    }

    public class StockInQueryRequest
    {
        public string? Model { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? SalesOrderCode { get; set; }
        public string? StockInCode { get; set; }
        public string? SourceDisplayNo { get; set; }
        public string? WarehouseId { get; set; }
        public DateTime? StockInDateStart { get; set; }
        public DateTime? StockInDateEnd { get; set; }
        public string? Remark { get; set; }
    }

    public class CreateStockInRequest
    {
        public string StockInCode { get; set; } = string.Empty;
        public string? PurchaseOrderId { get; set; }
        /// <summary>到货通知主键；若提供则写入 SourceId/SourceCode</summary>
        public string? StockInNotifyId { get; set; }
        /// <summary>质检单主键；若提供则写入 QcId/QcCode，并在未提供到货通知时从质检关联通知补全 Source*</summary>
        public string? QcId { get; set; }
        public string? VendorId { get; set; }
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public DateTime StockInDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public string? Remark { get; set; }
        public List<CreateStockInItemRequest> Items { get; set; } = new();
    }

    public class CreateStockInItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string? BatchNo { get; set; }
        public string? WarehouseLocation { get; set; }
    }

    public class UpdateStockInRequest
    {
        public string? Remark { get; set; }
    }
}

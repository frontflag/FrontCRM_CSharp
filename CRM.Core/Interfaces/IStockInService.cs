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
        /// <summary>销售订单号（由采购订单明细关联推导，可能多单逗号拼接）</summary>
        public string? SalesOrderCode { get; set; }
        public DateTime StockInDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 入库服务接口
    /// </summary>
    public interface IStockInService
    {
        Task<StockIn> CreateAsync(CreateStockInRequest request);
        Task<StockIn?> GetByIdAsync(string id);
        Task<IReadOnlyList<StockInListItemDto>> GetListAsync(StockInQueryRequest? request = null);
        Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class StockInQueryRequest
    {
        public string? Model { get; set; }
        public string? VendorName { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public string? SalesOrderCode { get; set; }
    }

    public class CreateStockInRequest
    {
        public string StockInCode { get; set; } = string.Empty;
        public string? PurchaseOrderId { get; set; }
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

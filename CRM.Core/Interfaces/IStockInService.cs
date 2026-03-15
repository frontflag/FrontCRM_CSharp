using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 入库服务接口
    /// </summary>
    public interface IStockInService
    {
        Task<StockIn> CreateAsync(CreateStockInRequest request);
        Task<StockIn?> GetByIdAsync(string id);
        Task<IEnumerable<StockIn>> GetAllAsync();
        Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
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

using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 出库服务接口
    /// </summary>
    public interface IStockOutService
    {
        Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request);
        /// <summary>
        /// 执行出库（内部包含预占/拣货/出库确认的 FIFO 逻辑）
        /// </summary>
        Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request);
        Task<StockOut?> GetByIdAsync(string id);
        Task<IEnumerable<StockOut>> GetAllAsync();
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateStockOutRequestRequest
    {
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string RequestUserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public List<CreateStockOutRequestItemRequest> Items { get; set; } = new();
    }

    public class CreateStockOutRequestItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? WarehouseLocation { get; set; }
    }

    public class ExecuteStockOutRequest
    {
        public string StockOutRequestId { get; set; } = string.Empty;
        public string StockOutCode { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string OperatorId { get; set; } = string.Empty;
        public DateTime StockOutDate { get; set; }
        public string? Remark { get; set; }
        public List<ExecuteStockOutItemRequest> Items { get; set; } = new();
    }

    public class ExecuteStockOutItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? BatchNo { get; set; }
        public string? WarehouseLocation { get; set; }
    }
}

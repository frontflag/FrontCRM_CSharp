using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 出库服务接口
    /// </summary>
    public interface IStockOutService
    {
        Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request, string? actingUserId = null);
        /// <summary>销售订单明细申请出库前的数量上下文（前端只读展示）</summary>
        Task<StockOutApplyContextDto> GetApplyContextAsync(string salesOrderId, string salesOrderItemId);
        Task<IEnumerable<StockOutRequestListItemDto>> GetStockOutRequestListAsync();
        /// <summary>
        /// 执行出库（内部包含预占/拣货/出库确认的 FIFO 逻辑）
        /// </summary>
        Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request, string? actingUserId = null);
        Task<StockOut?> GetByIdAsync(string id);
        /// <summary>出库单列表（含客户、业务员、销售明细编号等展示字段）</summary>
        Task<IEnumerable<StockOutListItemDto>> GetStockOutListAsync();
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null);
    }

    public class CreateStockOutRequestRequest
    {
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        /// <summary>销售订单明细主键（sellorderitem）</summary>
        public string SalesOrderItemId { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string RequestUserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>销售明细「申请出库」弹窗用：数量口径由服务端计算，前端仅展示。</summary>
    public class StockOutApplyContextDto
    {
        public string salesOrderItemId { get; set; } = string.Empty;
        public decimal salesOrderQty { get; set; }
        /// <summary>已占用：出库通知单数量合计（状态≠2 已取消）</summary>
        public decimal alreadyNotifiedQty { get; set; }
        /// <summary>按销售行尚可申请的通知数量上限</summary>
        public decimal remainingNotifyQty { get; set; }
        /// <summary>当前可用库存（与库存中心口径一致）</summary>
        public decimal availableStockQty { get; set; }
        /// <summary>建议本次填写上限 = min(remainingNotifyQty, availableStockQty)</summary>
        public decimal suggestedMaxQty { get; set; }
    }

    public class StockOutListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string StockOutCode { get; set; } = string.Empty;
        public short StockOutType { get; set; }
        public string? SourceCode { get; set; }
        public string? SourceId { get; set; }
        public DateTime StockOutDate { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
        public string? CreateByUserId { get; set; }
        public string? CreateUserName { get; set; }
        public string? CustomerName { get; set; }
        public string? SalesUserName { get; set; }
        public string? SellOrderItemCode { get; set; }
    }

    public class StockOutRequestListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string RequestCode { get; set; } = string.Empty;
        public string SalesOrderId { get; set; } = string.Empty;
        public string SalesOrderItemId { get; set; } = string.Empty;
        public string? SalesOrderCode { get; set; }
        public string? MaterialModel { get; set; }
        public string? Brand { get; set; }
        /// <summary>出库通知数量（单表 Quantity）</summary>
        public decimal OutQuantity { get; set; }
        public DateTime? ExpectedStockOutDate { get; set; }
        public string? SalesUserName { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string RequestUserId { get; set; } = string.Empty;
        public string? RequestUserName { get; set; }
        public DateTime RequestDate { get; set; }
        public short Status { get; set; }
        public string? Remark { get; set; }
        public DateTime CreateTime { get; set; }
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

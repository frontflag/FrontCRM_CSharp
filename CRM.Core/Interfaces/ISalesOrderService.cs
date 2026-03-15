using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 销售订单服务接口
    /// </summary>
    public interface ISalesOrderService
    {
        Task<SellOrder> CreateAsync(CreateSalesOrderRequest request);
        Task<SellOrder?> GetByIdAsync(string id);
        Task<IEnumerable<SellOrder>> GetAllAsync();
        Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
        Task RequestStockOutAsync(string id, string requestedBy);
    }

    public class CreateSalesOrderRequest
    {
        public string OrderCode { get; set; } = string.Empty;
        public string? QuoteId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string SalesUserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public short Currency { get; set; }
        public string? PaymentTerms { get; set; }
        public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateSalesOrderItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        public string? Remark { get; set; }
    }
}

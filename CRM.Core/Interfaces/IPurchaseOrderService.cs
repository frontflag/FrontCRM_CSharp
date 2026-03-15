using CRM.Core.Models.Purchase;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 采购订单服务接口
    /// </summary>
    public interface IPurchaseOrderService
    {
        Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request);
        Task<PurchaseOrder?> GetByIdAsync(string id);
        Task<IEnumerable<PurchaseOrder>> GetAllAsync();
        Task<PurchaseOrder> UpdateAsync(string id, UpdatePurchaseOrderRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreatePurchaseOrderRequest
    {
        public string OrderCode { get; set; } = string.Empty;
        public string? SalesOrderId { get; set; }
        public string VendorId { get; set; } = string.Empty;
        public string PurchaseUserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public short Currency { get; set; }
        public string? PaymentTerms { get; set; }
        public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
    }

    public class CreatePurchaseOrderItemRequest
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

    public class UpdatePurchaseOrderRequest
    {
        public string? Remark { get; set; }
    }
}

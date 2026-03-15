using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 发票服务接口
    /// </summary>
    public interface IInvoiceService
    {
        Task<Invoice> CreateAsync(CreateInvoiceRequest request);
        Task<Invoice?> GetByIdAsync(string id);
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<Invoice> UpdateAsync(string id, UpdateInvoiceRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateInvoiceRequest
    {
        public string InvoiceCode { get; set; } = string.Empty;
        public short InvoiceType { get; set; } // 1:进项发票 2:销项发票
        public string? SalesOrderId { get; set; }
        public string? PurchaseOrderId { get; set; }
        public string? CustomerId { get; set; }
        public string? VendorId { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? InvoiceUrl { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public List<CreateInvoiceItemRequest> Items { get; set; } = new();
    }

    public class CreateInvoiceItemRequest
    {
        public int LineNo { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class UpdateInvoiceRequest
    {
        public string? Remark { get; set; }
    }
}

using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 进项发票服务接口
    /// </summary>
    public interface IFinancePurchaseInvoiceService
    {
        Task<FinancePurchaseInvoice> CreateAsync(CreateFinancePurchaseInvoiceRequest request);
        Task<FinancePurchaseInvoice?> GetByIdAsync(string id);
        Task<IEnumerable<FinancePurchaseInvoice>> GetAllAsync();
        Task<FinancePurchaseInvoice> UpdateAsync(string id, UpdateFinancePurchaseInvoiceRequest request);
        Task DeleteAsync(string id);
        Task ConfirmAsync(string id, DateTime confirmDate);
        Task RedInvoiceAsync(string id);
    }

    public class CreateFinancePurchaseInvoiceRequest
    {
        public string VendorId { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal BillAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ExcludTaxAmount { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Remark { get; set; }
        public List<CreateFinancePurchaseInvoiceItemRequest> Items { get; set; } = new();
    }

    public class CreateFinancePurchaseInvoiceItemRequest
    {
        public string? StockInId { get; set; }
        public string? StockInCode { get; set; }
        public string? PurchaseOrderCode { get; set; }
        public decimal StockInCost { get; set; }
        public decimal BillCost { get; set; }
        public long BillQty { get; set; }
        public decimal BillAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ExcludTaxAmount { get; set; }
    }

    public class UpdateFinancePurchaseInvoiceRequest
    {
        public string? InvoiceNo { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? Remark { get; set; }
    }
}

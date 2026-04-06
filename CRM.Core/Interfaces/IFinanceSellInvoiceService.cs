using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 销项发票服务接口
    /// </summary>
    public interface IFinanceSellInvoiceService
    {
        Task<FinanceSellInvoice> CreateAsync(CreateFinanceSellInvoiceRequest request, string? actingUserId = null);
        Task<FinanceSellInvoice?> GetByIdAsync(string id);
        Task<IEnumerable<FinanceSellInvoice>> GetAllAsync();
        Task<FinanceSellInvoice> UpdateAsync(string id, UpdateFinanceSellInvoiceRequest request, string? actingUserId = null);
        Task DeleteAsync(string id);
        Task UpdateInvoiceStatusAsync(string id, short invoiceStatus, string? actingUserId = null);
        Task VoidAsync(string id, string? actingUserId = null);
        Task<PagedResult<FinanceSellInvoice>> GetPagedAsync(FinanceSellInvoiceQueryRequest request);
    }

    public class CreateFinanceSellInvoiceRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? InvoiceCode { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal InvoiceTotal { get; set; }
        public DateTime? MakeInvoiceDate { get; set; }
        public byte Currency { get; set; } = 1;
        public short Type { get; set; } = 10;
        public short SellInvoiceType { get; set; } = 100;
        public string? Remark { get; set; }
        public List<CreateSellInvoiceItemRequest> Items { get; set; } = new();
    }

    public class CreateSellInvoiceItemRequest
    {
        public decimal InvoiceTotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal ValueAddedTax { get; set; }
        public decimal TaxFreeTotal { get; set; }
        public decimal Price { get; set; }
        public long Qty { get; set; }
        public string? StockOutItemId { get; set; }
        public byte Currency { get; set; } = 1;
    }

    public class UpdateFinanceSellInvoiceRequest
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal? InvoiceTotal { get; set; }
        public DateTime? MakeInvoiceDate { get; set; }
        public string? Remark { get; set; }
    }

    public class FinanceSellInvoiceQueryRequest
    {
        public string? Keyword { get; set; }
        public short? InvoiceStatus { get; set; }
        public byte? ReceiveStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}

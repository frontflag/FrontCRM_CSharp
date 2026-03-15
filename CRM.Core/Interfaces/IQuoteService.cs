using CRM.Core.Models.Quote;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 报价服务接口
    /// </summary>
    public interface IQuoteService
    {
        Task<Quote> CreateAsync(CreateQuoteRequest request);
        Task<Quote?> GetByIdAsync(string id);
        Task<IEnumerable<Quote>> GetAllAsync();
        Task<Quote> UpdateAsync(string id, UpdateQuoteRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateQuoteRequest
    {
        public string QuoteCode { get; set; } = string.Empty;
        public string? RFQId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string SalesUserId { get; set; } = string.Empty;
        public string? PurchaseUserId { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<CreateQuoteItemRequest> Items { get; set; } = new();
    }

    public class CreateQuoteItemRequest
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

    public class UpdateQuoteRequest
    {
        public string? Remark { get; set; }
    }
}

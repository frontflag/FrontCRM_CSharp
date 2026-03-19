using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 收款服务接口
    /// </summary>
    public interface IFinanceReceiptService
    {
        Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request);
        Task<FinanceReceipt?> GetByIdAsync(string id);
        Task<IEnumerable<FinanceReceipt>> GetAllAsync();
        Task<FinanceReceipt> UpdateAsync(string id, UpdateFinanceReceiptRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
        Task VerifyReceiptItemAsync(string receiptItemId, string sellInvoiceId, decimal amount);
        Task<PagedResult<FinanceReceipt>> GetPagedAsync(FinanceReceiptQueryRequest request);
    }

    public class CreateFinanceReceiptRequest
    {
        public string FinanceReceiptCode { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public decimal ReceiptAmount { get; set; }
        public byte ReceiptCurrency { get; set; } = 1;
        public DateTime? ReceiptDate { get; set; }
        public string? ReceiptUserId { get; set; }
        public short ReceiptMode { get; set; } = 1;
        public string? ReceiptBankId { get; set; }
        public string? Remark { get; set; }
        public List<CreateFinanceReceiptItemRequest> Items { get; set; } = new();
    }

    public class CreateFinanceReceiptItemRequest
    {
        public string? SellOrderId { get; set; }
        public string? SellOrderItemId { get; set; }
        public string? FinanceSellInvoiceId { get; set; }
        public string? FinanceSellInvoiceItemId { get; set; }
        public decimal ReceiptAmount { get; set; }
        public string? StockOutItemId { get; set; }
        public string? ProductId { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
    }

    public class UpdateFinanceReceiptRequest
    {
        public decimal? ReceiptAmount { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public short? ReceiptMode { get; set; }
        public string? Remark { get; set; }
    }

    public class FinanceReceiptQueryRequest
    {
        public string? Keyword { get; set; }
        public short? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}

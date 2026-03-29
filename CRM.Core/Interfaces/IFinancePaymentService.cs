using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 付款服务接口
    /// </summary>
    public interface IFinancePaymentService
    {
        Task<FinancePayment> CreateAsync(CreateFinancePaymentRequest request);
        Task<FinancePayment?> GetByIdAsync(string id);
        Task<IEnumerable<FinancePayment>> GetAllAsync();
        Task<FinancePayment> UpdateAsync(string id, UpdateFinancePaymentRequest request);
        Task DeleteAsync(string id);
        /// <param name="remark">审核驳回原因等补充说明（可选）</param>
        Task UpdateStatusAsync(string id, short status, string? remark = null);
        Task VerifyPaymentItemAsync(string paymentItemId, decimal amount);
        Task<PagedResult<FinancePayment>> GetPagedAsync(FinancePaymentQueryRequest request);
    }

    public class CreateFinancePaymentRequest
    {
        public string FinancePaymentCode { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public decimal PaymentAmountToBe { get; set; }
        public byte PaymentCurrency { get; set; } = 1;
        public DateTime? PaymentDate { get; set; }
        public string? PaymentUserId { get; set; }
        public short PaymentMode { get; set; } = 1;
        public string? BankSlipNo { get; set; }
        public string? Remark { get; set; }
        public List<CreateFinancePaymentItemRequest> Items { get; set; } = new();
    }

    public class CreateFinancePaymentItemRequest
    {
        public string? PurchaseOrderId { get; set; }
        public string? PurchaseOrderItemId { get; set; }
        public decimal PaymentAmountToBe { get; set; }
        public string? ProductId { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
    }

    public class UpdateFinancePaymentRequest
    {
        public decimal? PaymentAmountToBe { get; set; }
        /// <summary>付款币别 1:人民币 2:美元 3:欧元</summary>
        public byte? PaymentCurrency { get; set; }
        public DateTime? PaymentDate { get; set; }
        public short? PaymentMode { get; set; }
        public string? BankSlipNo { get; set; }
        public string? Remark { get; set; }
    }

    public class FinancePaymentQueryRequest
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

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
        Task UpdateStatusAsync(string id, short status);
        Task VerifyPaymentItemAsync(string paymentItemId, decimal amount);
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
        public DateTime? PaymentDate { get; set; }
        public short? PaymentMode { get; set; }
        public string? Remark { get; set; }
    }
}

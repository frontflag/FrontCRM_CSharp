using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 付款服务接口
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentRequest> CreatePaymentRequestAsync(CreatePaymentRequest request, string? actingUserId = null);
        Task<Payment> ApproveAndPayAsync(ApprovePaymentRequest request, string? actingUserId = null);
        Task<Payment?> GetByIdAsync(string id);
        Task<IEnumerable<Payment>> GetAllAsync();
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreatePaymentRequest
    {
        public string RequestCode { get; set; } = string.Empty;
        public string? PurchaseOrderId { get; set; }
        public string VendorId { get; set; } = string.Empty;
        public string RequestUserId { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public decimal Amount { get; set; }
        public short Currency { get; set; }
        public short PaymentMethod { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? Remark { get; set; } = string.Empty;
    }

    public class ApprovePaymentRequest
    {
        public string PaymentRequestId { get; set; } = string.Empty;
        public string ApproverId { get; set; } = string.Empty;
        public DateTime ApproveDate { get; set; }
        public DateTime ActualPaymentDate { get; set; }
        public string? Remark { get; set; }
    }
}

using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 收款服务接口
    /// </summary>
    public interface IReceiptService
    {
        Task<Receipt> CreateAsync(CreateReceiptRequest request);
        Task<Receipt?> GetByIdAsync(string id);
        Task<IEnumerable<Receipt>> GetAllAsync();
        Task<Receipt> UpdateAsync(string id, UpdateReceiptRequest request);
        Task DeleteAsync(string id);
        Task UpdateStatusAsync(string id, short status);
    }

    public class CreateReceiptRequest
    {
        public string ReceiptCode { get; set; } = string.Empty;
        public string? InvoiceId { get; set; }
        public string? SalesOrderId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public decimal Amount { get; set; }
        public short Currency { get; set; }
        public short PaymentMethod { get; set; }
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? Remark { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
    }

    public class UpdateReceiptRequest
    {
        public string? Remark { get; set; }
    }
}

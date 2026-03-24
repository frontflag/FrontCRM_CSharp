using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// 收款服务实现
    /// </summary>
    public class ReceiptService : IReceiptService
    {
        private readonly IRepository<Receipt> _receiptRepository;

        public ReceiptService(IRepository<Receipt> receiptRepository)
        {
            _receiptRepository = receiptRepository;
        }

        public async Task<Receipt> CreateAsync(CreateReceiptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReceiptCode))
                throw new ArgumentException("收款单号不能为空", nameof(request.ReceiptCode));

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            // 检查收款单号是否已存在
            var allReceipts = await _receiptRepository.GetAllAsync();
            if (allReceipts.Any(r => r.ReceiptCode == request.ReceiptCode))
                throw new InvalidOperationException($"收款单号 {request.ReceiptCode} 已存在");

            var receipt = new Receipt
            {
                Id = Guid.NewGuid().ToString(),
                ReceiptCode = request.ReceiptCode.Trim(),
                ReceiptType = 1, // 销售收款
                CustomerId = request.CustomerId,
                SellOrderId = request.SalesOrderId,
                InvoiceId = request.InvoiceId,
                ReceiptDate = PostgreSqlDateTime.ToUtc(request.ReceiptDate),
                ReceivableAmount = request.Amount,
                ReceiptAmount = request.Amount,
                Currency = request.Currency,
                ReceiptMethod = request.PaymentMethod,
                ReceiptAccountName = request.BankAccount,
                ReceiptBank = request.BankName,
                HandlerId = request.ReceivedBy,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _receiptRepository.AddAsync(receipt);
            return receipt;
        }

        public async Task<Receipt?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _receiptRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Receipt>> GetAllAsync()
        {
            return await _receiptRepository.GetAllAsync();
        }

        public async Task<Receipt> UpdateAsync(string id, UpdateReceiptRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var receipt = await _receiptRepository.GetByIdAsync(id);
            if (receipt == null)
                throw new InvalidOperationException($"收款记录 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                receipt.Remark = request.Remark;

            receipt.ModifyTime = DateTime.UtcNow;

            await _receiptRepository.UpdateAsync(receipt);
            return receipt;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var receipt = await _receiptRepository.GetByIdAsync(id);
            if (receipt == null)
                throw new InvalidOperationException($"收款记录 {id} 不存在");

            await _receiptRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var receipt = await _receiptRepository.GetByIdAsync(id);
            if (receipt == null)
                throw new InvalidOperationException($"收款记录 {id} 不存在");

            receipt.Status = status;
            receipt.ModifyTime = DateTime.UtcNow;

            await _receiptRepository.UpdateAsync(receipt);
        }
    }
}

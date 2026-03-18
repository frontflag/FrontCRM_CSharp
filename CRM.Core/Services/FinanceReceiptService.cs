using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    public class FinanceReceiptService : IFinanceReceiptService
    {
        private readonly IRepository<FinanceReceipt> _receiptRepo;
        private readonly IRepository<FinanceReceiptItem> _itemRepo;
        private readonly IRepository<FinanceSellInvoice> _sellInvoiceRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public FinanceReceiptService(
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> itemRepo,
            IRepository<FinanceSellInvoice> sellInvoiceRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _receiptRepo = receiptRepo;
            _itemRepo = itemRepo;
            _sellInvoiceRepo = sellInvoiceRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var receipt = new FinanceReceipt
            {
                Id = Guid.NewGuid().ToString(),
                FinanceReceiptCode = request.FinanceReceiptCode,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                ReceiptAmount = request.ReceiptAmount,
                ReceiptCurrency = request.ReceiptCurrency,
                ReceiptDate = request.ReceiptDate,
                ReceiptUserId = request.ReceiptUserId,
                ReceiptMode = request.ReceiptMode,
                ReceiptBankId = request.ReceiptBankId,
                Remark = request.Remark,
                Status = 0,
                CreateTime = DateTime.UtcNow
            };
            await _receiptRepo.AddAsync(receipt);

            foreach (var item in request.Items)
            {
                var receiptItem = new FinanceReceiptItem
                {
                    Id = Guid.NewGuid().ToString(),
                    FinanceReceiptId = receipt.Id,
                    SellOrderId = item.SellOrderId,
                    SellOrderItemId = item.SellOrderItemId,
                    FinanceSellInvoiceId = item.FinanceSellInvoiceId,
                    FinanceSellInvoiceItemId = item.FinanceSellInvoiceItemId,
                    ReceiptAmount = item.ReceiptAmount,
                    ReceiptConvertAmount = item.ReceiptAmount,
                    StockOutItemId = item.StockOutItemId,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    VerificationStatus = 0,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(receiptItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return receipt;
        }

        public async Task<FinanceReceipt?> GetByIdAsync(string id) =>
            await _receiptRepo.GetByIdAsync(id);

        public async Task<IEnumerable<FinanceReceipt>> GetAllAsync() =>
            await _receiptRepo.GetAllAsync();

        public async Task<FinanceReceipt> UpdateAsync(string id, UpdateFinanceReceiptRequest request)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");

            if (request.ReceiptAmount.HasValue) receipt.ReceiptAmount = request.ReceiptAmount.Value;
            if (request.ReceiptDate.HasValue) receipt.ReceiptDate = request.ReceiptDate.Value;
            if (request.ReceiptMode.HasValue) receipt.ReceiptMode = request.ReceiptMode.Value;
            if (request.Remark != null) receipt.Remark = request.Remark;
            receipt.ModifyTime = DateTime.UtcNow;

            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return receipt;
        }

        public async Task DeleteAsync(string id)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");
            var items = await _itemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.FinanceReceiptId == id))
                await _itemRepo.DeleteAsync(item.Id);
            await _receiptRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");
            receipt.Status = status;
            receipt.ModifyTime = DateTime.UtcNow;
            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task VerifyReceiptItemAsync(string receiptItemId, string sellInvoiceId, decimal amount)
        {
            var item = await _itemRepo.GetByIdAsync(receiptItemId)
                ?? throw new InvalidOperationException($"收款明细 {receiptItemId} 不存在");

            item.VerificationStatus = 2; // 核销完成
            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);

            // 更新销项发票收款状态
            if (!string.IsNullOrEmpty(sellInvoiceId))
            {
                var invoice = await _sellInvoiceRepo.GetByIdAsync(sellInvoiceId);
                if (invoice != null)
                {
                    invoice.ReceiveDone += amount;
                    invoice.ReceiveToBe -= amount;
                    if (invoice.ReceiveToBe <= 0)
                        invoice.ReceiveStatus = 2; // 收款完成
                    else if (invoice.ReceiveDone > 0)
                        invoice.ReceiveStatus = 1; // 部分收款
                    invoice.ModifyTime = DateTime.UtcNow;
                    await _sellInvoiceRepo.UpdateAsync(invoice);
                }
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}

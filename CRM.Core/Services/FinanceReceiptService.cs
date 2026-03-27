using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinanceReceiptService : IFinanceReceiptService
    {
        private readonly IRepository<FinanceReceipt> _receiptRepo;
        private readonly IRepository<FinanceReceiptItem> _itemRepo;
        private readonly IRepository<FinanceSellInvoice> _sellInvoiceRepo;
        private readonly IRepository<SellInvoiceItem> _sellInvoiceItemRepo;
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public FinanceReceiptService(
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> itemRepo,
            IRepository<FinanceSellInvoice> sellInvoiceRepo,
            IRepository<SellInvoiceItem> sellInvoiceItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _receiptRepo = receiptRepo;
            _itemRepo = itemRepo;
            _sellInvoiceRepo = sellInvoiceRepo;
            _sellInvoiceItemRepo = sellInvoiceItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var receiptCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Receipt);

            var receipt = new FinanceReceipt
            {
                Id = Guid.NewGuid().ToString(),
                FinanceReceiptCode = receiptCode,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SalesUserId = request.SalesUserId,
                ReceiptAmount = request.ReceiptAmount,
                ReceiptCurrency = request.ReceiptCurrency,
                ReceiptDate = PostgreSqlDateTime.ToUtc(request.ReceiptDate),
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

        public async Task<PagedResult<FinanceReceipt>> GetPagedAsync(FinanceReceiptQueryRequest request)
        {
            var all = await _receiptRepo.GetAllAsync();
            var filteredByPermission = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                filteredByPermission = await _dataPermissionService.FilterFinanceReceiptsAsync(request.CurrentUserId, filteredByPermission);
            }

            var query = filteredByPermission.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(r =>
                    (!string.IsNullOrWhiteSpace(r.FinanceReceiptCode) && r.FinanceReceiptCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(r.CustomerName) && r.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.Status.HasValue)
                query = query.Where(r => r.Status == request.Status.Value);

            if (request.StartDate.HasValue)
                query = query.Where(r => r.CreateTime >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(r => r.CreateTime <= request.EndDate.Value.AddDays(1));

            var totalCount = query.Count();
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var items = query.OrderByDescending(r => r.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<FinanceReceipt>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<FinanceReceipt> UpdateAsync(string id, UpdateFinanceReceiptRequest request)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");

            if (request.ReceiptAmount.HasValue) receipt.ReceiptAmount = request.ReceiptAmount.Value;
            if (request.ReceiptDate.HasValue) receipt.ReceiptDate = PostgreSqlDateTime.ToUtc(request.ReceiptDate.Value);
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

            // 状态流转：Draft(0) -> PendingAudit(1) -> Approved(2) -> Received(3)
            //                          \-> Cancelled(4)
            // 审核通过后允许直接收款或取消
            var current = receipt.Status;
            var allowed =
                (current == 0 && status == 1) ||                // 提交审核
                (current == 0 && status == 4) ||                // 草稿取消
                (current == 1 && (status == 2 || status == 4)) || // 审核通过/驳回取消
                (current == 2 && (status == 3 || status == 4));   // 已审核收款/取消

            if (!allowed)
                throw new InvalidOperationException($"不允许的状态流转: {current} -> {status}");

            receipt.Status = status;
            if (status == 3) // 已收款
            {
                receipt.ReceiptDate ??= DateTime.UtcNow;
            }
            receipt.ModifyTime = DateTime.UtcNow;
            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task VerifyReceiptItemAsync(string receiptItemId, string sellInvoiceId, decimal amount)
        {
            var item = await _itemRepo.GetByIdAsync(receiptItemId)
                ?? throw new InvalidOperationException($"收款明细 {receiptItemId} 不存在");

            if (amount <= 0)
                throw new ArgumentException("核销金额必须大于0", nameof(amount));

            if (amount > item.ReceiptConvertAmount)
                throw new InvalidOperationException($"核销金额超限：应收折算金额 {item.ReceiptConvertAmount}，本次 {amount}");

            if (amount >= item.ReceiptConvertAmount)
                item.VerificationStatus = 2; // 核销完成
            else
                item.VerificationStatus = 1; // 部分核销
            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);

            // 更新销项发票收款状态
            var targetSellInvoiceId = !string.IsNullOrWhiteSpace(sellInvoiceId)
                ? sellInvoiceId
                : item.FinanceSellInvoiceId;
            if (!string.IsNullOrWhiteSpace(targetSellInvoiceId))
            {
                var invoice = await _sellInvoiceRepo.GetByIdAsync(targetSellInvoiceId);
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

                    // 同步销项发票明细的收款状态（当前按主单状态同步）
                    var sellInvoiceItems = (await _sellInvoiceItemRepo.FindAsync(x => x.FinanceSellInvoiceId == invoice.Id)).ToList();
                    foreach (var sellItem in sellInvoiceItems)
                    {
                        sellItem.ReceiveStatus = (short)invoice.ReceiveStatus;
                        sellItem.ModifyTime = DateTime.UtcNow;
                        await _sellInvoiceItemRepo.UpdateAsync(sellItem);
                    }
                }
            }

            await SyncSellOrderReceiptStatusAsync(item);

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        private async Task SyncSellOrderReceiptStatusAsync(FinanceReceiptItem receiptItem)
        {
            if (string.IsNullOrWhiteSpace(receiptItem.SellOrderId))
                return;

            var order = await _sellOrderRepo.GetByIdAsync(receiptItem.SellOrderId);
            if (order == null) return;

            var orderReceiptItems = (await _itemRepo.FindAsync(x => x.SellOrderId == order.Id)).ToList();
            order.FinanceReceiptStatus = orderReceiptItems.All(x => x.VerificationStatus == 2)
                ? (short)2
                : orderReceiptItems.Any(x => x.VerificationStatus > 0)
                    ? (short)1
                    : (short)0;
            order.ModifyTime = DateTime.UtcNow;
            await _sellOrderRepo.UpdateAsync(order);
        }
    }
}

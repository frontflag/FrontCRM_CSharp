using System.Linq;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
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
        private readonly IRepository<User> _userRepository;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IFinanceReceiptListQuery _receiptListQuery;

        public FinanceReceiptService(
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> itemRepo,
            IRepository<FinanceSellInvoice> sellInvoiceRepo,
            IRepository<SellInvoiceItem> sellInvoiceItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            IRepository<User> userRepository,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            IFinanceReceiptListQuery receiptListQuery,
            IUnitOfWork? unitOfWork = null)
        {
            _receiptRepo = receiptRepo;
            _itemRepo = itemRepo;
            _sellInvoiceRepo = sellInvoiceRepo;
            _sellInvoiceItemRepo = sellInvoiceItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _userRepository = userRepository;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _receiptListQuery = receiptListQuery;
            _unitOfWork = unitOfWork;
        }

        private async Task EnrichCreateUserNamesAsync(IReadOnlyList<FinanceReceipt> items)
        {
            if (items.Count == 0) return;
            var ids = items
                .Select(r => r.CreateByUserId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;
            var users = (await _userRepository.FindAsync(u => ids.Contains(u.Id))).ToList();
            var map = users
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .GroupBy(u => u.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => EntityLookupService.FormatUserLoginName(g.First()) ?? g.Key,
                    StringComparer.OrdinalIgnoreCase);
            foreach (var r in items)
            {
                if (string.IsNullOrWhiteSpace(r.CreateByUserId)) continue;
                if (map.TryGetValue(r.CreateByUserId.Trim(), out var name))
                    r.CreateUserName = name;
            }
        }

        public async Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request, string? actingUserId = null)
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
                BankSlipNo = request.BankSlipNo,
                Remark = request.Remark,
                Status = 0,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
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

        public async Task<FinanceReceipt?> GetByIdAsync(string id)
        {
            var r = await _receiptRepo.GetByIdAsync(id);
            if (r == null) return null;
            await EnrichCreateUserNamesAsync(new List<FinanceReceipt> { r });
            return r;
        }

        public async Task<IEnumerable<FinanceReceipt>> GetAllAsync() =>
            await _receiptRepo.GetAllAsync();

        public async Task<PagedResult<FinanceReceipt>> GetPagedAsync(FinanceReceiptQueryRequest request)
        {
            var result = await _receiptListQuery.GetPagedAsync(request);
            var items = result.Items.ToList();
            await EnrichCreateUserNamesAsync(items);
            return new PagedResult<FinanceReceipt>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = result.PageIndex,
                PageSize = result.PageSize
            };
        }

        public async Task<FinanceReceipt> UpdateAsync(string id, UpdateFinanceReceiptRequest request, string? actingUserId = null)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.CustomerId))
                receipt.CustomerId = request.CustomerId.Trim();
            if (request.CustomerName != null) receipt.CustomerName = request.CustomerName;
            if (request.ReceiptAmount.HasValue) receipt.ReceiptAmount = request.ReceiptAmount.Value;
            if (request.ReceiptCurrency.HasValue) receipt.ReceiptCurrency = request.ReceiptCurrency.Value;
            if (request.ReceiptDate.HasValue) receipt.ReceiptDate = PostgreSqlDateTime.ToUtc(request.ReceiptDate.Value);
            if (request.ReceiptMode.HasValue) receipt.ReceiptMode = request.ReceiptMode.Value;
            if (request.BankSlipNo != null) receipt.BankSlipNo = request.BankSlipNo;
            if (request.Remark != null) receipt.Remark = request.Remark;
            receipt.ModifyTime = DateTime.UtcNow;
            receipt.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return receipt;
        }

        public async Task DeleteAsync(string id)
        {
            var receipt = await _receiptRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"收款单 {id} 不存在");
            var items = await _itemRepo.GetAllAsync();
            var recalcLineIds = items
                .Where(i => i.FinanceReceiptId == id && !string.IsNullOrWhiteSpace(i.SellOrderItemId))
                .Select(i => i.SellOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var item in items.Where(i => i.FinanceReceiptId == id))
                await _itemRepo.DeleteAsync(item.Id);
            await _receiptRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            foreach (var sid in recalcLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
        }

        /// <inheritdoc />
        public async Task ForceDeleteAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var entity = await _receiptRepo.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("收款单不存在");
            if (!string.Equals(confirmBillCode.Trim(), entity.FinanceReceiptCode?.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            var guard = await _forceDeleteGuard.CanForceDeleteFinanceReceiptAsync(entity.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await DeleteAsync(entity.Id);

            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.SalesOrder,
                entity.Id,
                string.IsNullOrWhiteSpace(entity.FinanceReceiptCode) ? null : entity.FinanceReceiptCode.Trim(),
                "收款单强制删除",
                actingUserId.Trim(),
                string.IsNullOrWhiteSpace(actingUserName) ? null : actingUserName.Trim(),
                $"强制删除收款单：Id={entity.Id}，Code={entity.FinanceReceiptCode}",
                reason: null);
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
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
            receipt.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task VerifyReceiptItemAsync(string receiptItemId, string sellInvoiceId, decimal amount, string? actingUserId = null)
        {
            var item = await _itemRepo.GetByIdAsync(receiptItemId)
                ?? throw new InvalidOperationException($"收款明细 {receiptItemId} 不存在");

            if (amount <= 0)
                throw new ArgumentException("核销金额必须大于0", nameof(amount));

            var remaining = item.ReceiptConvertAmount - item.VerifiedAmount;
            if (amount > remaining)
                throw new InvalidOperationException($"核销金额超限：剩余可核销 {remaining}，本次 {amount}");

            item.VerifiedAmount += amount;
            if (item.VerifiedAmount >= item.ReceiptConvertAmount)
                item.VerificationStatus = 2; // 核销完成
            else if (item.VerifiedAmount > 0)
                item.VerificationStatus = 1; // 部分核销
            else
                item.VerificationStatus = 0;
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
                    invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
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

            await SyncSellOrderReceiptStatusAsync(item, actingUserId);

            if (!string.IsNullOrWhiteSpace(item.SellOrderItemId))
                await _sellOrderItemExtendSync.RecalculateAsync(item.SellOrderItemId.Trim());

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        private async Task SyncSellOrderReceiptStatusAsync(FinanceReceiptItem receiptItem, string? actingUserId = null)
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
            order.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _sellOrderRepo.UpdateAsync(order);
        }
    }
}

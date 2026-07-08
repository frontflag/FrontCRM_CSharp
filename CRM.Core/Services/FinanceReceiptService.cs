using System.Linq;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinanceReceiptService : IFinanceReceiptService
    {
        private const short ReceiptApproved = 2;
        private const short ReceiptReceived = 3;

        private readonly IRepository<FinanceReceipt> _receiptRepo;
        private readonly IRepository<FinanceReceiptItem> _itemRepo;
        private readonly IRepository<FinanceSellInvoice> _sellInvoiceRepo;
        private readonly IRepository<SellInvoiceItem> _sellInvoiceItemRepo;
        private readonly IRepository<SellOrder> _sellOrderRepo;
        private readonly IRepository<CustomerInfo> _customerRepo;
        private readonly IRepository<User> _userRepository;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IFinanceReceiptListQuery _receiptListQuery;
        private readonly IFinanceCustomerAdvanceService _advanceService;
        private readonly IFinanceReceivableService _receivableService;
        private readonly IRepository<FreightForwarderCompany> _ffCompanyRepo;

        public FinanceReceiptService(
            IRepository<FinanceReceipt> receiptRepo,
            IRepository<FinanceReceiptItem> itemRepo,
            IRepository<FinanceSellInvoice> sellInvoiceRepo,
            IRepository<SellInvoiceItem> sellInvoiceItemRepo,
            IRepository<SellOrder> sellOrderRepo,
            IRepository<CustomerInfo> customerRepo,
            IRepository<User> userRepository,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            IFinanceReceiptListQuery receiptListQuery,
            IFinanceCustomerAdvanceService advanceService,
            IFinanceReceivableService receivableService,
            IRepository<FreightForwarderCompany> ffCompanyRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _receiptRepo = receiptRepo;
            _itemRepo = itemRepo;
            _sellInvoiceRepo = sellInvoiceRepo;
            _sellInvoiceItemRepo = sellInvoiceItemRepo;
            _sellOrderRepo = sellOrderRepo;
            _customerRepo = customerRepo;
            _userRepository = userRepository;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _receiptListQuery = receiptListQuery;
            _advanceService = advanceService;
            _receivableService = receivableService;
            _ffCompanyRepo = ffCompanyRepo;
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

        private async Task EnrichCustomerExtendFieldsAsync(IReadOnlyList<FinanceReceipt> items)
        {
            if (items.Count == 0) return;
            var ids = items
                .Select(r => r.CustomerId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (ids.Length == 0) return;

            var customers = (await _customerRepo.FindAsync(c => ids.Contains(c.Id))).ToList();
            var byId = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                .GroupBy(c => c.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var r in items)
            {
                if (string.IsNullOrWhiteSpace(r.CustomerId)) continue;
                if (!byId.TryGetValue(r.CustomerId.Trim(), out var cust)) continue;

                var nameZh = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                if (!string.IsNullOrWhiteSpace(nameZh))
                    r.CustomerName = nameZh.Trim();
                if (!string.IsNullOrWhiteSpace(cust.EnglishOfficialName))
                    r.CustomerEnglishName = cust.EnglishOfficialName.Trim();
                if (!string.IsNullOrWhiteSpace(cust.CustomerCode))
                    r.CustomerCode = cust.CustomerCode.Trim();
            }
        }

        public async Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            ValidateFreightForwarderReceiptFlags(
                request.IsFreightForwarderPayment,
                request.ReceiptPurpose,
                request.FreightForwarderCompanyId);
            await EnsureFreightForwarderCompanyExistsAsync(request.FreightForwarderCompanyId);

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
                IsFreightForwarderPayment = request.IsFreightForwarderPayment,
                FreightForwarderCompanyId = string.IsNullOrWhiteSpace(request.FreightForwarderCompanyId)
                    ? null
                    : request.FreightForwarderCompanyId.Trim(),
                Status = 0,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };
            await _receiptRepo.AddAsync(receipt);

            var itemRequests = (request.Items ?? new List<CreateFinanceReceiptItemRequest>())
                .Where(i => i.ReceiptAmount > 0m)
                .ToList();
            if (itemRequests.Count == 0 && request.ReceiptAmount > 0m)
            {
                itemRequests.Add(new CreateFinanceReceiptItemRequest
                {
                    ReceiptAmount = request.ReceiptAmount,
                    ReceiptPurpose = request.IsFreightForwarderPayment
                        ? FinanceReceiptPurposeCode.Normal
                        : request.ReceiptPurpose,
                    AdvanceSellOrderId = request.AdvanceSellOrderId,
                    SellOrderId = string.IsNullOrWhiteSpace(request.AdvanceSellOrderId)
                        ? null
                        : request.AdvanceSellOrderId.Trim()
                });
            }

            short headerPurpose = FinanceReceiptPurposeCode.Normal;
            foreach (var item in itemRequests)
            {
                var purpose = ResolveReceiptPurpose(item.ReceiptPurpose, request.ReceiptPurpose);
                if (request.IsFreightForwarderPayment)
                    purpose = FinanceReceiptPurposeCode.Normal;
                if (purpose == FinanceReceiptPurposeCode.Advance)
                    headerPurpose = FinanceReceiptPurposeCode.Advance;

                var advanceSo = string.IsNullOrWhiteSpace(item.AdvanceSellOrderId)
                    ? request.AdvanceSellOrderId
                    : item.AdvanceSellOrderId;

                var receiptItem = new FinanceReceiptItem
                {
                    Id = Guid.NewGuid().ToString(),
                    FinanceReceiptId = receipt.Id,
                    SellOrderId = string.IsNullOrWhiteSpace(item.SellOrderId)
                        ? (string.IsNullOrWhiteSpace(advanceSo) ? null : advanceSo.Trim())
                        : item.SellOrderId.Trim(),
                    SellOrderItemId = item.SellOrderItemId,
                    FinanceSellInvoiceId = item.FinanceSellInvoiceId,
                    FinanceSellInvoiceItemId = item.FinanceSellInvoiceItemId,
                    ReceiptAmount = item.ReceiptAmount,
                    ReceiptConvertAmount = item.ReceiptAmount,
                    StockOutItemId = item.StockOutItemId,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    Remark = item.Remark,
                    ReceiptPurpose = purpose,
                    AdvanceSellOrderId = string.IsNullOrWhiteSpace(advanceSo) ? null : advanceSo.Trim(),
                    VerificationStatus = 0,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(receiptItem);
            }

            receipt.ReceiptPurpose = headerPurpose;

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            var savedItems = (await _itemRepo.FindAsync(i => i.FinanceReceiptId == receipt.Id)).ToList();
            AttachReceiptItems(receipt, savedItems);
            return receipt;
        }

        private static short ResolveReceiptPurpose(short itemPurpose, short headerPurpose)
        {
            if (itemPurpose == FinanceReceiptPurposeCode.Advance)
                return FinanceReceiptPurposeCode.Advance;
            if (headerPurpose == FinanceReceiptPurposeCode.Advance)
                return FinanceReceiptPurposeCode.Advance;
            if (itemPurpose > 0)
                return itemPurpose;
            if (headerPurpose > 0)
                return headerPurpose;
            return FinanceReceiptPurposeCode.Normal;
        }

        public async Task<FinanceReceipt?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var r = await _receiptRepo.GetByIdAsync(id);
            if (r == null) return null;

            var items = (await _itemRepo.FindAsync(i => i.FinanceReceiptId == id)).ToList();
            AttachReceiptItems(r, items);
            await EnrichCreateUserNamesAsync(new List<FinanceReceipt> { r });
            await EnrichFreightForwarderCompanyNamesAsync(new List<FinanceReceipt> { r });
            return r;
        }

        /// <summary>挂载收款明细；主表有金额但无明细行时补一条仅用于展示的占位行（历史数据/仅改主表金额）。</summary>
        private static void AttachReceiptItems(FinanceReceipt receipt, IReadOnlyList<FinanceReceiptItem> items)
        {
            receipt.Items = items
                .OrderBy(i => i.CreateTime)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (receipt.Items.Count > 0)
            {
                receipt.ReceiptPurpose = receipt.Items.Max(i => i.ReceiptPurpose);
                return;
            }

            if (receipt.ReceiptAmount <= 0m) return;

            receipt.Items = new List<FinanceReceiptItem>
            {
                new FinanceReceiptItem
                {
                    FinanceReceiptId = receipt.Id,
                    ReceiptAmount = receipt.ReceiptAmount,
                    ReceiptConvertAmount = receipt.ReceiptAmount,
                    VerificationStatus = 0,
                }
            };
        }

        public async Task<IEnumerable<FinanceReceipt>> GetAllAsync() =>
            await _receiptRepo.GetAllAsync();

        public async Task<PagedResult<FinanceReceipt>> GetPagedAsync(FinanceReceiptQueryRequest request)
        {
            var result = await _receiptListQuery.GetPagedAsync(request);
            var items = result.Items.ToList();
            await EnrichCreateUserNamesAsync(items);
            await EnrichCustomerExtendFieldsAsync(items);
            await EnrichFreightForwarderCompanyNamesAsync(items);
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

            if (request.IsFreightForwarderPayment.HasValue || request.FreightForwarderCompanyId != null)
            {
                var nextFf = request.IsFreightForwarderPayment ?? receipt.IsFreightForwarderPayment;
                var nextCompanyId = request.FreightForwarderCompanyId != null
                    ? (string.IsNullOrWhiteSpace(request.FreightForwarderCompanyId) ? null : request.FreightForwarderCompanyId.Trim())
                    : receipt.FreightForwarderCompanyId;

                var items = (await _itemRepo.FindAsync(i => i.FinanceReceiptId == id)).ToList();
                var hasAdvance = items.Any(i => i.ReceiptPurpose == FinanceReceiptPurposeCode.Advance);
                ValidateFreightForwarderReceiptFlags(nextFf, hasAdvance ? FinanceReceiptPurposeCode.Advance : FinanceReceiptPurposeCode.Normal, nextCompanyId);
                await EnsureFreightForwarderCompanyExistsAsync(nextCompanyId);

                receipt.IsFreightForwarderPayment = nextFf;
                receipt.FreightForwarderCompanyId = nextCompanyId;
            }

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

            var guard = await _forceDeleteGuard.CanForceDeleteFinanceReceiptAsync(receipt.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await DeleteCoreAsync(receipt);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.FinanceReceipt,
                RecordId = receipt.Id,
                RecordCode = receipt.FinanceReceiptCode,
                EntityDisplayName = DeleteLogEntityNames.FinanceReceipt
            });
        }

        private async Task DeleteCoreAsync(FinanceReceipt receipt)
        {
            var id = receipt.Id;
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

            await DeleteCoreAsync(entity);

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.FinanceReceipt,
                RecordId = entity.Id,
                RecordCode = entity.FinanceReceiptCode,
                EntityDisplayName = DeleteLogEntityNames.FinanceReceipt,
                IsForceDelete = true,
                ForceConfirmBillCode = confirmBillCode.Trim(),
                OperatorUserId = actingUserId.Trim(),
                OperatorUserName = actingUserName?.Trim(),
                OperationDescOverride = $"强制删除收款单：Id={entity.Id}，Code={entity.FinanceReceiptCode}"
            });
        }

        /// <inheritdoc />
        public async Task<FinanceReceipt> ReverseVerificationAsync(
            string id,
            string confirmBillCode,
            string actingUserId,
            string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var receipt = await _receiptRepo.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("收款单不存在");
            if (!string.Equals(confirmBillCode.Trim(), receipt.FinanceReceiptCode?.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝反核销");

            if (receipt.Status != ReceiptApproved && receipt.Status != ReceiptReceived)
                throw new InvalidOperationException("仅已审核或已收款状态的收款单可反核销");

            var items = (await _itemRepo.FindAsync(i => i.FinanceReceiptId == receipt.Id)).ToList();
            if (items.Any(i => i.AdvancePoolAmount > 0m))
                throw new ArgumentException("存在客户预收池入账，须先回滚预收池后再反核销");

            var hasVerification = items.Any(i => i.VerificationStatus > 0 || i.VerifiedAmount > 0m);
            var existingWriteOffs = await _receivableService.GetWriteOffsByReceiptIdAsync(receipt.Id);
            if (!hasVerification && existingWriteOffs.Count == 0)
                throw new ArgumentException("当前收款单无需反核销");

            var reverseResult = await _receivableService.ReverseWriteOffsByReceiptAsync(receipt.Id, actingUserId);

            foreach (var item in items.Where(i => i.VerifiedAmount > 0m))
                await ReverseLegacySellInvoiceVerificationAsync(item, actingUserId);

            var syncedSellOrders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.SellOrderId)))
            {
                var sellOrderId = item.SellOrderId!.Trim();
                if (!syncedSellOrders.Add(sellOrderId))
                    continue;
                await SyncSellOrderReceiptStatusAsync(item, actingUserId);
            }

            receipt.ModifyTime = DateTime.UtcNow;
            receipt.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _receiptRepo.UpdateAsync(receipt);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            var recvCodes = reverseResult.ReceivableCodes.Count > 0
                ? string.Join("、", reverseResult.ReceivableCodes)
                : "—";
            var stockOutCodes = reverseResult.StockOutCodes.Count > 0
                ? string.Join("、", reverseResult.StockOutCodes)
                : "—";
            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.FinanceReceipt,
                receipt.Id,
                receipt.FinanceReceiptCode,
                OperationLogActionTypes.FinanceReceiptReverseVerification,
                actingUserId.Trim(),
                actingUserName?.Trim(),
                $"收款反核销：Id={receipt.Id}，Code={receipt.FinanceReceiptCode}，撤销流水 {reverseResult.WriteOffCount} 笔，关联应收：{recvCodes}，出库单：{stockOutCodes}");

            receipt.Items = items;
            await EnrichCreateUserNamesAsync(new[] { receipt });
            return receipt;
        }

        private async Task ReverseLegacySellInvoiceVerificationAsync(FinanceReceiptItem item, string? actingUserId)
        {
            var amount = item.VerifiedAmount;
            if (amount <= 0m)
                return;

            if (!string.IsNullOrWhiteSpace(item.FinanceSellInvoiceId))
            {
                var invoice = await _sellInvoiceRepo.GetByIdAsync(item.FinanceSellInvoiceId);
                if (invoice != null)
                {
                    invoice.ReceiveDone = Math.Max(0m, invoice.ReceiveDone - amount);
                    invoice.ReceiveToBe += amount;
                    if (invoice.ReceiveToBe <= 0)
                        invoice.ReceiveStatus = 2;
                    else if (invoice.ReceiveDone > 0)
                        invoice.ReceiveStatus = 1;
                    else
                        invoice.ReceiveStatus = 0;
                    invoice.ModifyTime = DateTime.UtcNow;
                    invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
                    await _sellInvoiceRepo.UpdateAsync(invoice);

                    var sellInvoiceItems = (await _sellInvoiceItemRepo.FindAsync(x => x.FinanceSellInvoiceId == invoice.Id)).ToList();
                    foreach (var sellItem in sellInvoiceItems)
                    {
                        sellItem.ReceiveStatus = (short)invoice.ReceiveStatus;
                        sellItem.ModifyTime = DateTime.UtcNow;
                        await _sellInvoiceItemRepo.UpdateAsync(sellItem);
                    }
                }
            }

            item.VerifiedAmount = 0m;
            item.VerificationStatus = 0;
            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);

            if (!string.IsNullOrWhiteSpace(item.SellOrderItemId))
                await _sellOrderItemExtendSync.RecalculateAsync(item.SellOrderItemId.Trim());
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

            if (status == 2 || status == 3)
                await _advanceService.TryCreditExplicitAdvanceOnReceiptApprovedAsync(receipt.Id, actingUserId);
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

        private static void ValidateFreightForwarderReceiptFlags(
            bool isFreightForwarderPayment, short receiptPurpose, string? freightForwarderCompanyId)
        {
            if (!isFreightForwarderPayment)
                return;
            if (receiptPurpose == FinanceReceiptPurposeCode.Advance)
                throw new InvalidOperationException("货代付款与预收款不能同时勾选");
        }

        private async Task EnsureFreightForwarderCompanyExistsAsync(string? freightForwarderCompanyId)
        {
            if (string.IsNullOrWhiteSpace(freightForwarderCompanyId))
                return;
            _ = await _ffCompanyRepo.GetByIdAsync(freightForwarderCompanyId.Trim())
                ?? throw new InvalidOperationException("货代公司不存在");
        }

        private async Task EnrichFreightForwarderCompanyNamesAsync(IReadOnlyList<FinanceReceipt> items)
        {
            if (items.Count == 0) return;
            var ids = items
                .Select(r => r.FreightForwarderCompanyId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;

            var companies = (await _ffCompanyRepo.FindAsync(c => ids.Contains(c.Id))).ToList();
            var map = companies.ToDictionary(c => c.Id, c => c.Cname, StringComparer.OrdinalIgnoreCase);
            foreach (var r in items)
            {
                if (!string.IsNullOrWhiteSpace(r.FreightForwarderCompanyId)
                    && map.TryGetValue(r.FreightForwarderCompanyId, out var name))
                    r.FreightForwarderCompanyName = name;
            }
        }
    }
}

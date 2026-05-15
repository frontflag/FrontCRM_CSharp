using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinancePaymentService : IFinancePaymentService
    {
        private readonly IRepository<FinancePayment> _paymentRepo;
        private readonly IRepository<FinancePaymentItem> _itemRepo;
        private readonly IRepository<FinancePaymentBank> _paymentBankRepo;
        private readonly IRepository<PurchaseOrder> _poRepo;
        private readonly IRepository<PurchaseOrderItem> _poItemRepo;
        private readonly IRepository<VendorInfo> _vendorRepo;
        private readonly IRepository<User> _userRepository;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IPurchaseOrderItemExtendSyncService _poItemExtendSync;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IFinancePaymentListQuery _paymentListQuery;

        public FinancePaymentService(
            IRepository<FinancePayment> paymentRepo,
            IRepository<FinancePaymentItem> itemRepo,
            IRepository<FinancePaymentBank> paymentBankRepo,
            IRepository<PurchaseOrder> poRepo,
            IRepository<PurchaseOrderItem> poItemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IRepository<VendorInfo> vendorRepo,
            IRepository<User> userRepository,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            IFinancePaymentListQuery paymentListQuery,
            IUnitOfWork? unitOfWork = null)
        {
            _paymentRepo = paymentRepo;
            _itemRepo = itemRepo;
            _paymentBankRepo = paymentBankRepo;
            _poRepo = poRepo;
            _poItemRepo = poItemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _poItemExtendSync = poItemExtendSync;
            _vendorRepo = vendorRepo;
            _userRepository = userRepository;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _paymentListQuery = paymentListQuery;
            _unitOfWork = unitOfWork;
        }

        private async Task EnrichCreateUserNamesAsync(IReadOnlyList<FinancePayment> items)
        {
            if (items.Count == 0) return;
            var ids = items
                .Select(p => p.CreateByUserId)
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
            foreach (var p in items)
            {
                if (string.IsNullOrWhiteSpace(p.CreateByUserId)) continue;
                if (map.TryGetValue(p.CreateByUserId.Trim(), out var name))
                    p.CreateUserName = name;
            }
        }

        public async Task<FinancePayment> CreateAsync(CreateFinancePaymentRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            await ValidateFinancePaymentBankIdAsync(request.FinancePaymentBankId);

            var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.FinancePayment);
            if (code.Length > 16)
                throw new InvalidOperationException($"生成的财务付款单号超长：{code}");

            var payer = string.IsNullOrWhiteSpace(request.FeeIntermediateBankPayer)
                ? null
                : request.FeeIntermediateBankPayer.Trim();
            if (payer != null && payer.Length > 20)
                payer = payer[..20];

            var payment = new FinancePayment
            {
                Id = Guid.NewGuid().ToString(),
                FinancePaymentCode = code,
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                PaymentAmountToBe = request.PaymentAmountToBe,
                PaymentCurrency = request.PaymentCurrency,
                PaymentDate = PostgreSqlDateTime.ToUtc(request.PaymentDate),
                PaymentUserId = request.PaymentUserId,
                PaymentMode = request.PaymentMode,
                BankSlipNo = request.BankSlipNo,
                FinancePaymentBankId = string.IsNullOrWhiteSpace(request.FinancePaymentBankId)
                    ? null
                    : request.FinancePaymentBankId.Trim(),
                RequestRemark = string.IsNullOrWhiteSpace(request.RequestRemark)
                    ? null
                    : request.RequestRemark.Trim(),
                FeeIntermediateBank = request.FeeIntermediateBank,
                FeeBankCharge = request.FeeBankCharge,
                FeeFreight = request.FeeFreight,
                FeeMisc = request.FeeMisc,
                FeeRounding = request.FeeRounding,
                FeeIntermediateBankPayer = payer,
                Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim(),
                // 新建
                Status = 1,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };
            await _paymentRepo.AddAsync(payment);

            foreach (var item in request.Items)
            {
                var payItem = new FinancePaymentItem
                {
                    Id = Guid.NewGuid().ToString(),
                    FinancePaymentId = payment.Id,
                    PurchaseOrderId = item.PurchaseOrderId,
                    PurchaseOrderItemId = item.PurchaseOrderItemId,
                    PaymentAmountToBe = item.PaymentAmountToBe,
                    VerificationToBe = item.PaymentAmountToBe,
                    ProductId = item.ProductId,
                    PN = item.PN,
                    Brand = item.Brand,
                    LineRemark = string.IsNullOrWhiteSpace(item.LineRemark) ? null : item.LineRemark.Trim(),
                    VerificationStatus = 0,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(payItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            foreach (var pid in request.Items
                         .Select(i => i.PurchaseOrderItemId)
                         .Where(s => !string.IsNullOrWhiteSpace(s))
                         .Select(s => s.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase))
                await _poItemExtendSync.RecalculateAsync(pid);

            await EnrichVendorCodesAsync(new[] { payment });
            await EnrichPaymentBankNamesAsync(new[] { payment });
            await EnrichCreateUserNamesAsync(new[] { payment });
            return payment;
        }

        public async Task<FinancePayment?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var payment = await _paymentRepo.GetByIdAsync(id);
            if (payment == null) return null;

            var items = await _itemRepo.FindAsync(i => i.FinancePaymentId == id);
            payment.Items = items.ToList();
            await EnrichVendorCodesAsync(new[] { payment });
            await EnrichPaymentBankNamesAsync(new[] { payment });
            await EnrichCreateUserNamesAsync(new[] { payment });
            return payment;
        }

        public async Task<IEnumerable<FinancePayment>> GetAllAsync()
        {
            var all = (await _paymentRepo.GetAllAsync()).ToList();
            await EnrichVendorCodesAsync(all);
            await EnrichPaymentBankNamesAsync(all);
            await EnrichCreateUserNamesAsync(all);
            return all;
        }

        public async Task<PagedResult<FinancePayment>> GetPagedAsync(FinancePaymentQueryRequest request)
        {
            var result = await _paymentListQuery.GetPagedAsync(request);
            var items = result.Items.ToList();
            await EnrichVendorCodesAsync(items);
            await EnrichPaymentBankNamesAsync(items);
            await EnrichCreateUserNamesAsync(items);
            return new PagedResult<FinancePayment>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = result.PageIndex,
                PageSize = result.PageSize
            };
        }

        public async Task<FinancePayment> UpdateAsync(string id, UpdateFinancePaymentRequest request, string? actingUserId = null)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");

            if (request.PaymentAmountToBe.HasValue) payment.PaymentAmountToBe = request.PaymentAmountToBe.Value;
            if (request.PaymentCurrency.HasValue) payment.PaymentCurrency = request.PaymentCurrency.Value;
            if (request.PaymentDate.HasValue) payment.PaymentDate = PostgreSqlDateTime.ToUtc(request.PaymentDate.Value);
            if (request.PaymentMode.HasValue) payment.PaymentMode = request.PaymentMode.Value;
            if (request.BankSlipNo != null) payment.BankSlipNo = request.BankSlipNo;
            if (request.FinancePaymentBankId != null)
            {
                await ValidateFinancePaymentBankIdAsync(request.FinancePaymentBankId);
                payment.FinancePaymentBankId = string.IsNullOrWhiteSpace(request.FinancePaymentBankId)
                    ? null
                    : request.FinancePaymentBankId.Trim();
            }
            if (request.RequestRemark != null)
                payment.RequestRemark = string.IsNullOrWhiteSpace(request.RequestRemark) ? null : request.RequestRemark.Trim();
            if (request.FeeIntermediateBank.HasValue) payment.FeeIntermediateBank = request.FeeIntermediateBank.Value;
            if (request.FeeBankCharge.HasValue) payment.FeeBankCharge = request.FeeBankCharge.Value;
            if (request.FeeFreight.HasValue) payment.FeeFreight = request.FeeFreight.Value;
            if (request.FeeMisc.HasValue) payment.FeeMisc = request.FeeMisc.Value;
            if (request.FeeRounding.HasValue) payment.FeeRounding = request.FeeRounding.Value;
            if (request.FeeIntermediateBankPayer != null)
            {
                var payer = string.IsNullOrWhiteSpace(request.FeeIntermediateBankPayer)
                    ? null
                    : request.FeeIntermediateBankPayer.Trim();
                payment.FeeIntermediateBankPayer = payer != null && payer.Length > 20 ? payer[..20] : payer;
            }
            if (request.Remark != null) payment.Remark = request.Remark;
            payment.ModifyTime = DateTime.UtcNow;
            payment.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _paymentRepo.UpdateAsync(payment);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await EnrichVendorCodesAsync(new[] { payment });
            await EnrichPaymentBankNamesAsync(new[] { payment });
            await EnrichCreateUserNamesAsync(new[] { payment });
            return payment;
        }

        public async Task DeleteAsync(string id)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");
            var items = (await _itemRepo.FindAsync(i => i.FinancePaymentId == id)).ToList();
            var poItemIds = items
                .Where(i => !string.IsNullOrWhiteSpace(i.PurchaseOrderItemId))
                .Select(i => i.PurchaseOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var item in items)
                await _itemRepo.DeleteAsync(item.Id);
            await _paymentRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            foreach (var pid in poItemIds)
                await _poItemExtendSync.RecalculateAsync(pid);
        }

        /// <inheritdoc />
        public async Task ForceDeleteAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var entity = await _paymentRepo.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("付款单不存在");
            if (!string.Equals(confirmBillCode.Trim(), entity.FinancePaymentCode?.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            var guard = await _forceDeleteGuard.CanForceDeleteFinancePaymentAsync(entity.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await DeleteAsync(entity.Id);

            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.PurchaseOrder,
                entity.Id,
                string.IsNullOrWhiteSpace(entity.FinancePaymentCode) ? null : entity.FinancePaymentCode.Trim(),
                "付款单强制删除",
                actingUserId.Trim(),
                string.IsNullOrWhiteSpace(actingUserName) ? null : actingUserName.Trim(),
                $"强制删除付款单：Id={entity.Id}，Code={entity.FinancePaymentCode}",
                reason: null);
        }

        public async Task UpdateStatusAsync(string id, short status, string? remark = null, string? actingUserId = null)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");

            // 状态流转：New(1) -> PendingAudit(2) -> Approved(10) -> Completed(100)
            //                            \-> AuditFailed(-1)
            var current = payment.Status;
            var allowed =
                (current == 1 && status == 2) ||      // 提交审核
                (current == -1 && status == 1) ||     // 驳回后回到新建（编辑重提）
                (current == 2 && (status == 10 || status == -1)) || // 审批通过/拒绝
                (current == 10 && status == 100) ||   // 确认付款完成
                (current == 1 && status == -2) ||     // 新建取消
                (current == 2 && status == -2);       // 待审核取消

            if (!allowed)
                throw new InvalidOperationException($"不允许的状态流转: {current} -> {status}");

            payment.Status = status;
            IReadOnlyList<FinancePaymentItem>? completedItems = null;
            if (status == 100) // 付款完成
            {
                payment.PaymentAmount = payment.PaymentAmountToBe;
                payment.PaymentTotalAmount = payment.PaymentAmountToBe;
                payment.PaymentDate ??= DateTime.UtcNow;

                // 付款完成即视为本单各明细核销完毕，回写采购订单明细/主单「付款状态」
                // （否则仅 UpdateStatus 不走 VerifyPaymentItemAsync，PO 会一直显示未付款）
                var payItems = (await _itemRepo.FindAsync(i => i.FinancePaymentId == id)).ToList();
                foreach (var pi in payItems)
                {
                    pi.VerificationDone = pi.PaymentAmountToBe;
                    pi.VerificationToBe = 0m;
                    pi.VerificationStatus = 2;
                    pi.PaymentAmount = pi.PaymentAmountToBe;
                    await _itemRepo.UpdateAsync(pi);
                }

                completedItems = payItems;
            }

            if (!string.IsNullOrWhiteSpace(remark))
                payment.Remark = remark.Trim();

            payment.ModifyTime = DateTime.UtcNow;
            payment.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _paymentRepo.UpdateAsync(payment);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();

            if (completedItems != null)
            {
                foreach (var pi in completedItems.Where(x => !string.IsNullOrWhiteSpace(x.PurchaseOrderItemId)))
                    await SyncPurchaseFinanceStatusAsync(pi);
                if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            }
            else if (status == -2)
            {
                var payItems = (await _itemRepo.FindAsync(i => i.FinancePaymentId == id)).ToList();
                foreach (var pi in payItems.Where(x => !string.IsNullOrWhiteSpace(x.PurchaseOrderItemId)))
                    await _poItemExtendSync.RecalculateAsync(pi.PurchaseOrderItemId!);
            }
        }

        public async Task VerifyPaymentItemAsync(string paymentItemId, decimal amount)
        {
            var item = await _itemRepo.GetByIdAsync(paymentItemId)
                ?? throw new InvalidOperationException($"付款明细 {paymentItemId} 不存在");

            if (amount <= 0)
                throw new ArgumentException("核销金额必须大于0", nameof(amount));

            if (amount > item.VerificationToBe)
                throw new InvalidOperationException($"核销金额超限：待核销 {item.VerificationToBe}，本次 {amount}");

            item.VerificationDone += amount;
            item.VerificationToBe -= amount;
            if (item.VerificationToBe <= 0)
                item.VerificationStatus = 2; // 核销完成
            else if (item.VerificationDone > 0)
                item.VerificationStatus = 1; // 部分核销

            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);

            await SyncPurchaseFinanceStatusAsync(item);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        private async Task SyncPurchaseFinanceStatusAsync(FinancePaymentItem payItem)
        {
            if (string.IsNullOrWhiteSpace(payItem.PurchaseOrderItemId))
                return;

            var poItem = await _poItemRepo.GetByIdAsync(payItem.PurchaseOrderItemId);
            if (poItem == null) return;

            // 同一采购明细允许分多次请款/付款，按累计核销金额计算付款状态
            var relatedPayItems = (await _itemRepo.GetAllAsync())
                .Where(x => x.PurchaseOrderItemId == poItem.Id)
                .ToList();
            var totalToBe = relatedPayItems.Sum(x => x.PaymentAmountToBe);
            var totalDone = relatedPayItems.Sum(x => x.VerificationDone);

            poItem.FinancePaymentStatus = totalDone <= 0
                ? (short)0
                : totalDone >= totalToBe && totalToBe > 0
                    ? (short)2
                    : (short)1;
            poItem.ModifyTime = DateTime.UtcNow;
            await _poItemRepo.UpdateAsync(poItem);

            var po = await _poRepo.GetByIdAsync(poItem.PurchaseOrderId);
            if (po == null) return;

            var allPoItems = (await _poItemRepo.FindAsync(x => x.PurchaseOrderId == po.Id)).ToList();
            po.FinanceStatus = allPoItems.All(x => x.FinancePaymentStatus == 2)
                ? (short)2
                : allPoItems.Any(x => x.FinancePaymentStatus > 0)
                    ? (short)1
                    : (short)0;
            po.ModifyTime = DateTime.UtcNow;
            await _poRepo.UpdateAsync(po);

            await _poItemExtendSync.RecalculateAsync(payItem.PurchaseOrderItemId);
        }

        private async Task ValidateFinancePaymentBankIdAsync(string? bankId)
        {
            if (string.IsNullOrWhiteSpace(bankId)) return;
            var id = bankId.Trim();
            var bank = await _paymentBankRepo.GetByIdAsync(id);
            if (bank == null) throw new ArgumentException("付款银行不存在");
            if (bank.IsDisabled) throw new ArgumentException("所选付款银行已禁用");
        }

        private async Task EnrichPaymentBankNamesAsync(IReadOnlyList<FinancePayment> items)
        {
            var list = items.Where(p => p != null).ToList();
            if (list.Count == 0) return;
            var ids = list
                .Select(p => p.FinancePaymentBankId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) return;
            var banks = (await _paymentBankRepo.FindAsync(b => ids.Contains(b.Id))).ToList();
            var map = banks
                .Where(b => !string.IsNullOrWhiteSpace(b.Id))
                .GroupBy(b => b.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().BankName, StringComparer.OrdinalIgnoreCase);
            foreach (var p in list)
            {
                if (string.IsNullOrWhiteSpace(p.FinancePaymentBankId)) continue;
                var bid = p.FinancePaymentBankId.Trim();
                if (map.TryGetValue(bid, out var name) && !string.IsNullOrWhiteSpace(name))
                    p.PaymentBankName = name.Trim();
            }
        }

        private async Task EnrichVendorCodesAsync(IEnumerable<FinancePayment> payments)
        {
            var list = payments.Where(p => p != null).ToList();
            if (list.Count == 0) return;

            var ids = list
                .Select(p => p.VendorId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (ids.Length == 0) return;

            var vendors = (await _vendorRepo.FindAsync(v => ids.Contains(v.Id))).ToList();
            var map = vendors
                .GroupBy(v => v.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Code, StringComparer.OrdinalIgnoreCase);

            foreach (var p in list)
            {
                if (string.IsNullOrWhiteSpace(p.VendorId)) continue;
                var vid = p.VendorId.Trim();
                if (map.TryGetValue(vid, out var code) && !string.IsNullOrWhiteSpace(code))
                    p.VendorCode = code.Trim();
            }
        }
    }
}

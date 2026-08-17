using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Models.System;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinancePurchaseInvoiceService : IFinancePurchaseInvoiceService
    {
        private readonly IRepository<FinancePurchaseInvoice> _invoiceRepo;
        private readonly IRepository<FinancePurchaseInvoiceItem> _itemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly IPurchaseOrderItemExtendSyncService _poItemExtendSync;
        private readonly IForceDeleteGuardService _forceDeleteGuard;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly IFinancePurchaseInvoiceListQuery _purchaseInvoiceListQuery;
        private readonly IRepository<VendorInfo> _vendorRepo;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IFinancePurchaseInvoicePaymentSyncService _invoicePaymentSync;
        private readonly IFinancePurchaseInvoiceWriteOffService _writeOffService;

        public FinancePurchaseInvoiceService(
            IRepository<FinancePurchaseInvoice> invoiceRepo,
            IRepository<FinancePurchaseInvoiceItem> itemRepo,
            IDataPermissionService dataPermissionService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            IFinancePurchaseInvoiceListQuery purchaseInvoiceListQuery,
            IRepository<VendorInfo> vendorRepo,
            ISerialNumberService serialNumberService,
            IFinancePurchaseInvoicePaymentSyncService invoicePaymentSync,
            IFinancePurchaseInvoiceWriteOffService writeOffService,
            IUnitOfWork? unitOfWork = null)
        {
            _invoiceRepo = invoiceRepo;
            _itemRepo = itemRepo;
            _dataPermissionService = dataPermissionService;
            _poItemExtendSync = poItemExtendSync;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _purchaseInvoiceListQuery = purchaseInvoiceListQuery;
            _vendorRepo = vendorRepo;
            _serialNumberService = serialNumberService;
            _invoicePaymentSync = invoicePaymentSync;
            _writeOffService = writeOffService;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinancePurchaseInvoice> CreateAsync(CreateFinancePurchaseInvoiceRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            var invoiceCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.InputInvoice);

            var currency = request.Currency is >= 1 and <= 3 ? request.Currency : (byte)1;
            var invoiceAmount = request.InvoiceAmount;
            var invoice = new FinancePurchaseInvoice
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                InvoiceCode = invoiceCode,
                InvoiceNo = request.InvoiceNo,
                Currency = currency,
                InvoiceAmount = invoiceAmount,
                BillAmount = request.BillAmount,
                TaxAmount = request.TaxAmount,
                ExcludTaxAmount = request.ExcludTaxAmount,
                InvoiceDate = PostgreSqlDateTime.ToUtc(request.InvoiceDate),
                ConfirmStatus = 0,
                RedInvoiceStatus = 0,
                VerifiedDone = 0m,
                VerifiedToBe = Math.Max(0m, invoiceAmount),
                VerificationStatus = 0,
                PaymentDone = 0m,
                PaymentToBe = 0m,
                PaymentStatus = 0,
                Remark = request.Remark,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };
            await _invoiceRepo.AddAsync(invoice);

            foreach (var item in request.Items)
            {
                var invoiceItem = new FinancePurchaseInvoiceItem
                {
                    Id = Guid.NewGuid().ToString(),
                    FinancePurchaseInvoiceId = invoice.Id,
                    StockInId = item.StockInId,
                    StockInCode = item.StockInCode,
                    PurchaseOrderCode = item.PurchaseOrderCode,
                    StockInCost = item.StockInCost,
                    BillCost = item.BillCost,
                    BillQty = item.BillQty,
                    BillAmount = item.BillAmount,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    ExcludTaxAmount = item.ExcludTaxAmount,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(invoiceItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await _poItemExtendSync.RecalculateForFinancePurchaseInvoiceAsync(invoice.Id);
            return invoice;
        }

        public async Task<FinancePurchaseInvoice?> GetByIdAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id);
            if (invoice != null)
                await EnrichVendorEnglishNamesAsync(new[] { invoice });
            return invoice;
        }

        public async Task<IEnumerable<FinancePurchaseInvoice>> GetAllAsync()
        {
            var all = (await _invoiceRepo.GetAllAsync()).ToList();
            await EnrichVendorEnglishNamesAsync(all);
            return all;
        }

        public async Task<PagedResult<FinancePurchaseInvoice>> GetPagedAsync(FinancePurchaseInvoiceQueryRequest request)
        {
            var result = await _purchaseInvoiceListQuery.GetPagedAsync(request);
            await EnrichVendorEnglishNamesAsync(result.Items);
            return result;
        }

        public async Task<FinancePurchaseInvoice> UpdateAsync(string id, UpdateFinancePurchaseInvoiceRequest request, string? actingUserId = null)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");

            if (request.InvoiceNo != null) invoice.InvoiceNo = request.InvoiceNo;
            if (request.Currency is >= 1 and <= 3) invoice.Currency = request.Currency.Value;
            if (request.InvoiceAmount.HasValue)
            {
                invoice.InvoiceAmount = request.InvoiceAmount.Value;
                invoice.VerifiedToBe = Math.Max(0m, invoice.InvoiceAmount - invoice.VerifiedDone);
                if (invoice.VerifiedDone <= 0m) invoice.VerificationStatus = 0;
                else if (invoice.InvoiceAmount > 0m && invoice.VerifiedDone + 0.0001m >= invoice.InvoiceAmount)
                    invoice.VerificationStatus = 2;
                else invoice.VerificationStatus = 1;
            }
            if (request.InvoiceDate.HasValue) invoice.InvoiceDate = PostgreSqlDateTime.ToUtc(request.InvoiceDate.Value);
            if (request.Remark != null) invoice.Remark = request.Remark;
            invoice.ModifyTime = DateTime.UtcNow;
            invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            if (request.InvoiceAmount.HasValue)
                await _invoicePaymentSync.RecalculateForInvoiceAsync(invoice.Id);
            return invoice;
        }

        public async Task DeleteAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            await DeleteCoreAsync(invoice);
            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.FinancePurchaseInvoice,
                RecordId = invoice.Id,
                RecordCode = invoice.InvoiceCode ?? invoice.InvoiceNo,
                EntityDisplayName = DeleteLogEntityNames.FinancePurchaseInvoice
            });
        }

        private async Task DeleteCoreAsync(FinancePurchaseInvoice invoice)
        {
            var id = invoice.Id;
            var poItemIds = await _poItemExtendSync.ResolvePurchaseOrderItemIdsForFinancePurchaseInvoiceAsync(id);
            var items = (await _itemRepo.FindAsync(i => i.FinancePurchaseInvoiceId == id)).ToList();
            foreach (var item in items)
                await _itemRepo.DeleteAsync(item.Id);
            await _invoiceRepo.DeleteAsync(id);
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

            var entity = await _invoiceRepo.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("进项发票不存在");
            var confirm = confirmBillCode.Trim();
            if (string.IsNullOrWhiteSpace(entity.InvoiceNo) || !string.Equals(confirm, entity.InvoiceNo.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            var guard = await _forceDeleteGuard.CanForceDeleteFinancePurchaseInvoiceAsync(entity.Id);
            if (!guard.CanDelete)
                throw new ArgumentException(guard.Message);

            await DeleteCoreAsync(entity);

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.FinancePurchaseInvoice,
                RecordId = entity.Id,
                RecordCode = entity.InvoiceCode ?? entity.InvoiceNo,
                EntityDisplayName = DeleteLogEntityNames.FinancePurchaseInvoice,
                IsForceDelete = true,
                ForceConfirmBillCode = confirmBillCode.Trim(),
                OperatorUserId = actingUserId.Trim(),
                OperatorUserName = actingUserName?.Trim(),
                OperationDescOverride = $"强制删除进项发票：Id={entity.Id}，InvoiceCode={entity.InvoiceCode}，InvoiceNo={entity.InvoiceNo}"
            });
        }

        /// <inheritdoc />
        public async Task<FinancePurchaseInvoice> ReverseVerificationAsync(
            string id,
            string confirmBillCode,
            string actingUserId,
            string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var invoice = await _invoiceRepo.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("进项发票不存在");
            if (!MatchesConfirmBillCode(invoice, confirmBillCode))
                throw new ArgumentException("确认单号不匹配，已拒绝反核销");

            var reverseResult = await _writeOffService.ReverseByInvoiceAsync(invoice.Id, actingUserId);

            var stockInCodes = reverseResult.StockInCodes.Count > 0
                ? string.Join("、", reverseResult.StockInCodes)
                : "—";
            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.FinancePurchaseInvoice,
                invoice.Id,
                invoice.InvoiceCode ?? invoice.InvoiceNo,
                OperationLogActionTypes.FinancePurchaseInvoiceReverseVerification,
                actingUserId.Trim(),
                actingUserName?.Trim(),
                $"进项发票反核销：Id={invoice.Id}，InvoiceCode={invoice.InvoiceCode}，InvoiceNo={invoice.InvoiceNo}，撤销流水 {reverseResult.WriteOffCount} 笔，关联入库：{stockInCodes}");

            return await GetByIdAsync(invoice.Id) ?? invoice;
        }

        private static bool MatchesConfirmBillCode(FinancePurchaseInvoice invoice, string confirmBillCode)
        {
            var confirm = confirmBillCode.Trim();
            if (string.IsNullOrWhiteSpace(confirm))
                return false;
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceCode) &&
                string.Equals(confirm, invoice.InvoiceCode.Trim(), StringComparison.Ordinal))
                return true;
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNo) &&
                string.Equals(confirm, invoice.InvoiceNo.Trim(), StringComparison.Ordinal))
                return true;
            return false;
        }

        public async Task ConfirmAsync(string id, DateTime confirmDate, string? actingUserId = null)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            if (invoice.RedInvoiceStatus == 1)
                throw new InvalidOperationException("已冲红的进项发票不允许认证");
            if (invoice.ConfirmStatus == 1)
                throw new InvalidOperationException("该进项发票已认证");
            if (invoice.InvoiceAmount <= 0)
                throw new InvalidOperationException("发票金额必须大于0才能认证");
            invoice.ConfirmStatus = 1;
            invoice.ConfirmDate = PostgreSqlDateTime.ToUtc(confirmDate);
            invoice.ModifyTime = DateTime.UtcNow;
            invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await _poItemExtendSync.RecalculateForFinancePurchaseInvoiceAsync(id);
        }

        public async Task UnconfirmAsync(string id, string? actingUserId = null)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            if (invoice.RedInvoiceStatus == 1)
                throw new InvalidOperationException("已冲红的进项发票不允许取消认证");
            if (invoice.ConfirmStatus != 1)
                throw new InvalidOperationException("当前发票未认证，无需取消认证");
            invoice.ConfirmStatus = 0;
            invoice.ConfirmDate = null;
            invoice.ModifyTime = DateTime.UtcNow;
            invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await _poItemExtendSync.RecalculateForFinancePurchaseInvoiceAsync(id);
        }

        public async Task RedInvoiceAsync(string id, string? actingUserId = null)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            if (invoice.RedInvoiceStatus == 1)
                throw new InvalidOperationException("该进项发票已冲红");
            if (invoice.ConfirmStatus == 1)
                throw new InvalidOperationException("已认证的进项发票不允许直接冲红，请先执行财务冲销流程");
            invoice.RedInvoiceStatus = 1;
            invoice.ModifyTime = DateTime.UtcNow;
            invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            await _poItemExtendSync.RecalculateForFinancePurchaseInvoiceAsync(id);
        }

        private async Task EnrichVendorEnglishNamesAsync(IEnumerable<FinancePurchaseInvoice> items)
        {
            var list = items.Where(x => x != null).ToList();
            if (list.Count == 0) return;

            var englishMap = await VendorDisplayEnrichment.LoadEnglishOfficialNameMapAsync(
                _vendorRepo,
                list.Select(x => x.VendorId));
            foreach (var inv in list)
                inv.VendorEnglishName = VendorDisplayEnrichment.ResolveEnglishOfficialName(englishMap, inv.VendorId);
        }
    }
}

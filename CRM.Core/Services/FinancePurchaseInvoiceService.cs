using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
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

        public FinancePurchaseInvoiceService(
            IRepository<FinancePurchaseInvoice> invoiceRepo,
            IRepository<FinancePurchaseInvoiceItem> itemRepo,
            IDataPermissionService dataPermissionService,
            IPurchaseOrderItemExtendSyncService poItemExtendSync,
            IForceDeleteGuardService forceDeleteGuard,
            ILogOperationAppendService logOperationAppend,
            IFinancePurchaseInvoiceListQuery purchaseInvoiceListQuery,
            IUnitOfWork? unitOfWork = null)
        {
            _invoiceRepo = invoiceRepo;
            _itemRepo = itemRepo;
            _dataPermissionService = dataPermissionService;
            _poItemExtendSync = poItemExtendSync;
            _forceDeleteGuard = forceDeleteGuard;
            _logOperationAppend = logOperationAppend;
            _purchaseInvoiceListQuery = purchaseInvoiceListQuery;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinancePurchaseInvoice> CreateAsync(CreateFinancePurchaseInvoiceRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            var invoice = new FinancePurchaseInvoice
            {
                Id = Guid.NewGuid().ToString(),
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                InvoiceNo = request.InvoiceNo,
                InvoiceAmount = request.InvoiceAmount,
                BillAmount = request.BillAmount,
                TaxAmount = request.TaxAmount,
                ExcludTaxAmount = request.ExcludTaxAmount,
                InvoiceDate = PostgreSqlDateTime.ToUtc(request.InvoiceDate),
                ConfirmStatus = 0,
                RedInvoiceStatus = 0,
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

        public async Task<FinancePurchaseInvoice?> GetByIdAsync(string id) =>
            await _invoiceRepo.GetByIdAsync(id);

        public async Task<IEnumerable<FinancePurchaseInvoice>> GetAllAsync() =>
            await _invoiceRepo.GetAllAsync();

        public async Task<PagedResult<FinancePurchaseInvoice>> GetPagedAsync(FinancePurchaseInvoiceQueryRequest request) =>
            await _purchaseInvoiceListQuery.GetPagedAsync(request);

        public async Task<FinancePurchaseInvoice> UpdateAsync(string id, UpdateFinancePurchaseInvoiceRequest request, string? actingUserId = null)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");

            if (request.InvoiceNo != null) invoice.InvoiceNo = request.InvoiceNo;
            if (request.InvoiceAmount.HasValue) invoice.InvoiceAmount = request.InvoiceAmount.Value;
            if (request.InvoiceDate.HasValue) invoice.InvoiceDate = PostgreSqlDateTime.ToUtc(request.InvoiceDate.Value);
            if (request.Remark != null) invoice.Remark = request.Remark;
            invoice.ModifyTime = DateTime.UtcNow;
            invoice.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return invoice;
        }

        public async Task DeleteAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
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

            await DeleteAsync(entity.Id);

            await _logOperationAppend.AppendAsync(
                BusinessLogTypes.PurchaseOrder,
                entity.Id,
                string.IsNullOrWhiteSpace(entity.InvoiceNo) ? null : entity.InvoiceNo.Trim(),
                "进项发票强制删除",
                actingUserId.Trim(),
                string.IsNullOrWhiteSpace(actingUserName) ? null : actingUserName.Trim(),
                $"强制删除进项发票：Id={entity.Id}，InvoiceNo={entity.InvoiceNo}",
                reason: null);
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
    }
}

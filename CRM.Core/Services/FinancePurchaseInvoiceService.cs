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

        public FinancePurchaseInvoiceService(
            IRepository<FinancePurchaseInvoice> invoiceRepo,
            IRepository<FinancePurchaseInvoiceItem> itemRepo,
            IDataPermissionService dataPermissionService,
            IUnitOfWork? unitOfWork = null)
        {
            _invoiceRepo = invoiceRepo;
            _itemRepo = itemRepo;
            _dataPermissionService = dataPermissionService;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinancePurchaseInvoice> CreateAsync(CreateFinancePurchaseInvoiceRequest request)
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
                CreateTime = DateTime.UtcNow
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
            return invoice;
        }

        public async Task<FinancePurchaseInvoice?> GetByIdAsync(string id) =>
            await _invoiceRepo.GetByIdAsync(id);

        public async Task<IEnumerable<FinancePurchaseInvoice>> GetAllAsync() =>
            await _invoiceRepo.GetAllAsync();

        public async Task<PagedResult<FinancePurchaseInvoice>> GetPagedAsync(FinancePurchaseInvoiceQueryRequest request)
        {
            var all = await _invoiceRepo.GetAllAsync();
            var filteredByPermission = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                filteredByPermission = await _dataPermissionService.FilterFinancePurchaseInvoicesAsync(request.CurrentUserId, filteredByPermission);
            }

            var query = filteredByPermission.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(inv =>
                    (!string.IsNullOrWhiteSpace(inv.VendorName) && inv.VendorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(inv.InvoiceNo) && inv.InvoiceNo.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.ConfirmStatus.HasValue)
                query = query.Where(inv => inv.ConfirmStatus == request.ConfirmStatus.Value);

            if (request.InvoiceStatus.HasValue)
                query = query.Where(inv => inv.RedInvoiceStatus == request.InvoiceStatus.Value);

            if (request.StartDate.HasValue)
                query = query.Where(inv => inv.InvoiceDate >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(inv => inv.InvoiceDate <= request.EndDate.Value.AddDays(1));

            var totalCount = query.Count();
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var items = query.OrderByDescending(inv => inv.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<FinancePurchaseInvoice>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<FinancePurchaseInvoice> UpdateAsync(string id, UpdateFinancePurchaseInvoiceRequest request)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");

            if (request.InvoiceNo != null) invoice.InvoiceNo = request.InvoiceNo;
            if (request.InvoiceAmount.HasValue) invoice.InvoiceAmount = request.InvoiceAmount.Value;
            if (request.InvoiceDate.HasValue) invoice.InvoiceDate = PostgreSqlDateTime.ToUtc(request.InvoiceDate.Value);
            if (request.Remark != null) invoice.Remark = request.Remark;
            invoice.ModifyTime = DateTime.UtcNow;

            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return invoice;
        }

        public async Task DeleteAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            var items = await _itemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.FinancePurchaseInvoiceId == id))
                await _itemRepo.DeleteAsync(item.Id);
            await _invoiceRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task ConfirmAsync(string id, DateTime confirmDate)
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
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UnconfirmAsync(string id)
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
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task RedInvoiceAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            if (invoice.RedInvoiceStatus == 1)
                throw new InvalidOperationException("该进项发票已冲红");
            if (invoice.ConfirmStatus == 1)
                throw new InvalidOperationException("已认证的进项发票不允许直接冲红，请先执行财务冲销流程");
            invoice.RedInvoiceStatus = 1;
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}

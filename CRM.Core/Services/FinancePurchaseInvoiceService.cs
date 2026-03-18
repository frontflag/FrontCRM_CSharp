using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    public class FinancePurchaseInvoiceService : IFinancePurchaseInvoiceService
    {
        private readonly IRepository<FinancePurchaseInvoice> _invoiceRepo;
        private readonly IRepository<FinancePurchaseInvoiceItem> _itemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public FinancePurchaseInvoiceService(
            IRepository<FinancePurchaseInvoice> invoiceRepo,
            IRepository<FinancePurchaseInvoiceItem> itemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _invoiceRepo = invoiceRepo;
            _itemRepo = itemRepo;
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
                InvoiceDate = request.InvoiceDate,
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

        public async Task<FinancePurchaseInvoice> UpdateAsync(string id, UpdateFinancePurchaseInvoiceRequest request)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");

            if (request.InvoiceNo != null) invoice.InvoiceNo = request.InvoiceNo;
            if (request.InvoiceAmount.HasValue) invoice.InvoiceAmount = request.InvoiceAmount.Value;
            if (request.InvoiceDate.HasValue) invoice.InvoiceDate = request.InvoiceDate.Value;
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
            invoice.ConfirmStatus = 1;
            invoice.ConfirmDate = confirmDate;
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task RedInvoiceAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"进项发票 {id} 不存在");
            invoice.RedInvoiceStatus = 1;
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}

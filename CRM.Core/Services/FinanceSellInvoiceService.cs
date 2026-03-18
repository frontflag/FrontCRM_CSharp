using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    public class FinanceSellInvoiceService : IFinanceSellInvoiceService
    {
        private readonly IRepository<FinanceSellInvoice> _invoiceRepo;
        private readonly IRepository<SellInvoiceItem> _itemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public FinanceSellInvoiceService(
            IRepository<FinanceSellInvoice> invoiceRepo,
            IRepository<SellInvoiceItem> itemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _invoiceRepo = invoiceRepo;
            _itemRepo = itemRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinanceSellInvoice> CreateAsync(CreateFinanceSellInvoiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            var invoice = new FinanceSellInvoice
            {
                Id = Guid.NewGuid().ToString(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                InvoiceCode = request.InvoiceCode,
                InvoiceNo = request.InvoiceNo,
                InvoiceTotal = request.InvoiceTotal,
                MakeInvoiceDate = request.MakeInvoiceDate,
                ReceiveStatus = 0,
                ReceiveDone = 0m,
                ReceiveToBe = request.InvoiceTotal,
                Currency = request.Currency,
                Type = request.Type,
                InvoiceStatus = 1,
                SellInvoiceType = request.SellInvoiceType,
                Remark = request.Remark,
                CreateTime = DateTime.UtcNow
            };
            await _invoiceRepo.AddAsync(invoice);

            foreach (var item in request.Items)
            {
                var invoiceItem = new SellInvoiceItem
                {
                    Id = Guid.NewGuid().ToString(),
                    FinanceSellInvoiceId = invoice.Id,
                    InvoiceTotal = item.InvoiceTotal,
                    TaxRate = item.TaxRate,
                    ValueAddedTax = item.ValueAddedTax,
                    TaxFreeTotal = item.TaxFreeTotal,
                    Price = item.Price,
                    Qty = item.Qty,
                    StockOutItemId = item.StockOutItemId,
                    Currency = item.Currency,
                    ReceiveStatus = 0,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(invoiceItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return invoice;
        }

        public async Task<FinanceSellInvoice?> GetByIdAsync(string id) =>
            await _invoiceRepo.GetByIdAsync(id);

        public async Task<IEnumerable<FinanceSellInvoice>> GetAllAsync() =>
            await _invoiceRepo.GetAllAsync();

        public async Task<FinanceSellInvoice> UpdateAsync(string id, UpdateFinanceSellInvoiceRequest request)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销项发票 {id} 不存在");

            if (request.InvoiceNo != null) invoice.InvoiceNo = request.InvoiceNo;
            if (request.InvoiceTotal.HasValue) invoice.InvoiceTotal = request.InvoiceTotal.Value;
            if (request.MakeInvoiceDate.HasValue) invoice.MakeInvoiceDate = request.MakeInvoiceDate.Value;
            if (request.Remark != null) invoice.Remark = request.Remark;
            invoice.ModifyTime = DateTime.UtcNow;

            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return invoice;
        }

        public async Task DeleteAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销项发票 {id} 不存在");
            var items = await _itemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.FinanceSellInvoiceId == id))
                await _itemRepo.DeleteAsync(item.Id);
            await _invoiceRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateInvoiceStatusAsync(string id, short invoiceStatus)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销项发票 {id} 不存在");
            invoice.InvoiceStatus = invoiceStatus;
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task VoidAsync(string id)
        {
            var invoice = await _invoiceRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"销项发票 {id} 不存在");
            invoice.InvoiceStatus = -1; // 已作废
            invoice.ModifyTime = DateTime.UtcNow;
            await _invoiceRepo.UpdateAsync(invoice);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}

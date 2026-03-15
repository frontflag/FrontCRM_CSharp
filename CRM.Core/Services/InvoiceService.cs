using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    /// <summary>
    /// 发票服务实现
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IRepository<Invoice> _invoiceRepository;

        public InvoiceService(IRepository<Invoice> invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<Invoice> CreateAsync(CreateInvoiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.InvoiceCode))
                throw new ArgumentException("发票代码不能为空", nameof(request.InvoiceCode));

            if (string.IsNullOrWhiteSpace(request.InvoiceNo))
                throw new ArgumentException("发票号码不能为空", nameof(request.InvoiceNo));

            // 检查发票号码是否已存在
            var allInvoices = await _invoiceRepository.GetAllAsync();
            if (allInvoices.Any(i => i.InvoiceNo == request.InvoiceNo))
                throw new InvalidOperationException($"发票号码 {request.InvoiceNo} 已存在");

            var invoice = new Invoice
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceCode = request.InvoiceCode.Trim(),
                InvoiceNo = request.InvoiceNo.Trim(),
                InvoiceType = request.InvoiceType,
                OrderType = request.InvoiceType == 1 ? "PurchaseOrder" : "SellOrder",
                OrderId = request.InvoiceType == 1 ? request.PurchaseOrderId : request.SalesOrderId,
                CustomerId = request.CustomerId,
                VendorId = request.VendorId,
                InvoiceDate = request.InvoiceDate,
                Amount = request.Amount,
                TaxAmount = request.TaxAmount,
                TotalAmount = request.TotalAmount,
                ScanFilePath = request.InvoiceUrl,
                Status = 0, // 待认证
                CreateTime = DateTime.UtcNow
            };

            await _invoiceRepository.AddAsync(invoice);
            return invoice;
        }

        public async Task<Invoice?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _invoiceRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _invoiceRepository.GetAllAsync();
        }

        public async Task<Invoice> UpdateAsync(string id, UpdateInvoiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                throw new InvalidOperationException($"发票 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                invoice.Remark = request.Remark;

            invoice.ModifyTime = DateTime.UtcNow;

            await _invoiceRepository.UpdateAsync(invoice);
            return invoice;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                throw new InvalidOperationException($"发票 {id} 不存在");

            await _invoiceRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                throw new InvalidOperationException($"发票 {id} 不存在");

            invoice.Status = status;
            invoice.ModifyTime = DateTime.UtcNow;

            await _invoiceRepository.UpdateAsync(invoice);
        }
    }
}

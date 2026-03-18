using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;

namespace CRM.Core.Services
{
    public class FinancePaymentService : IFinancePaymentService
    {
        private readonly IRepository<FinancePayment> _paymentRepo;
        private readonly IRepository<FinancePaymentItem> _itemRepo;
        private readonly IUnitOfWork? _unitOfWork;

        public FinancePaymentService(
            IRepository<FinancePayment> paymentRepo,
            IRepository<FinancePaymentItem> itemRepo,
            IUnitOfWork? unitOfWork = null)
        {
            _paymentRepo = paymentRepo;
            _itemRepo = itemRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinancePayment> CreateAsync(CreateFinancePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            var payment = new FinancePayment
            {
                Id = Guid.NewGuid().ToString(),
                FinancePaymentCode = request.FinancePaymentCode,
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                PaymentAmountToBe = request.PaymentAmountToBe,
                PaymentCurrency = request.PaymentCurrency,
                PaymentDate = request.PaymentDate,
                PaymentUserId = request.PaymentUserId,
                PaymentMode = request.PaymentMode,
                Remark = request.Remark,
                Status = 0,
                CreateTime = DateTime.UtcNow
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
                    VerificationStatus = 0,
                    CreateTime = DateTime.UtcNow
                };
                await _itemRepo.AddAsync(payItem);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return payment;
        }

        public async Task<FinancePayment?> GetByIdAsync(string id) =>
            await _paymentRepo.GetByIdAsync(id);

        public async Task<IEnumerable<FinancePayment>> GetAllAsync() =>
            await _paymentRepo.GetAllAsync();

        public async Task<FinancePayment> UpdateAsync(string id, UpdateFinancePaymentRequest request)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");

            if (request.PaymentAmountToBe.HasValue) payment.PaymentAmountToBe = request.PaymentAmountToBe.Value;
            if (request.PaymentDate.HasValue) payment.PaymentDate = request.PaymentDate.Value;
            if (request.PaymentMode.HasValue) payment.PaymentMode = request.PaymentMode.Value;
            if (request.Remark != null) payment.Remark = request.Remark;
            payment.ModifyTime = DateTime.UtcNow;

            await _paymentRepo.UpdateAsync(payment);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
            return payment;
        }

        public async Task DeleteAsync(string id)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");
            var items = await _itemRepo.GetAllAsync();
            foreach (var item in items.Where(i => i.FinancePaymentId == id))
                await _itemRepo.DeleteAsync(item.Id);
            await _paymentRepo.DeleteAsync(id);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");
            payment.Status = status;
            if (status == 3) // 已付款
            {
                payment.PaymentAmount = payment.PaymentAmountToBe;
                payment.PaymentTotalAmount = payment.PaymentAmountToBe;
                payment.PaymentDate ??= DateTime.UtcNow;
            }
            payment.ModifyTime = DateTime.UtcNow;
            await _paymentRepo.UpdateAsync(payment);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }

        public async Task VerifyPaymentItemAsync(string paymentItemId, decimal amount)
        {
            var item = await _itemRepo.GetByIdAsync(paymentItemId)
                ?? throw new InvalidOperationException($"付款明细 {paymentItemId} 不存在");

            item.VerificationDone += amount;
            item.VerificationToBe -= amount;
            if (item.VerificationToBe <= 0)
                item.VerificationStatus = 2; // 核销完成
            else if (item.VerificationDone > 0)
                item.VerificationStatus = 1; // 部分核销

            item.ModifyTime = DateTime.UtcNow;
            await _itemRepo.UpdateAsync(item);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync();
        }
    }
}

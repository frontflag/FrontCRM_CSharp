using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// 付款服务实现
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<PaymentRequest> _paymentRequestRepository;

        public PaymentService(
            IRepository<Payment> paymentRepository,
            IRepository<PaymentRequest> paymentRequestRepository)
        {
            _paymentRepository = paymentRepository;
            _paymentRequestRepository = paymentRequestRepository;
        }

        public async Task<PaymentRequest> CreatePaymentRequestAsync(CreatePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestCode))
                throw new ArgumentException("请款单号不能为空", nameof(request.RequestCode));

            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            // 检查请款单号是否已存在
            var allRequests = await _paymentRequestRepository.GetAllAsync();
            if (allRequests.Any(r => r.RequestCode == request.RequestCode))
                throw new InvalidOperationException($"请款单号 {request.RequestCode} 已存在");

            var paymentRequest = new PaymentRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestCode = request.RequestCode.Trim(),
                PurchaseOrderId = request.PurchaseOrderId,
                VendorId = request.VendorId,
                RequestUserId = request.RequestUserId,
                RequestDate = PostgreSqlDateTime.ToUtc(request.RequestDate),
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Remark = request.Remark,
                Status = 0, // 待审批
                CreateTime = DateTime.UtcNow
            };

            await _paymentRequestRepository.AddAsync(paymentRequest);
            return paymentRequest;
        }

        public async Task<Payment> ApproveAndPayAsync(ApprovePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentRequestId))
                throw new ArgumentException("请款单ID不能为空", nameof(request.PaymentRequestId));

            var paymentRequest = await _paymentRequestRepository.GetByIdAsync(request.PaymentRequestId);
            if (paymentRequest == null)
                throw new InvalidOperationException($"请款单 {request.PaymentRequestId} 不存在");

            // 创建付款记录
            var payment = new Payment
            {
                Id = Guid.NewGuid().ToString(),
                PaymentCode = $"PAY{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}",
                PurchaseOrderId = paymentRequest.PurchaseOrderId,
                VendorId = paymentRequest.VendorId,
                ApplyAmount = paymentRequest.Amount,
                PaymentAmount = paymentRequest.Amount,
                Currency = paymentRequest.Currency,
                PaymentMethod = paymentRequest.PaymentMethod,
                PaymentDate = PostgreSqlDateTime.ToUtc(request.ActualPaymentDate),
                Status = 3, // 已付款
                CreateTime = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);

            // 更新请款单状态
            paymentRequest.Status = 2; // 已付款
            paymentRequest.ModifyTime = DateTime.UtcNow;
            await _paymentRequestRepository.UpdateAsync(paymentRequest);

            return payment;
        }

        public async Task<Payment?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _paymentRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            return await _paymentRepository.GetAllAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new InvalidOperationException($"付款记录 {id} 不存在");

            payment.Status = status;
            payment.ModifyTime = DateTime.UtcNow;

            await _paymentRepository.UpdateAsync(payment);
        }
    }
}

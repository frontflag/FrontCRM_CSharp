using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class FinancePaymentService : IFinancePaymentService
    {
        private readonly IRepository<FinancePayment> _paymentRepo;
        private readonly IRepository<FinancePaymentItem> _itemRepo;
        private readonly IDataPermissionService _dataPermissionService;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public FinancePaymentService(
            IRepository<FinancePayment> paymentRepo,
            IRepository<FinancePaymentItem> itemRepo,
            IDataPermissionService dataPermissionService,
            ISerialNumberService serialNumberService,
            IUnitOfWork? unitOfWork = null)
        {
            _paymentRepo = paymentRepo;
            _itemRepo = itemRepo;
            _dataPermissionService = dataPermissionService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<FinancePayment> CreateAsync(CreateFinancePaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            var code = await _serialNumberService.GenerateNextAsync(ModuleCodes.FinancePayment);
            if (code.Length > 16)
                throw new InvalidOperationException($"生成的财务付款单号超长：{code}");

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
                Remark = request.Remark,
                // 新建
                Status = 1,
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

        public async Task<FinancePayment?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            var payment = await _paymentRepo.GetByIdAsync(id);
            if (payment == null) return null;

            var items = await _itemRepo.FindAsync(i => i.FinancePaymentId == id);
            payment.Items = items.ToList();
            return payment;
        }

        public async Task<IEnumerable<FinancePayment>> GetAllAsync() =>
            await _paymentRepo.GetAllAsync();

        public async Task<PagedResult<FinancePayment>> GetPagedAsync(FinancePaymentQueryRequest request)
        {
            var all = await _paymentRepo.GetAllAsync();
            var filteredByPermission = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.CurrentUserId))
            {
                filteredByPermission = await _dataPermissionService.FilterFinancePaymentsAsync(request.CurrentUserId, filteredByPermission);
            }

            var query = filteredByPermission.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();
                query = query.Where(p =>
                    (!string.IsNullOrWhiteSpace(p.FinancePaymentCode) && p.FinancePaymentCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.VendorName) && p.VendorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.Status.HasValue)
                query = query.Where(p => p.Status == request.Status.Value);

            if (request.StartDate.HasValue)
                query = query.Where(p => p.CreateTime >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(p => p.CreateTime <= request.EndDate.Value.AddDays(1));

            var totalCount = query.Count();
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var items = query.OrderByDescending(p => p.CreateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<FinancePayment>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };
        }

        public async Task<FinancePayment> UpdateAsync(string id, UpdateFinancePaymentRequest request)
        {
            var payment = await _paymentRepo.GetByIdAsync(id)
                ?? throw new InvalidOperationException($"付款单 {id} 不存在");

            if (request.PaymentAmountToBe.HasValue) payment.PaymentAmountToBe = request.PaymentAmountToBe.Value;
            if (request.PaymentDate.HasValue) payment.PaymentDate = PostgreSqlDateTime.ToUtc(request.PaymentDate.Value);
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

        public async Task UpdateStatusAsync(string id, short status, string? remark = null)
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
            if (status == 100) // 付款完成
            {
                payment.PaymentAmount = payment.PaymentAmountToBe;
                payment.PaymentTotalAmount = payment.PaymentAmountToBe;
                payment.PaymentDate ??= DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(remark))
                payment.Remark = remark.Trim();

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

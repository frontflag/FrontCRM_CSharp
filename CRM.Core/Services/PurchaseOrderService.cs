using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services
{
    /// <summary>
    /// 采购订单服务实现
    /// </summary>
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;

        public PurchaseOrderService(IRepository<PurchaseOrder> purchaseOrderRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        public async Task<PurchaseOrder> CreateAsync(CreatePurchaseOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderCode))
                throw new ArgumentException("订单号不能为空", nameof(request.OrderCode));

            if (string.IsNullOrWhiteSpace(request.VendorId))
                throw new ArgumentException("供应商ID不能为空", nameof(request.VendorId));

            // 检查订单号是否已存在
            var allOrders = await _purchaseOrderRepository.GetAllAsync();
            if (allOrders.Any(o => o.PurchaseOrderCode == request.OrderCode))
                throw new InvalidOperationException($"采购订单号 {request.OrderCode} 已存在");

            var order = new PurchaseOrder
            {
                Id = Guid.NewGuid().ToString(),
                PurchaseOrderCode = request.OrderCode.Trim(),
                VendorId = request.VendorId,
                PurchaseUserId = request.PurchaseUserId,
                DeliveryDate = request.DeliveryDate,
                Total = request.TotalAmount,
                ConvertTotal = request.TotalAmount, // 简化处理
                Currency = request.Currency,
                Remark = request.PaymentTerms,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _purchaseOrderRepository.AddAsync(order);
            return order;
        }

        public async Task<PurchaseOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _purchaseOrderRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            return await _purchaseOrderRepository.GetAllAsync();
        }

        public async Task<PurchaseOrder> UpdateAsync(string id, UpdatePurchaseOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _purchaseOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"采购订单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                order.Remark = request.Remark;

            order.ModifyTime = DateTime.UtcNow;

            await _purchaseOrderRepository.UpdateAsync(order);
            return order;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _purchaseOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"采购订单 {id} 不存在");

            await _purchaseOrderRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _purchaseOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"采购订单 {id} 不存在");

            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;

            await _purchaseOrderRepository.UpdateAsync(order);
        }
    }
}

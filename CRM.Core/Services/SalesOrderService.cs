using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;

namespace CRM.Core.Services
{
    /// <summary>
    /// 销售订单服务实现
    /// </summary>
    public class SalesOrderService : ISalesOrderService
    {
        private readonly IRepository<SellOrder> _salesOrderRepository;

        public SalesOrderService(IRepository<SellOrder> salesOrderRepository)
        {
            _salesOrderRepository = salesOrderRepository;
        }

        public async Task<SellOrder> CreateAsync(CreateSalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OrderCode))
                throw new ArgumentException("订单号不能为空", nameof(request.OrderCode));

            if (string.IsNullOrWhiteSpace(request.CustomerId))
                throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

            // 检查订单号是否已存在
            var allOrders = await _salesOrderRepository.GetAllAsync();
            if (allOrders.Any(o => o.SellOrderCode == request.OrderCode))
                throw new InvalidOperationException($"销售订单号 {request.OrderCode} 已存在");

            var order = new SellOrder
            {
                Id = Guid.NewGuid().ToString(),
                SellOrderCode = request.OrderCode.Trim(),
                CustomerId = request.CustomerId,
                SalesUserId = request.SalesUserId,
                DeliveryDate = request.DeliveryDate,
                Total = request.TotalAmount,
                ConvertTotal = request.GrandTotal,
                Currency = request.Currency,
                Remark = request.PaymentTerms,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _salesOrderRepository.AddAsync(order);
            return order;
        }

        public async Task<SellOrder?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _salesOrderRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<SellOrder>> GetAllAsync()
        {
            return await _salesOrderRepository.GetAllAsync();
        }

        public async Task<SellOrder> UpdateAsync(string id, UpdateSalesOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _salesOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"销售订单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                order.Remark = request.Remark;

            order.ModifyTime = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(order);
            return order;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _salesOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"销售订单 {id} 不存在");

            await _salesOrderRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _salesOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"销售订单 {id} 不存在");

            order.Status = status;
            order.ModifyTime = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(order);
        }

        public async Task RequestStockOutAsync(string id, string requestedBy)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var order = await _salesOrderRepository.GetByIdAsync(id);
            if (order == null)
                throw new InvalidOperationException($"销售订单 {id} 不存在");

            // 更新状态为待出库
            order.StockOutStatus = 1; // 待出库
            order.ModifyTime = DateTime.UtcNow;

            await _salesOrderRepository.UpdateAsync(order);
        }
    }
}

using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    /// <summary>
    /// 入库服务实现
    /// </summary>
    public class StockInService : IStockInService
    {
        private readonly IRepository<StockIn> _stockInRepository;

        public StockInService(IRepository<StockIn> stockInRepository)
        {
            _stockInRepository = stockInRepository;
        }

        public async Task<StockIn> CreateAsync(CreateStockInRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockInCode))
                throw new ArgumentException("入库单号不能为空", nameof(request.StockInCode));

            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            // 检查入库单号是否已存在
            var allStockIns = await _stockInRepository.GetAllAsync();
            if (allStockIns.Any(s => s.StockInCode == request.StockInCode))
                throw new InvalidOperationException($"入库单号 {request.StockInCode} 已存在");

            var stockIn = new StockIn
            {
                Id = Guid.NewGuid().ToString(),
                StockInCode = request.StockInCode.Trim(),
                StockInType = 1, // 采购入库
                SourceCode = request.PurchaseOrderId, // 使用SourceCode存储关联订单ID
                WarehouseId = request.WarehouseId,
                VendorId = request.VendorId,
                StockInDate = request.StockInDate,
                TotalQuantity = request.TotalQuantity,
                Remark = request.Remark,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _stockInRepository.AddAsync(stockIn);
            return stockIn;
        }

        public async Task<StockIn?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _stockInRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<StockIn>> GetAllAsync()
        {
            return await _stockInRepository.GetAllAsync();
        }

        public async Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                stockIn.Remark = request.Remark;

            stockIn.ModifyTime = DateTime.UtcNow;

            await _stockInRepository.UpdateAsync(stockIn);
            return stockIn;
        }

        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            await _stockInRepository.DeleteAsync(id);
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            stockIn.Status = status;
            stockIn.ModifyTime = DateTime.UtcNow;

            await _stockInRepository.UpdateAsync(stockIn);
        }
    }
}

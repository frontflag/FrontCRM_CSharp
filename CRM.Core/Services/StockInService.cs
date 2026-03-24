using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    /// <summary>
    /// 入库服务实现
    /// </summary>
    public class StockInService : IStockInService
    {
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly ILogisticsService _logisticsService;
        private readonly IUnitOfWork _unitOfWork;

        public StockInService(
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            ILogisticsService logisticsService,
            IUnitOfWork unitOfWork)
        {
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _logisticsService = logisticsService;
            _unitOfWork = unitOfWork;
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

            var stockInId = Guid.NewGuid().ToString();
            var purchaseOrderId = request.PurchaseOrderId?.Trim();
            var stockIn = new StockIn
            {
                Id = stockInId,
                StockInCode = request.StockInCode.Trim(),
                StockInType = 1, // 采购入库
                // SourceCode 最大 32，避免直接写入 36 位 GUID 导致保存失败
                SourceCode = !string.IsNullOrWhiteSpace(purchaseOrderId)
                    ? purchaseOrderId!.Length > 32 ? purchaseOrderId[..32] : purchaseOrderId
                    : null,
                // 保留完整来源单ID，供入库完成回写使用
                SourceId = purchaseOrderId,
                WarehouseId = request.WarehouseId,
                VendorId = request.VendorId,
                StockInDate = PostgreSqlDateTime.ToUtc(request.StockInDate),
                TotalQuantity = request.TotalQuantity,
                Remark = request.Remark,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow
            };

            await _stockInRepository.AddAsync(stockIn);
            // 先落主单，避免后续插入明细时触发 FK(stockinitem.stockinid -> stockin.stockinid) 失败
            await _unitOfWork.SaveChangesAsync();

            if (request.Items != null && request.Items.Count > 0)
            {
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var price = item.UnitPrice ?? 0;
                    var amount = item.Quantity * price;
                    totalAmount += amount;
                    var line = new StockInItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockInId = stockInId,
                        MaterialId = item.MaterialCode?.Trim() ?? string.Empty,
                        Quantity = item.Quantity,
                        Price = price,
                        Amount = amount,
                        BatchNo = item.BatchNo?.Trim(),
                        LocationId = !string.IsNullOrWhiteSpace(item.WarehouseLocation) ? item.WarehouseLocation.Trim() : null,
                        CreateTime = DateTime.UtcNow
                    };
                    await _stockInItemRepository.AddAsync(line);
                }
                stockIn.TotalAmount = totalAmount;
                await _stockInRepository.UpdateAsync(stockIn);
            }

            await _unitOfWork.SaveChangesAsync();
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
            await _unitOfWork.SaveChangesAsync();
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
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            // 幂等保护：状态未变化时直接返回，避免重复点击导致重复推进回写
            if (stockIn.Status == status)
                return;

            stockIn.Status = status;
            stockIn.ModifyTime = DateTime.UtcNow;

            await _stockInRepository.UpdateAsync(stockIn);
            await _unitOfWork.SaveChangesAsync();

            if (status == 2)
            {
                await _logisticsService.HandleStockInCompletedAsync(
                    stockIn.Id,
                    !string.IsNullOrWhiteSpace(stockIn.SourceId) ? stockIn.SourceId : stockIn.SourceCode);
            }
        }
    }
}

using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Services
{
    /// <summary>
    /// 出库服务实现
    /// </summary>
    public class StockOutService : IStockOutService
    {
        private readonly IRepository<StockOut> _stockOutRepository;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepository;

        public StockOutService(
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutRequest> stockOutRequestRepository)
        {
            _stockOutRepository = stockOutRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
        }

        public async Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestCode))
                throw new ArgumentException("申请单号不能为空", nameof(request.RequestCode));

            if (string.IsNullOrWhiteSpace(request.SalesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(request.SalesOrderId));

            // 检查申请单号是否已存在
            var allRequests = await _stockOutRequestRepository.GetAllAsync();
            if (allRequests.Any(r => r.RequestCode == request.RequestCode))
                throw new InvalidOperationException($"出库申请单号 {request.RequestCode} 已存在");

            var stockOutRequest = new StockOutRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestCode = request.RequestCode.Trim(),
                SalesOrderId = request.SalesOrderId,
                CustomerId = request.CustomerId,
                RequestUserId = request.RequestUserId,
                RequestDate = request.RequestDate,
                Status = 0, // 待处理
                CreateTime = DateTime.UtcNow
            };

            await _stockOutRequestRepository.AddAsync(stockOutRequest);
            return stockOutRequest;
        }

        public async Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockOutCode))
                throw new ArgumentException("出库单号不能为空", nameof(request.StockOutCode));

            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            // 检查出库单号是否已存在
            var allStockOuts = await _stockOutRepository.GetAllAsync();
            if (allStockOuts.Any(s => s.StockOutCode == request.StockOutCode))
                throw new InvalidOperationException($"出库单号 {request.StockOutCode} 已存在");

            var stockOut = new StockOut
            {
                Id = Guid.NewGuid().ToString(),
                StockOutCode = request.StockOutCode.Trim(),
                StockOutType = 1, // 销售出库
                SourceCode = request.StockOutRequestId, // 使用SourceCode存储关联申请ID
                WarehouseId = request.WarehouseId,
                StockOutDate = request.StockOutDate,
                Remark = request.Remark,
                Status = 2, // 已出库
                CreateTime = DateTime.UtcNow
            };

            await _stockOutRepository.AddAsync(stockOut);
            return stockOut;
        }

        public async Task<StockOut?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _stockOutRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<StockOut>> GetAllAsync()
        {
            return await _stockOutRepository.GetAllAsync();
        }

        public async Task UpdateStatusAsync(string id, short status)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockOut = await _stockOutRepository.GetByIdAsync(id);
            if (stockOut == null)
                throw new InvalidOperationException($"出库单 {id} 不存在");

            stockOut.Status = status;
            stockOut.ModifyTime = DateTime.UtcNow;

            await _stockOutRepository.UpdateAsync(stockOut);
        }
    }
}

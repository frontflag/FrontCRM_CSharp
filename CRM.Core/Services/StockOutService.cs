using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using System.Linq;

namespace CRM.Core.Services
{
    /// <summary>
    /// 出库服务实现
    /// </summary>
    public class StockOutService : IStockOutService
    {
        private readonly IRepository<StockOut> _stockOutRepository;
        private readonly IRepository<StockOutItem> _stockOutItemRepository;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepository;
        private readonly IRepository<StockInfo> _stockRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockOutService(
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutItem> stockOutItemRepository,
            IRepository<StockOutRequest> stockOutRequestRepository,
            IRepository<StockInfo> stockRepository,
            IUnitOfWork unitOfWork)
        {
            _stockOutRepository = stockOutRepository;
            _stockOutItemRepository = stockOutItemRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
            _stockRepository = stockRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestCode))
                throw new ArgumentException("申请单号不能为空", nameof(request.RequestCode));

            if (string.IsNullOrWhiteSpace(request.SalesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(request.SalesOrderId));

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
                Status = 0,
                CreateTime = DateTime.UtcNow
            };

            await _stockOutRequestRepository.AddAsync(stockOutRequest);
            await _unitOfWork.SaveChangesAsync();
            return stockOutRequest;
        }

        /// <summary>
        /// 执行出库（包含预占 / 拣货 / 确认三个阶段的 FIFO 逻辑）
        /// </summary>
        public async Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockOutCode))
                throw new ArgumentException("出库单号不能为空", nameof(request.StockOutCode));

            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            var allStockOuts = await _stockOutRepository.GetAllAsync();
            if (allStockOuts.Any(s => s.StockOutCode == request.StockOutCode))
                throw new InvalidOperationException($"出库单号 {request.StockOutCode} 已存在");

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("出库明细不能为空", nameof(request.Items));

            var stockOutId = Guid.NewGuid().ToString();
            decimal totalQty = 0m;
            decimal totalAmount = 0m;

            // 预加载所有库存，避免多次访问数据库
            var allStocks = (await _stockRepository.GetAllAsync()).ToList();
            var changedStocks = new HashSet<StockInfo>();

            foreach (var item in request.Items)
            {
                var materialId = item.MaterialCode?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(materialId))
                    throw new ArgumentException("物料编码不能为空", nameof(item.MaterialCode));

                var needQty = item.Quantity;
                if (needQty <= 0)
                    continue;

                // 1) 找到该物料在指定仓库下的所有可用库存（FIFO：按生产日期 / 创建时间排序）
                var candidateStocks = allStocks
                    .Where(s => s.WarehouseId == request.WarehouseId
                                && s.MaterialId == materialId
                                && s.QtyRepertoryAvailable > 0)
                    .OrderBy(s => s.ProductionDate ?? s.CreateTime)
                    .ThenBy(s => s.CreateTime)
                    .ToList();

                if (!candidateStocks.Any())
                    throw new InvalidOperationException($"物料 {materialId} 在仓库 {request.WarehouseId} 无可用库存");

                var remaining = needQty;

                foreach (var stock in candidateStocks)
                {
                    if (remaining <= 0)
                        break;

                    var available = stock.QtyRepertoryAvailable;
                    if (available <= 0)
                        continue;

                    var takeQty = Math.Min(remaining, available);
                    if (takeQty <= 0)
                        continue;

                    // ====== 阶段一：销售预占（QtySales / QtyRepertoryAvailable） ======
                    stock.QtySales += takeQty;
                    stock.QtyRepertoryAvailable -= takeQty;

                    // ====== 阶段二：拣货占用（QtyOccupy / QtySales） ======
                    stock.QtySales -= takeQty;
                    stock.QtyOccupy += takeQty;

                    // ====== 阶段三：出库确认（Qty / QtyStockOut / QtyRepertory / QtyOccupy） ======
                    stock.QtyStockOut += takeQty;
                    stock.QtyOccupy -= takeQty;
                    stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
                    stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;

                    changedStocks.Add(stock);

                    // 生成出库明细行，记录具体来自哪条库存
                    var line = new StockOutItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockOutId = stockOutId,
                        MaterialId = materialId,
                        Quantity = takeQty,
                        OrderQty = needQty,
                        PlanQty = takeQty,
                        PickQty = takeQty,
                        ActualQty = takeQty,
                        Price = 0m,
                        Amount = 0m,
                        StockId = stock.Id,
                        WarehouseId = stock.WarehouseId,
                        LocationId = stock.LocationId,
                        BatchNo = stock.BatchNo,
                        CreateTime = DateTime.UtcNow
                    };
                    await _stockOutItemRepository.AddAsync(line);

                    totalQty += takeQty;
                    remaining -= takeQty;
                }

                if (remaining > 0)
                    throw new InvalidOperationException($"物料 {materialId} 在仓库 {request.WarehouseId} 库存不足，缺少 {remaining}");
            }

            // 持久化库存变更
            foreach (var stock in changedStocks)
            {
                await _stockRepository.UpdateAsync(stock);
            }

            var stockOut = new StockOut
            {
                Id = stockOutId,
                StockOutCode = request.StockOutCode.Trim(),
                StockOutType = 1,
                SourceCode = request.StockOutRequestId,
                WarehouseId = request.WarehouseId,
                StockOutDate = request.StockOutDate,
                TotalQuantity = totalQty,
                TotalAmount = totalAmount,
                Remark = request.Remark,
                Status = 2,
                ConfirmedBy = request.OperatorId,
                ConfirmedTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow
            };

            await _stockOutRepository.AddAsync(stockOut);
            await _unitOfWork.SaveChangesAsync();
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
            await _unitOfWork.SaveChangesAsync();
        }
    }
}

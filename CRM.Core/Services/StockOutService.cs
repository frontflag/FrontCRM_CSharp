using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

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
        private readonly IRepository<PickingTask> _pickingTaskRepository;
        private readonly IRepository<StockInfo> _stockRepository;
        private readonly IRepository<SellOrder> _sellOrderRepository;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IInventoryCenterService _inventoryCenterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;

        public StockOutService(
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutItem> stockOutItemRepository,
            IRepository<StockOutRequest> stockOutRequestRepository,
            IRepository<PickingTask> pickingTaskRepository,
            IRepository<StockInfo> stockRepository,
            IRepository<SellOrder> sellOrderRepository,
            IRepository<SellOrderItem> sellOrderItemRepository,
            IRepository<User> userRepository,
            IInventoryCenterService inventoryCenterService,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _stockOutRepository = stockOutRepository;
            _stockOutItemRepository = stockOutItemRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
            _pickingTaskRepository = pickingTaskRepository;
            _stockRepository = stockRepository;
            _sellOrderRepository = sellOrderRepository;
            _sellOrderItemRepository = sellOrderItemRepository;
            _userRepository = userRepository;
            _inventoryCenterService = inventoryCenterService;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SalesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(request.SalesOrderId));
            if (string.IsNullOrWhiteSpace(request.SalesOrderItemId))
                throw new ArgumentException("销售订单明细不能为空", nameof(request.SalesOrderItemId));

            var so = await _sellOrderRepository.GetByIdAsync(request.SalesOrderId);
            if (so == null)
                throw new InvalidOperationException("销售订单不存在");
            if (so.Status < SellOrderMainStatus.Approved)
                throw new InvalidOperationException("销售订单未审核，不能申请出库");
            if (so.Status == SellOrderMainStatus.Completed)
                throw new InvalidOperationException("销售订单已完成，不能申请出库");

            var soItem = await _sellOrderItemRepository.GetByIdAsync(request.SalesOrderItemId.Trim());
            if (soItem == null)
                throw new InvalidOperationException("销售订单明细不存在");
            if (!string.Equals(soItem.SellOrderId, request.SalesOrderId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("销售订单明细不属于该订单");
            if (soItem.Status != 0)
                throw new InvalidOperationException("该销售订单明细已取消，不能申请出库");

            if (request.Quantity <= 0)
                throw new ArgumentException("出库通知数量必须大于 0", nameof(request.Quantity));
            if (request.Quantity > soItem.Qty)
                throw new ArgumentException($"出库数量不能超过订单明细数量（{soItem.Qty.ToString(CultureInfo.InvariantCulture)}）", nameof(request.Quantity));

            var requestCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest);

            var stockOutRequest = new StockOutRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestCode = requestCode,
                SalesOrderId = request.SalesOrderId,
                SalesOrderItemId = request.SalesOrderItemId.Trim(),
                MaterialCode = string.IsNullOrWhiteSpace(request.MaterialCode) ? (soItem.PN?.Trim() ?? string.Empty) : request.MaterialCode.Trim(),
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? soItem.Brand?.Trim() : request.MaterialName.Trim(),
                Quantity = request.Quantity,
                CustomerId = request.CustomerId,
                RequestUserId = request.RequestUserId,
                RequestDate = PostgreSqlDateTime.ToUtc(request.RequestDate),
                Status = 0,
                Remark = request.Remark,
                CreateTime = DateTime.UtcNow,
            };

            await _stockOutRequestRepository.AddAsync(stockOutRequest);
            await _unitOfWork.SaveChangesAsync();
            return stockOutRequest;
        }

        public async Task<IEnumerable<StockOutRequestListItemDto>> GetStockOutRequestListAsync()
        {
            var reqs = (await _stockOutRequestRepository.GetAllAsync()).ToList();
            var soMap = (await _sellOrderRepository.GetAllAsync())
                .ToDictionary(x => x.Id, x => x);
            var users = (await _userRepository.GetAllAsync()).ToList();
            var userNameById = users
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return string.IsNullOrWhiteSpace(first.RealName) ? first.UserName : first.RealName!;
                    },
                    StringComparer.OrdinalIgnoreCase);
            var userNameByLogin = users
                .Where(x => !string.IsNullOrWhiteSpace(x.UserName))
                .GroupBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        return string.IsNullOrWhiteSpace(first.RealName) ? first.UserName : first.RealName!;
                    },
                    StringComparer.OrdinalIgnoreCase);

            return reqs
                .OrderByDescending(x => x.RequestDate)
                .ThenByDescending(x => x.CreateTime)
                .Select(x =>
                {
                    soMap.TryGetValue(x.SalesOrderId, out var so);
                    var materialModel = string.IsNullOrWhiteSpace(x.MaterialCode) ? null : x.MaterialCode.Trim();
                    var brand = string.IsNullOrWhiteSpace(x.MaterialName) ? null : x.MaterialName.Trim();

                    string? requestUserName = null;
                    if (!string.IsNullOrWhiteSpace(x.RequestUserId))
                    {
                        if (!userNameById.TryGetValue(x.RequestUserId, out requestUserName))
                        {
                            userNameByLogin.TryGetValue(x.RequestUserId, out requestUserName);
                        }
                    }
                    return new StockOutRequestListItemDto
                    {
                        Id = x.Id,
                        RequestCode = x.RequestCode,
                        SalesOrderId = x.SalesOrderId,
                        SalesOrderItemId = x.SalesOrderItemId,
                        SalesOrderCode = so?.SellOrderCode,
                        MaterialModel = materialModel,
                        Brand = brand,
                        OutQuantity = x.Quantity,
                        ExpectedStockOutDate = x.RequestDate == default ? null : x.RequestDate,
                        SalesUserName = so?.SalesUserName,
                        CustomerId = x.CustomerId,
                        CustomerName = so?.CustomerName,
                        RequestUserId = x.RequestUserId,
                        RequestUserName = requestUserName,
                        RequestDate = x.RequestDate,
                        Status = x.Status,
                        Remark = x.Remark,
                        CreateTime = x.CreateTime
                    };
                })
                .ToList();
        }

        /// <summary>
        /// 执行出库（包含预占 / 拣货 / 确认三个阶段的 FIFO 逻辑）
        /// </summary>
        public async Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            if (string.IsNullOrWhiteSpace(request.StockOutRequestId))
                throw new InvalidOperationException("执行出库前必须关联出库申请并完成拣货任务");

            var requestId = request.StockOutRequestId.Trim();
            var stockOutRequest = await _stockOutRequestRepository.GetByIdAsync(requestId)
                ?? throw new InvalidOperationException("出库申请不存在");
            if (stockOutRequest.Status == 1)
                throw new InvalidOperationException("该出库通知已执行出库，请勿重复操作");
            if (stockOutRequest.Status == 2)
                throw new InvalidOperationException("该出库通知已取消，不能执行出库");

            var pickingTasks = (await _pickingTaskRepository.GetAllAsync())
                .Where(x => string.Equals(x.StockOutRequestId, requestId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!pickingTasks.Any())
                throw new InvalidOperationException("执行出库前请先生成拣货任务");
            if (!pickingTasks.Any(x => x.Status == 100))
                throw new InvalidOperationException("执行出库前请先完成拣货任务");

            var soItem = await _sellOrderItemRepository.GetByIdAsync(stockOutRequest.SalesOrderItemId.Trim());
            var productIdFromLine = string.IsNullOrWhiteSpace(soItem?.ProductId) ? null : soItem!.ProductId!.Trim();

            var stockOutCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOut);

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
                // 库存 MaterialId 常与 ProductId 一致；出库明细传 PN，需与订单行 ProductId 一并匹配
                var candidateStocks = allStocks
                    .Where(s => s.WarehouseId == request.WarehouseId
                                && StockMaterialMatch.Matches(s, materialId, productIdFromLine)
                                && s.QtyRepertoryAvailable > 0)
                    .OrderBy(s => s.ProductionDate ?? s.CreateTime)
                    .ThenBy(s => s.CreateTime)
                    .ToList();

                if (!candidateStocks.Any())
                    throw new InvalidOperationException(
                        $"物料 {materialId} 在仓库 {request.WarehouseId} 无可用库存（已按 PN 与订单 ProductId 尝试匹配）");

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
                        MaterialId = stock.MaterialId,
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

            // SourceCode 字段最大 32 字符，不能写入 36 位 GUID；完整出库通知 ID 放在 SourceId
            var requestCode = stockOutRequest.RequestCode?.Trim() ?? string.Empty;
            if (requestCode.Length > 32)
                requestCode = requestCode.Substring(0, 32);

            var stockOut = new StockOut
            {
                Id = stockOutId,
                StockOutCode = stockOutCode,
                StockOutType = 1,
                SourceCode = string.IsNullOrEmpty(requestCode) ? null : requestCode,
                SourceId = requestId,
                CustomerId = string.IsNullOrWhiteSpace(stockOutRequest.CustomerId) ? null : stockOutRequest.CustomerId.Trim(),
                WarehouseId = request.WarehouseId,
                StockOutDate = PostgreSqlDateTime.ToUtc(request.StockOutDate),
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
            await _inventoryCenterService.RecordStockOutAsync(stockOut.Id);

            stockOutRequest.Status = 1;
            stockOutRequest.ModifyTime = DateTime.UtcNow;
            await _stockOutRequestRepository.UpdateAsync(stockOutRequest);
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

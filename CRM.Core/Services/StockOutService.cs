using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services
{
    /// <summary>
    /// 出库服务实现
    /// </summary>
    public class StockOutService : IStockOutService
    {
        private readonly IRepository<StockOut> _stockOutRepository;
        private readonly IRepository<StockOutItem> _stockOutItemRepository;
        private readonly IRepository<StockOutItemExtend> _stockOutItemExtendRepository;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepository;
        private readonly IRepository<PickingTask> _pickingTaskRepository;
        private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
        private readonly IRepository<StockInfo> _stockRepository;
        private readonly IRepository<StockItem> _stockItemRepository;
        private readonly IRepository<SellOrder> _sellOrderRepository;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
        private readonly IRepository<SellOrderItemExtend> _sellOrderItemExtendRepository;
        private readonly IRepository<CustomerInfo> _customerRepository;
        private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<WarehouseInfo> _warehouseRepository;
        private readonly IInventoryCenterService _inventoryCenterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
        private readonly ILogger<StockOutService> _logger;

        public StockOutService(
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutItem> stockOutItemRepository,
            IRepository<StockOutItemExtend> stockOutItemExtendRepository,
            IRepository<StockOutRequest> stockOutRequestRepository,
            IRepository<PickingTask> pickingTaskRepository,
            IRepository<PickingTaskItem> pickingTaskItemRepository,
            IRepository<StockInfo> stockRepository,
            IRepository<StockItem> stockItemRepository,
            IRepository<SellOrder> sellOrderRepository,
            IRepository<SellOrderItem> sellOrderItemRepository,
            IRepository<SellOrderItemExtend> sellOrderItemExtendRepository,
            IRepository<CustomerInfo> customerRepository,
            IRepository<PurchaseOrderItem> purchaseOrderItemRepository,
            IRepository<PurchaseOrder> purchaseOrderRepository,
            IRepository<User> userRepository,
            IRepository<WarehouseInfo> warehouseRepository,
            IInventoryCenterService inventoryCenterService,
            ISerialNumberService serialNumberService,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
            IUnitOfWork unitOfWork,
            ILogger<StockOutService> logger)
        {
            _stockOutRepository = stockOutRepository;
            _stockOutItemRepository = stockOutItemRepository;
            _stockOutItemExtendRepository = stockOutItemExtendRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
            _pickingTaskRepository = pickingTaskRepository;
            _pickingTaskItemRepository = pickingTaskItemRepository;
            _stockRepository = stockRepository;
            _stockItemRepository = stockItemRepository;
            _sellOrderRepository = sellOrderRepository;
            _sellOrderItemRepository = sellOrderItemRepository;
            _sellOrderItemExtendRepository = sellOrderItemExtendRepository;
            _customerRepository = customerRepository;
            _purchaseOrderItemRepository = purchaseOrderItemRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _userRepository = userRepository;
            _warehouseRepository = warehouseRepository;
            _inventoryCenterService = inventoryCenterService;
            _serialNumberService = serialNumberService;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _purchasedStockAvailableSync = purchasedStockAvailableSync;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<StockOutRequest> CreateStockOutRequestAsync(CreateStockOutRequestRequest request, string? actingUserId = null)
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

            var lineId = request.SalesOrderItemId.Trim();
            await EnsureSellLineMeetsStockOutPurchaseGateAsync(lineId);

            var qtyInt = InventoryQuantity.RoundFromDecimal(request.Quantity);
            if (qtyInt <= 0)
                throw new ArgumentException("出库通知数量必须大于 0", nameof(request.Quantity));
            // 勿用 string.Equals(..., OrdinalIgnoreCase)：EF Core 无法翻译为 SQL（Npgsql）
            var existingReqs = (await _stockOutRequestRepository.FindAsync(r => r.SalesOrderItemId == lineId))
                .ToList();
            var alreadyNotified = existingReqs.Where(r => r.Status != 2).Sum(r => r.Quantity);
            var remainingByLine = soItem.Qty - alreadyNotified;
            if (remainingByLine <= 0m)
                throw new InvalidOperationException("该销售明细可出库通知数量已用尽，无法继续申请");
            if (request.Quantity > remainingByLine)
                throw new ArgumentException(
                    $"出库通知数量不能超过剩余可申请数量（{remainingByLine.ToString(CultureInfo.InvariantCulture)}，已占用 {alreadyNotified.ToString(CultureInfo.InvariantCulture)}）",
                    nameof(request.Quantity));

            var stockDto = await _inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(lineId);
            var lineAvail = stockDto.AvailableQty;
            if (lineAvail < 0)
                lineAvail = 0;
            var extForCap = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            var stockingAvail = extForCap?.PurchasedStock_AvailableQty ?? 0;
            var maxShippable = lineAvail + stockingAvail;
            if (qtyInt > maxShippable)
                throw new InvalidOperationException(
                    $"在库可用数量不足（客单在库 {lineAvail.ToString(CultureInfo.InvariantCulture)} + 备货在库 {stockingAvail.ToString(CultureInfo.InvariantCulture)} = {maxShippable.ToString(CultureInfo.InvariantCulture)}，本次申请 {qtyInt.ToString(CultureInfo.InvariantCulture)}）");

            var requestCode = string.IsNullOrWhiteSpace(request.RequestCode)
                ? await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOutRequest)
                : request.RequestCode.Trim();

            var stockOutRequest = new StockOutRequest
            {
                Id = Guid.NewGuid().ToString(),
                RequestCode = requestCode,
                SalesOrderId = request.SalesOrderId,
                SalesOrderItemId = request.SalesOrderItemId.Trim(),
                MaterialCode = string.IsNullOrWhiteSpace(request.MaterialCode) ? (soItem.PN?.Trim() ?? string.Empty) : request.MaterialCode.Trim(),
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? soItem.Brand?.Trim() : request.MaterialName.Trim(),
                Quantity = qtyInt,
                CustomerId = request.CustomerId,
                RequestUserId = request.RequestUserId,
                RequestDate = PostgreSqlDateTime.ToUtc(request.RequestDate),
                Status = 0,
                Remark = request.Remark,
                ShipmentMethod = string.IsNullOrWhiteSpace(request.ShipmentMethod)
                    ? null
                    : request.ShipmentMethod.Trim(),
                RegionType = RegionTypeCode.Normalize(request.RegionType),
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId)
            };

            await _stockOutRequestRepository.AddAsync(stockOutRequest);
            var saveAfterReq = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] CreateStockOutRequest saved StockOutRequestId={RequestId} SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                stockOutRequest.Id, lineId, saveAfterReq);
            await _sellOrderItemExtendSync.RecalculateAsync(lineId);
            var saveAfterExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] CreateStockOutRequest after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                lineId, saveAfterExtend);
            return stockOutRequest;
        }

        /// <inheritdoc />
        public async Task<StockOutApplyContextDto> GetApplyContextAsync(string salesOrderId, string salesOrderItemId)
        {
            if (string.IsNullOrWhiteSpace(salesOrderId))
                throw new ArgumentException("销售订单ID不能为空", nameof(salesOrderId));
            if (string.IsNullOrWhiteSpace(salesOrderItemId))
                throw new ArgumentException("销售订单明细不能为空", nameof(salesOrderItemId));

            var so = await _sellOrderRepository.GetByIdAsync(salesOrderId.Trim());
            if (so == null)
                throw new InvalidOperationException("销售订单不存在");
            if (so.Status < SellOrderMainStatus.Approved)
                throw new InvalidOperationException("销售订单未审核，不能申请出库");
            if (so.Status == SellOrderMainStatus.Completed)
                throw new InvalidOperationException("销售订单已完成，不能申请出库");

            var soItem = await _sellOrderItemRepository.GetByIdAsync(salesOrderItemId.Trim());
            if (soItem == null)
                throw new InvalidOperationException("销售订单明细不存在");
            if (!string.Equals(soItem.SellOrderId, salesOrderId.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("销售订单明细不属于该订单");
            if (soItem.Status != 0)
                throw new InvalidOperationException("该销售订单明细已取消，不能申请出库");

            var lineId = salesOrderItemId.Trim();
            await EnsureSellLineMeetsStockOutPurchaseGateAsync(lineId);
            var existingReqs = (await _stockOutRequestRepository.FindAsync(r => r.SalesOrderItemId == lineId))
                .ToList();
            var alreadyNotified = existingReqs.Where(r => r.Status != 2).Sum(r => r.Quantity);
            var remainingNotify = soItem.Qty - alreadyNotified;
            if (remainingNotify < 0m)
                remainingNotify = 0m;

            var stockDto = await _inventoryCenterService.GetAvailableQtyForSellOrderItemAsync(lineId);
            var lineAvail = stockDto.AvailableQty;
            if (lineAvail < 0)
                lineAvail = 0;
            var ext = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            var purchasedStock = ext?.PurchasedStock_AvailableQty ?? 0;
            var combined = (decimal)lineAvail + purchasedStock;
            var suggested = remainingNotify <= combined ? remainingNotify : combined;
            if (suggested < 0m)
                suggested = 0m;

            return new StockOutApplyContextDto
            {
                salesOrderItemId = lineId,
                salesOrderQty = soItem.Qty,
                alreadyNotifiedQty = alreadyNotified,
                remainingNotifyQty = remainingNotify,
                availableStockQty = lineAvail,
                purchasedStockAvailableQty = purchasedStock,
                suggestedMaxQty = suggested
            };
        }

        /// <summary>
        /// 销售明细须已有关联采购行，且每条关联采购单主表状态 ≥ 供应商确认（30）；
        /// 与列表「申请出库」可点逻辑一致：<see cref="SellOrderItemExtend.PurchasedStock_AvailableQty"/> &gt; 0 时跳过本门槛（备货可用放宽）。
        /// </summary>
        private async Task EnsureSellLineMeetsStockOutPurchaseGateAsync(string sellOrderItemLineId)
        {
            var lineId = sellOrderItemLineId.Trim();

            var ext = await _sellOrderItemExtendRepository.GetByIdAsync(lineId);
            if (ext != null && ext.PurchasedStock_AvailableQty > 0)
                return;

            var min = PurchaseOrderMainStatusCodes.VendorConfirmedOrBeyond;
            var poItems = (await _purchaseOrderItemRepository.FindAsync(i => i.SellOrderItemId == lineId))
                .ToList();
            if (poItems.Count == 0)
                throw new InvalidOperationException("该销售明细尚未生成采购订单明细，不能申请出库");

            var poIds = poItems
                .Select(i => i.PurchaseOrderId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var pid in poIds)
            {
                var po = await _purchaseOrderRepository.GetByIdAsync(pid);
                if (po == null || po.Status < min)
                    throw new InvalidOperationException("关联采购订单尚未供应商确认，不能申请出库");
            }
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
                        ShipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim(),
                        RegionType = x.RegionType,
                        CreateTime = x.CreateTime
                    };
                })
                .ToList();
        }

        /// <summary>汇总层与可选在库明细层同步扣减可用量、占用与已出库量。</summary>
        private static void ApplyOutboundTakeToStockAndOptionalLayer(
            StockInfo stock,
            StockItem? layer,
            int takeQty,
            HashSet<StockInfo> changedStocks,
            HashSet<StockItem> changedLayers)
        {
            stock.QtySales += takeQty;
            stock.QtyRepertoryAvailable -= takeQty;
            stock.QtySales -= takeQty;
            stock.QtyOccupy += takeQty;
            stock.QtyStockOut += takeQty;
            stock.QtyOccupy -= takeQty;
            stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
            stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;
            changedStocks.Add(stock);

            if (layer == null)
                return;

            layer.QtySales += takeQty;
            layer.QtyRepertoryAvailable -= takeQty;
            layer.QtySales -= takeQty;
            layer.QtyOccupy += takeQty;
            layer.QtyStockOut += takeQty;
            layer.QtyOccupy -= takeQty;
            layer.QtyRepertory = layer.QtyInbound - layer.QtyStockOut;
            layer.QtyRepertoryAvailable = layer.QtyRepertory - layer.QtyOccupy - layer.QtySales;
            changedLayers.Add(layer);
            layer.ModifyTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 执行出库：须有关联的已完成拣货任务；按 <c>pickingtaskitem</c> 与 <c>request.Items</c> 数量校验后，逐行扣减对应 <c>stockitem</c>。
        /// </summary>
        public async Task<StockOut> ExecuteStockOutAsync(ExecuteStockOutRequest request, string? actingUserId = null)
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

            var pickingTasks = (await _pickingTaskRepository.GetAllAsync()).ToList()
                .Where(x => string.Equals(x.StockOutRequestId, requestId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (!pickingTasks.Any())
                throw new InvalidOperationException("执行出库前请先生成拣货任务");
            if (!pickingTasks.Any(x => x.Status == 100))
                throw new InvalidOperationException("执行出库前请先完成拣货任务");

            var sellLineId = stockOutRequest.SalesOrderItemId.Trim();

            var stockOutCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockOut);

            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut begin StockOutRequestId={RequestId} SellOrderItemId={SellOrderItemId} WarehouseId={WarehouseId} PlannedStockOutCode={StockOutCode}",
                requestId, sellLineId, request.WarehouseId, stockOutCode);

            if (request.Items == null || request.Items.Count == 0)
                throw new ArgumentException("出库明细不能为空", nameof(request.Items));

            var requestQtySum = request.Items.Sum(x => InventoryQuantity.RoundFromDecimal(x.Quantity));
            if (requestQtySum != stockOutRequest.Quantity)
                throw new InvalidOperationException(
                    $"出库明细数量合计（{requestQtySum}）须与出库通知数量（{stockOutRequest.Quantity}）一致。");

            var completedTask = pickingTasks
                .Where(x => x.Status == 100)
                .OrderByDescending(x => x.ModifyTime ?? DateTime.MinValue)
                .ThenByDescending(x => x.CreateTime)
                .FirstOrDefault();
            if (completedTask == null)
                throw new InvalidOperationException("执行出库前请先完成拣货任务");

            var pickItems = (await _pickingTaskItemRepository.GetAllAsync())
                .Where(x => string.Equals(x.PickingTaskId, completedTask.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.CreateTime)
                .ToList();
            if (pickItems.Count == 0)
                throw new InvalidOperationException("拣货任务无明细，无法执行出库");
            if (pickItems.Any(x => string.IsNullOrWhiteSpace(x.StockItemId)))
                throw new InvalidOperationException(
                    "拣货明细缺少在库明细绑定（stock_item_id），请按新流程保存拣货并完成拣货后再执行出库");
            var pickSum = pickItems.Sum(x => x.PlanQty);
            if (pickSum != stockOutRequest.Quantity)
                throw new InvalidOperationException($"拣货计划数量合计（{pickSum}）与出库通知数量不一致。");
            if (pickSum != requestQtySum)
                throw new InvalidOperationException("拣货计划数量合计与出库明细数量合计不一致，请核对后重试。");

            var stockOutId = Guid.NewGuid().ToString();
            var totalQty = 0;
            decimal totalAmount = 0m;
            short stockOutHeaderRegionType = RegionTypeCode.Domestic;
            var stockOutHeaderRegionCaptured = false;

            // 预加载所有库存，避免多次访问数据库
            var allStocks = (await _stockRepository.GetAllAsync()).ToList();
            var allStockItems = (await _stockItemRepository.GetAllAsync()).ToList();
            var changedStocks = new HashSet<StockInfo>();
            var changedLayers = new HashSet<StockItem>();
            var stocksById = allStocks.ToDictionary(s => s.Id, s => s, StringComparer.OrdinalIgnoreCase);
            var stockItemsById = allStockItems
                .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var wh = request.WarehouseId.Trim();

            foreach (var pickItem in pickItems)
            {
                var takeQty = pickItem.PlanQty;
                if (takeQty <= 0)
                    continue;

                var layerId = pickItem.StockItemId!.Trim();
                if (!stockItemsById.TryGetValue(layerId, out var layer))
                    throw new InvalidOperationException($"拣货引用的在库明细不存在：{layerId}");

                var stockIdPick = pickItem.StockId?.Trim() ?? "";
                if (string.IsNullOrEmpty(stockIdPick) || !stocksById.TryGetValue(stockIdPick, out var stock))
                    throw new InvalidOperationException($"拣货引用的汇总库存不存在：{stockIdPick}");

                if (!string.Equals(layer.StockAggregateId?.Trim(), stock.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("拣货行 stock_id 与在库明细汇总桶不一致");

                if (!string.Equals(layer.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("拣货明细仓库与执行出库所选仓库不一致");

                if (layer.QtyRepertoryAvailable < takeQty)
                    throw new InvalidOperationException(
                        $"在库明细 {layerId} 可用量不足（当前 {layer.QtyRepertoryAvailable}，需出 {takeQty}），可能库存已变更，请重新拣货。");

                if (!stockOutHeaderRegionCaptured)
                {
                    stockOutHeaderRegionType = RegionTypeCode.Normalize(stock.RegionType);
                    stockOutHeaderRegionCaptured = true;
                }

                ApplyOutboundTakeToStockAndOptionalLayer(stock, layer, takeQty, changedStocks, changedLayers);

                var outLine = new StockOutItem
                {
                    Id = Guid.NewGuid().ToString(),
                    StockOutId = stockOutId,
                    MaterialId = layer.MaterialId,
                    PurchasePn = string.IsNullOrWhiteSpace(layer.PurchasePn) ? null : layer.PurchasePn.Trim(),
                    PurchaseBrand = string.IsNullOrWhiteSpace(layer.PurchaseBrand) ? null : layer.PurchaseBrand.Trim(),
                    Quantity = takeQty,
                    OrderQty = stockOutRequest.Quantity,
                    PlanQty = takeQty,
                    PickQty = takeQty,
                    ActualQty = takeQty,
                    Price = 0m,
                    Amount = 0m,
                    StockId = stock.Id,
                    StockItemId = layer.Id,
                    PickingTaskItemId = pickItem.Id,
                    WarehouseId = stock.WarehouseId,
                    LocationId = layer.LocationId,
                    BatchNo = layer.BatchNo,
                    CreateTime = DateTime.UtcNow
                };
                await _stockOutItemRepository.AddAsync(outLine);
                await _stockOutItemExtendRepository.AddAsync(
                    BuildStockOutItemExtend(outLine, layer, stock, takeQty));

                totalQty += takeQty;
            }

            if (totalQty != stockOutRequest.Quantity)
                throw new InvalidOperationException("实际扣减数量与出库通知数量不一致，已中止。");

            // 持久化库存变更
            foreach (var stock in changedStocks)
            {
                await _stockRepository.UpdateAsync(stock);
            }

            foreach (var layer in changedLayers)
            {
                await _stockItemRepository.UpdateAsync(layer);
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
                RegionType = stockOutHeaderRegionType,
                SourceCode = string.IsNullOrEmpty(requestCode) ? null : requestCode,
                SourceId = requestId,
                PickingTaskId = completedTask.Id,
                SellOrderItemId = sellLineId,
                CustomerId = string.IsNullOrWhiteSpace(stockOutRequest.CustomerId) ? null : stockOutRequest.CustomerId.Trim(),
                WarehouseId = request.WarehouseId,
                StockOutDate = PostgreSqlDateTime.ToUtc(request.StockOutDate),
                TotalQuantity = totalQty,
                TotalAmount = totalAmount,
                Remark = request.Remark,
                ShipmentMethod = string.IsNullOrWhiteSpace(stockOutRequest.ShipmentMethod)
                    ? null
                    : stockOutRequest.ShipmentMethod.Trim(),
                Status = 2,
                ConfirmedBy = request.OperatorId,
                ConfirmedTime = DateTime.UtcNow,
                CreateTime = DateTime.UtcNow,
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId)
            };

            await _stockOutRepository.AddAsync(stockOut);
            var saveStockOut = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut persisted header StockOutId={StockOutId} StockOutCode={StockOutCode} SellOrderItemId={SellOrderItemId} Status={Status} TotalQuantity={TotalQty} SaveChanges={Rows}",
                stockOut.Id, stockOut.StockOutCode, stockOut.SellOrderItemId, stockOut.Status, stockOut.TotalQuantity, saveStockOut);

            await _inventoryCenterService.RecordStockOutAsync(stockOut.Id);
            _logger.LogInformation("[SellLineStockOutSync] ExecuteStockOut RecordStockOutAsync done StockOutId={StockOutId}", stockOut.Id);

            stockOutRequest.Status = 1;
            stockOutRequest.ModifyTime = DateTime.UtcNow;
            stockOutRequest.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId);
            await _stockOutRequestRepository.UpdateAsync(stockOutRequest);
            var saveRequest = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut request marked executed StockOutRequestId={RequestId} SaveChanges={Rows}",
                requestId, saveRequest);

            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut calling RecalculateAsync SellOrderItemId={SellOrderItemId}",
                sellLineId);
            await _sellOrderItemExtendSync.RecalculateAsync(sellLineId);
            try
            {
                await _purchasedStockAvailableSync.TryRecalculateFromChangedStockInfosAsync(changedStocks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[PurchasedStockAvail] TryRecalculateFromChangedStockInfos failed after ExecuteStockOut SellOrderItemId={SellOrderItemId}",
                    sellLineId);
            }

            var saveExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] ExecuteStockOut after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                sellLineId, saveExtend);

            return stockOut;
        }

        public async Task<StockOut?> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _stockOutRepository.GetByIdAsync(id);
        }

        /// <inheritdoc />
        public async Task<StockOutDetailViewDto?> GetDetailViewAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;
            var x = await _stockOutRepository.GetByIdAsync(id.Trim());
            if (x == null)
                return null;

            SellOrderItem? line = null;
            if (!string.IsNullOrWhiteSpace(x.SellOrderItemId))
                line = await _sellOrderItemRepository.GetByIdAsync(x.SellOrderItemId.Trim());

            SellOrder? so = null;
            if (line != null && !string.IsNullOrWhiteSpace(line.SellOrderId))
                so = await _sellOrderRepository.GetByIdAsync(line.SellOrderId.Trim());

            string? customerName = null;
            if (!string.IsNullOrWhiteSpace(x.CustomerId))
            {
                var cust = await _customerRepository.GetByIdAsync(x.CustomerId.Trim());
                if (cust != null)
                    customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
            }

            if (customerName == null && so != null)
                customerName = so.CustomerName;

            var salesUserName = so?.SalesUserName;
            var sellOrderItemCode = string.IsNullOrWhiteSpace(line?.SellOrderItemCode)
                ? null
                : line!.SellOrderItemCode;

            string? createUserName = null;
            if (!string.IsNullOrWhiteSpace(x.CreateByUserId))
            {
                var u = await _userRepository.GetByIdAsync(x.CreateByUserId.Trim());
                if (u != null)
                    createUserName = string.IsNullOrWhiteSpace(u.RealName) ? u.UserName : u.RealName;
            }

            string? warehouseCode = null;
            if (!string.IsNullOrWhiteSpace(x.WarehouseId))
            {
                var wh = await _warehouseRepository.GetByIdAsync(x.WarehouseId.Trim());
                if (wh != null && !string.IsNullOrWhiteSpace(wh.WarehouseCode))
                    warehouseCode = wh.WarehouseCode.Trim();
            }

            var listRow = new StockOutListItemDto
            {
                Id = x.Id,
                StockOutCode = x.StockOutCode,
                StockOutType = x.StockOutType,
                SourceCode = x.SourceCode,
                SourceId = x.SourceId,
                StockOutDate = x.StockOutDate,
                TotalQuantity = x.TotalQuantity,
                TotalAmount = x.TotalAmount,
                Status = x.Status,
                Remark = x.Remark,
                CreateTime = x.CreateTime,
                CreateByUserId = x.CreateByUserId,
                CreateUserName = createUserName,
                CustomerName = customerName,
                SalesUserName = salesUserName,
                SellOrderItemCode = sellOrderItemCode,
                ShipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim(),
                CourierTrackingNo = string.IsNullOrWhiteSpace(x.CourierTrackingNo) ? null : x.CourierTrackingNo.Trim()
            };

            return new StockOutDetailViewDto
            {
                Id = listRow.Id,
                StockOutCode = listRow.StockOutCode,
                StockOutType = listRow.StockOutType,
                SourceCode = listRow.SourceCode,
                SourceId = listRow.SourceId,
                StockOutDate = listRow.StockOutDate,
                TotalQuantity = listRow.TotalQuantity,
                TotalAmount = listRow.TotalAmount,
                Status = listRow.Status,
                Remark = listRow.Remark,
                CreateTime = listRow.CreateTime,
                CreateByUserId = listRow.CreateByUserId,
                CreateUserName = listRow.CreateUserName,
                CustomerName = listRow.CustomerName,
                SalesUserName = listRow.SalesUserName,
                SellOrderItemCode = listRow.SellOrderItemCode,
                ShipmentMethod = listRow.ShipmentMethod,
                CourierTrackingNo = listRow.CourierTrackingNo,
                WarehouseId = x.WarehouseId,
                WarehouseCode = warehouseCode,
                SellOrderItemId = string.IsNullOrWhiteSpace(x.SellOrderItemId) ? null : x.SellOrderItemId.Trim()
            };
        }

        public async Task<IEnumerable<StockOutListItemDto>> GetStockOutListAsync()
        {
            var outs = (await _stockOutRepository.GetAllAsync()).ToList();
            var lineIdSet = outs
                .Select(x => x.SellOrderItemId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var itemById = (await _sellOrderItemRepository.GetAllAsync())
                .Where(x => lineIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var orderIdSet = itemById.Values
                .Select(x => x.SellOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orderById = (await _sellOrderRepository.GetAllAsync())
                .Where(x => orderIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var custIdSet = outs
                .Select(x => x.CustomerId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var o in orderById.Values)
            {
                if (!string.IsNullOrWhiteSpace(o.CustomerId))
                    custIdSet.Add(o.CustomerId.Trim());
            }

            var customerById = (await _customerRepository.GetAllAsync())
                .Where(c => custIdSet.Contains(c.Id))
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

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

            return outs
                .OrderByDescending(x => x.CreateTime)
                .Select(x =>
                {
                    SellOrderItem? line = null;
                    if (!string.IsNullOrWhiteSpace(x.SellOrderItemId))
                        itemById.TryGetValue(x.SellOrderItemId.Trim(), out line);

                    SellOrder? so = null;
                    if (line != null && !string.IsNullOrWhiteSpace(line.SellOrderId))
                        orderById.TryGetValue(line.SellOrderId.Trim(), out so);

                    string? customerName = null;
                    if (!string.IsNullOrWhiteSpace(x.CustomerId)
                        && customerById.TryGetValue(x.CustomerId.Trim(), out var cust))
                    {
                        customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                    }
                    else if (so != null)
                    {
                        customerName = so.CustomerName;
                    }

                    var salesUserName = so?.SalesUserName;
                    var sellOrderItemCode = string.IsNullOrWhiteSpace(line?.SellOrderItemCode)
                        ? null
                        : line!.SellOrderItemCode;

                    string? createUserName = null;
                    if (!string.IsNullOrWhiteSpace(x.CreateByUserId))
                    {
                        if (!userNameById.TryGetValue(x.CreateByUserId, out createUserName))
                            userNameByLogin.TryGetValue(x.CreateByUserId, out createUserName);
                    }

                    return new StockOutListItemDto
                    {
                        Id = x.Id,
                        StockOutCode = x.StockOutCode,
                        StockOutType = x.StockOutType,
                        SourceCode = x.SourceCode,
                        SourceId = x.SourceId,
                        StockOutDate = x.StockOutDate,
                        TotalQuantity = x.TotalQuantity,
                        TotalAmount = x.TotalAmount,
                        Status = x.Status,
                        Remark = x.Remark,
                        CreateTime = x.CreateTime,
                        CreateByUserId = x.CreateByUserId,
                        CreateUserName = createUserName,
                        CustomerName = customerName,
                        SalesUserName = salesUserName,
                        SellOrderItemCode = sellOrderItemCode,
                        ShipmentMethod = string.IsNullOrWhiteSpace(x.ShipmentMethod) ? null : x.ShipmentMethod.Trim(),
                        CourierTrackingNo = string.IsNullOrWhiteSpace(x.CourierTrackingNo) ? null : x.CourierTrackingNo.Trim()
                    };
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<StockOutItemListRowDto>> GetStockOutItemListAsync(StockOutItemListQuery? query)
        {
            query ??= new StockOutItemListQuery();
            var items = (await _stockOutItemRepository.GetAllAsync()).ToList();
            var outById = (await _stockOutRepository.GetAllAsync())
                .ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            var lineIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var custIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in items)
            {
                if (!outById.TryGetValue(x.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    lineIdSet.Add(hdr.SellOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId))
                    custIdSet.Add(hdr.CustomerId.Trim());
            }

            var itemById = (await _sellOrderItemRepository.GetAllAsync())
                .Where(x => lineIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var orderIdSet = itemById.Values
                .Select(x => x.SellOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orderById = (await _sellOrderRepository.GetAllAsync())
                .Where(x => orderIdSet.Contains(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var o in orderById.Values)
            {
                if (!string.IsNullOrWhiteSpace(o.CustomerId))
                    custIdSet.Add(o.CustomerId.Trim());
            }

            var customerById = (await _customerRepository.GetAllAsync())
                .Where(c => custIdSet.Contains(c.Id))
                .GroupBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var codeNeedle = query.StockOutCode?.Trim();
            var custNeedle = query.CustomerName?.Trim();
            var salesNeedle = query.SalesUserName?.Trim();
            var pnNeedle = query.PurchasePn?.Trim();
            var soLineCodeNeedle = query.SellOrderItemCode?.Trim();
            var statusFilter = query.Status;

            var result = new List<StockOutItemListRowDto>();
            foreach (var line in items)
            {
                if (!outById.TryGetValue(line.StockOutId?.Trim() ?? string.Empty, out var hdr))
                    continue;

                if (statusFilter.HasValue && hdr.Status != statusFilter.Value)
                    continue;
                if (!TextContainsOptional(hdr.StockOutCode, codeNeedle))
                    continue;
                if (!StockOutDateInRange(hdr.StockOutDate, query.StockOutDateFrom, query.StockOutDateTo))
                    continue;

                SellOrderItem? soLine = null;
                if (!string.IsNullOrWhiteSpace(hdr.SellOrderItemId))
                    itemById.TryGetValue(hdr.SellOrderItemId.Trim(), out soLine);

                SellOrder? so = null;
                if (soLine != null && !string.IsNullOrWhiteSpace(soLine.SellOrderId))
                    orderById.TryGetValue(soLine.SellOrderId.Trim(), out so);

                string? customerName = null;
                if (!string.IsNullOrWhiteSpace(hdr.CustomerId)
                    && customerById.TryGetValue(hdr.CustomerId.Trim(), out var cust))
                {
                    customerName = string.IsNullOrWhiteSpace(cust.OfficialName) ? cust.CustomerName : cust.OfficialName;
                }
                else if (so != null)
                    customerName = so.CustomerName;

                if (!TextContainsOptional(customerName, custNeedle))
                    continue;

                var salesUserName = so?.SalesUserName;
                if (!TextContainsOptional(salesUserName, salesNeedle))
                    continue;

                var sellOrderItemCode = string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode)
                    ? null
                    : soLine!.SellOrderItemCode.Trim();
                if (!TextContainsOptional(sellOrderItemCode, soLineCodeNeedle))
                    continue;

                var pn = string.IsNullOrWhiteSpace(line.PurchasePn) ? null : line.PurchasePn.Trim();
                if (!TextContainsOptional(pn, pnNeedle))
                    continue;

                var outQty = line.ActualQty > 0 ? line.ActualQty : line.Quantity;

                result.Add(new StockOutItemListRowDto
                {
                    StockOutItemId = line.Id,
                    StockOutId = hdr.Id,
                    Status = hdr.Status,
                    StockOutCode = hdr.StockOutCode,
                    StockOutDate = hdr.StockOutDate,
                    CustomerName = customerName,
                    SalesUserName = salesUserName,
                    PurchasePn = pn,
                    PurchaseBrand = string.IsNullOrWhiteSpace(line.PurchaseBrand) ? null : line.PurchaseBrand.Trim(),
                    OutQuantity = outQty,
                    ShipmentMethod = string.IsNullOrWhiteSpace(hdr.ShipmentMethod) ? null : hdr.ShipmentMethod.Trim(),
                    CourierTrackingNo = string.IsNullOrWhiteSpace(hdr.CourierTrackingNo) ? null : hdr.CourierTrackingNo.Trim(),
                    SellOrderItemCode = sellOrderItemCode
                });
            }

            return result
                .OrderByDescending(x => x.StockOutDate)
                .ThenBy(x => x.StockOutCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.StockOutItemId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool TextContainsOptional(string? haystack, string? needleTrimmedOrNull)
        {
            if (string.IsNullOrEmpty(needleTrimmedOrNull))
                return true;
            if (string.IsNullOrEmpty(haystack))
                return false;
            return haystack.Contains(needleTrimmedOrNull, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime DateStartUtc(DateTime d)
        {
            var utc = d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
            return utc.Date;
        }

        private static bool StockOutDateInRange(DateTime stockOutDate, DateTime? from, DateTime? to)
        {
            if (!from.HasValue && !to.HasValue)
                return true;
            var t = DateStartUtc(stockOutDate);
            if (from.HasValue && t < DateStartUtc(from.Value))
                return false;
            if (to.HasValue && t >= DateStartUtc(to.Value).AddDays(1))
                return false;
            return true;
        }

        /// <inheritdoc />
        public async Task UpdateHeaderAsync(string id, UpdateStockOutHeaderRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stockOut = await _stockOutRepository.GetByIdAsync(id.Trim());
            if (stockOut == null)
                throw new InvalidOperationException($"出库单 {id} 不存在");

            stockOut.StockOutDate = PostgreSqlDateTime.ToUtc(request.StockOutDate);
            stockOut.ShipmentMethod = string.IsNullOrWhiteSpace(request.ShipmentMethod)
                ? null
                : request.ShipmentMethod.Trim();
            stockOut.CourierTrackingNo = string.IsNullOrWhiteSpace(request.CourierTrackingNo)
                ? null
                : request.CourierTrackingNo.Trim();
            stockOut.ModifyTime = DateTime.UtcNow;
            stockOut.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            await _stockOutRepository.UpdateAsync(stockOut);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockOut = await _stockOutRepository.GetByIdAsync(id);
            if (stockOut == null)
                throw new InvalidOperationException($"出库单 {id} 不存在");

            var previousStatus = stockOut.Status;
            stockOut.Status = status;
            stockOut.ModifyTime = DateTime.UtcNow;
            stockOut.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus begin StockOutId={StockOutId} StockOutCode={StockOutCode} Type={StockOutType} PrevStatus={Prev} NewStatus={New} SellOrderItemId={SellOrderItemId} SourceId={SourceId}",
                stockOut.Id,
                stockOut.StockOutCode,
                stockOut.StockOutType,
                previousStatus,
                status,
                stockOut.SellOrderItemId ?? "(null)",
                stockOut.SourceId ?? "(null)");

            await _stockOutRepository.UpdateAsync(stockOut);
            var saveHeader = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus header saved StockOutId={StockOutId} SaveChanges={Rows}",
                stockOut.Id, saveHeader);

            // 销售出库：进入或离开「已出库/已完成」时须刷新销售明细扩展（汇总仅含 2、4）；扩展变更需 SaveChanges 才落库（与入库链一致）
            const short stockOutCompleted = 2;
            const short stockOutFinished = 4;
            const short salesStockOutType = 1;
            static bool IsOutboundDone(short s) => s == stockOutCompleted || s == stockOutFinished;

            if (stockOut.StockOutType != salesStockOutType)
            {
                _logger.LogInformation(
                    "[SellLineStockOutSync] UpdateStatus skip extend chain (not sales stock-out) StockOutId={StockOutId} StockOutType={StockOutType}",
                    stockOut.Id,
                    stockOut.StockOutType);
                return;
            }

            StockOutRequest? sorForLine = null;
            if (!string.IsNullOrWhiteSpace(stockOut.SourceId))
                sorForLine = await _stockOutRequestRepository.GetByIdAsync(stockOut.SourceId.Trim());

            if (IsOutboundDone(status) && !IsOutboundDone(previousStatus))
            {
                if (sorForLine != null && sorForLine.Status == 0)
                {
                    sorForLine.Status = 1;
                    sorForLine.ModifyTime = DateTime.UtcNow;
                    sorForLine.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);
                    await _stockOutRequestRepository.UpdateAsync(sorForLine);
                    var saveSor = await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation(
                        "[SellLineStockOutSync] UpdateStatus stockoutrequest marked fulfilled StockOutRequestId={RequestId} SaveChanges={Rows}",
                        sorForLine.Id,
                        saveSor);
                }
            }

            var soLineId = !string.IsNullOrWhiteSpace(stockOut.SellOrderItemId)
                ? stockOut.SellOrderItemId.Trim()
                : sorForLine?.SalesOrderItemId?.Trim();
            if (string.IsNullOrWhiteSpace(soLineId))
            {
                _logger.LogWarning(
                    "[SellLineStockOutSync] UpdateStatus cannot resolve SellOrderItemId (header null and no request line) StockOutId={StockOutId} SourceId={SourceId}",
                    stockOut.Id,
                    stockOut.SourceId ?? "(null)");
                return;
            }

            var extendRefresh = IsOutboundDone(status) || IsOutboundDone(previousStatus);
            if (!extendRefresh)
            {
                _logger.LogInformation(
                    "[SellLineStockOutSync] UpdateStatus skip Recalculate (neither prev nor new status is outbound-done 2|4) StockOutId={StockOutId} Prev={Prev} New={New}",
                    stockOut.Id,
                    previousStatus,
                    status);
                return;
            }

            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus calling RecalculateAsync SellOrderItemId={SellOrderItemId}",
                soLineId);
            await _sellOrderItemExtendSync.RecalculateAsync(soLineId);
            var saveExtend = await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation(
                "[SellLineStockOutSync] UpdateStatus after Recalculate SellOrderItemId={SellOrderItemId} SaveChanges={Rows}",
                soLineId,
                saveExtend);
        }

        /// <summary>每条出库明细对应一条扩展行（主键与 <see cref="StockOutItem.Id"/> 相同）。</summary>
        /// <param name="lineQty">本条出库数量（与明细 <c>ActualQty</c>/<c>Quantity</c> 一致，本笔为 takeQty）。</param>
        /// <param name="aggregatePricingLayer">仅汇总层出库时：用于价快照的在库明细（采/销折 USD 与扩展行利润计算同源）。</param>
        private static StockOutItemExtend BuildStockOutItemExtend(
            StockOutItem outLine,
            StockItem? layer,
            StockInfo stock,
            int lineQty,
            StockItem? aggregatePricingLayer = null)
        {
            var ext = new StockOutItemExtend
            {
                Id = outLine.Id,
                StockItemId = string.IsNullOrWhiteSpace(outLine.StockItemId) ? null : outLine.StockItemId.Trim(),
                CreateTime = DateTime.UtcNow,
                QtyStockOut = lineQty,
            };
            if (layer != null)
            {
                FillStockOutItemExtendPricingFromLayer(ext, layer, lineQty);
                return ext;
            }

            ext.StockType = stock.StockType;
            ext.SellOrderItemId = string.IsNullOrWhiteSpace(stock.SellOrderItemId) ? null : stock.SellOrderItemId.Trim();
            ext.SellOrderItemCode = string.IsNullOrWhiteSpace(stock.SellOrderItemCode) ? null : stock.SellOrderItemCode.Trim();
            ext.PurchaseOrderItemId = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemId) ? null : stock.PurchaseOrderItemId.Trim();
            ext.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemCode) ? null : stock.PurchaseOrderItemCode.Trim();
            if (aggregatePricingLayer != null)
                FillStockOutItemExtendPricingFromLayer(ext, aggregatePricingLayer, lineQty);
            else
            {
                ext.PurchasePrice = 0m;
                ext.PurchaseCurrency = (short)CurrencyCode.RMB;
                ext.PurchasePriceUsd = 0m;
                ext.SalesPrice = null;
                ext.SalesCurrency = null;
                ext.SalesPriceUsd = null;
                ext.ProfitOutBizUsd = 0m;
            }

            return ext;
        }

        /// <summary>
        /// 扩展行利润与 <see cref="StockItem.ComputeProfitOutBizUsd"/> 公式一致（数量用本条出库量；<see cref="StockItem.ProfitOutBizUsd"/> 为入库 × <c>QtyInbound</c> 快照），
        /// 数量参数为<strong>本条出库明细的出库数量</strong> <paramref name="lineQty"/>（非层上累计 <c>QtyStockOut</c>）。
        /// </summary>
        private static void FillStockOutItemExtendPricingFromLayer(StockOutItemExtend ext, StockItem layer, int lineQty)
        {
            ext.StockType = layer.StockType;
            ext.SellOrderItemId = string.IsNullOrWhiteSpace(layer.SellOrderItemId) ? null : layer.SellOrderItemId.Trim();
            ext.SellOrderItemCode = string.IsNullOrWhiteSpace(layer.SellOrderItemCode) ? null : layer.SellOrderItemCode.Trim();
            ext.PurchaseOrderItemId = string.IsNullOrWhiteSpace(layer.PurchaseOrderItemId) ? null : layer.PurchaseOrderItemId.Trim();
            ext.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(layer.PurchaseOrderItemCode) ? null : layer.PurchaseOrderItemCode.Trim();
            ext.PurchasePrice = layer.PurchasePrice;
            ext.PurchaseCurrency = layer.PurchaseCurrency;
            ext.PurchasePriceUsd = layer.PurchasePriceUsd;
            ext.SalesPrice = layer.SalesPrice;
            ext.SalesCurrency = layer.SalesCurrency;
            ext.SalesPriceUsd = layer.SalesPriceUsd;
            ext.ProfitOutBizUsd = StockItem.ComputeProfitOutBizUsd(
                layer.SellOrderItemId,
                layer.SalesPriceUsd,
                layer.PurchasePriceUsd,
                lineQty);
        }

        /// <summary>汇总层出库无拣货 <c>stockitem</c> 时，找同桶、同销售行、同物料的一条在库明细用于价快照（FIFO 序第一条）。</summary>
        private static StockItem? FindPricingStockItemForAggregateOut(
            StockInfo aggregate,
            string sellOrderItemId,
            string materialId,
            string warehouseId,
            List<StockItem> allStockItems)
        {
            var aggId = aggregate.Id.Trim();
            var line = sellOrderItemId.Trim();
            var mat = materialId.Trim();
            var wh = warehouseId.Trim();
            return allStockItems
                .Where(si =>
                    string.Equals(si.StockAggregateId?.Trim(), aggId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.WarehouseId?.Trim(), wh, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.SellOrderItemId?.Trim(), line, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(si.MaterialId?.Trim(), mat, StringComparison.OrdinalIgnoreCase))
                .OrderBy(si => si.ProductionDate ?? si.CreateTime)
                .ThenBy(si => si.CreateTime)
                .FirstOrDefault();
        }
    }
}

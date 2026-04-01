using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services
{
    public class InventoryCenterService : IInventoryCenterService
    {
        private readonly IRepository<StockInfo> _stockRepository;
        private readonly IRepository<MaterialInfo> _materialRepository;
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IRepository<StockOutRequest> _stockOutRequestRepository;
        private readonly IRepository<StockOut> _stockOutRepository;
        private readonly IRepository<StockOutItem> _stockOutItemRepository;
        private readonly IRepository<InventoryLedger> _ledgerRepository;
        private readonly IRepository<WarehouseInfo> _warehouseRepository;
        private readonly IRepository<PickingTask> _pickingTaskRepository;
        private readonly IRepository<PickingTaskItem> _pickingTaskItemRepository;
        private readonly IRepository<InventoryCountPlan> _countPlanRepository;
        private readonly IRepository<InventoryCountItem> _countItemRepository;
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
        private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
        private readonly IRepository<QCInfo> _qcRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private static bool IsTableMissingException(Exception ex)
            => (ex.Message?.Contains("42P01") ?? false)
               || (ex.InnerException?.Message?.Contains("42P01") ?? false)
               || (ex.Message?.Contains("不存在", StringComparison.Ordinal) ?? false) && (ex.Message?.Contains("关系", StringComparison.Ordinal) ?? false);

        /// <summary>按 ProductId 合并 PN/品牌；避免先写入空行导致后续采购明细无法补全。</summary>
        private static void MergeProductDisplay(
            Dictionary<string, (string? Pn, string? Brand)> map,
            string? productId,
            string? pn,
            string? brand)
        {
            var pid = productId?.Trim();
            if (string.IsNullOrEmpty(pid))
                return;

            var normPn = string.IsNullOrWhiteSpace(pn) ? null : pn.Trim();
            var normBrand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();

            if (!map.TryGetValue(pid, out var cur))
            {
                map[pid] = (normPn, normBrand);
                return;
            }

            var mergedPn = string.IsNullOrWhiteSpace(cur.Pn) ? normPn : cur.Pn;
            var mergedBrand = string.IsNullOrWhiteSpace(cur.Brand) ? normBrand : cur.Brand;
            map[pid] = (mergedPn, mergedBrand);
        }

        public InventoryCenterService(
            IRepository<StockInfo> stockRepository,
            IRepository<MaterialInfo> materialRepository,
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IRepository<StockOutRequest> stockOutRequestRepository,
            IRepository<StockOut> stockOutRepository,
            IRepository<StockOutItem> stockOutItemRepository,
            IRepository<InventoryLedger> ledgerRepository,
            IRepository<WarehouseInfo> warehouseRepository,
            IRepository<PickingTask> pickingTaskRepository,
            IRepository<PickingTaskItem> pickingTaskItemRepository,
            IRepository<InventoryCountPlan> countPlanRepository,
            IRepository<InventoryCountItem> countItemRepository,
            IRepository<PurchaseOrder> purchaseOrderRepository,
            IRepository<SellOrderItem> sellOrderItemRepository,
            IRepository<PurchaseOrderItem> purchaseOrderItemRepository,
            IRepository<QCInfo> qcRepository,
            ISerialNumberService serialNumberService,
            IUnitOfWork unitOfWork)
        {
            _stockRepository = stockRepository;
            _materialRepository = materialRepository;
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _stockOutRequestRepository = stockOutRequestRepository;
            _stockOutRepository = stockOutRepository;
            _stockOutItemRepository = stockOutItemRepository;
            _ledgerRepository = ledgerRepository;
            _warehouseRepository = warehouseRepository;
            _pickingTaskRepository = pickingTaskRepository;
            _pickingTaskItemRepository = pickingTaskItemRepository;
            _countPlanRepository = countPlanRepository;
            _countItemRepository = countItemRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _sellOrderItemRepository = sellOrderItemRepository;
            _purchaseOrderItemRepository = purchaseOrderItemRepository;
            _qcRepository = qcRepository;
            _serialNumberService = serialNumberService;
            _unitOfWork = unitOfWork;
        }

        public async Task PostStockInAsync(string stockInId)
        {
            if (string.IsNullOrWhiteSpace(stockInId)) return;

            var stockIn = await _stockInRepository.GetByIdAsync(stockInId);
            if (stockIn == null) return;

            var lines = (await _stockInItemRepository.GetAllAsync())
                .Where(x => x.StockInId == stockInId)
                .ToList();
            if (!lines.Any()) return;

            var allStocks = (await _stockRepository.GetAllAsync()).ToList();
            var allLedgers = (await _ledgerRepository.GetAllAsync()).ToList();
            var changed = false;

            foreach (var line in lines)
            {
                if (allLedgers.Any(x => x.BizType == "STOCK_IN" && x.BizId == stockInId && x.BizLineId == line.Id))
                    continue;

                var stock = allStocks.FirstOrDefault(s =>
                    s.MaterialId == line.MaterialId &&
                    s.WarehouseId == stockIn.WarehouseId &&
                    (s.LocationId ?? string.Empty) == (line.LocationId ?? string.Empty) &&
                    (s.BatchNo ?? string.Empty) == (line.BatchNo ?? string.Empty));

                var isNewStock = stock == null;
                if (isNewStock)
                {
                    stock = new StockInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        MaterialId = line.MaterialId,
                        WarehouseId = stockIn.WarehouseId,
                        LocationId = line.LocationId,
                        BatchNo = line.BatchNo,
                        Unit = "PCS",
                        Status = 1,
                        CreateTime = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(stock);
                    allStocks.Add(stock);
                }

                stock.Qty += line.Quantity;
                stock.QtyRepertory = stock.Qty - stock.QtyStockOut;
                stock.QtyRepertoryAvailable = stock.QtyRepertory - stock.QtyOccupy - stock.QtySales;
                stock.Quantity = stock.QtyRepertory;
                stock.AvailableQuantity = stock.QtyRepertoryAvailable;
                stock.LockedQuantity = stock.QtyOccupy + stock.QtySales;
                stock.ModifyTime = DateTime.UtcNow;
                // 新增实体保持 Added 状态即可，避免被 Update 覆盖为 Modified 触发 0 行更新并发异常
                if (!isNewStock)
                {
                    await _stockRepository.UpdateAsync(stock);
                }

                var ledger = new InventoryLedger
                {
                    Id = Guid.NewGuid().ToString(),
                    BizType = "STOCK_IN",
                    BizId = stockInId,
                    BizLineId = line.Id,
                    MaterialId = line.MaterialId,
                    WarehouseId = stockIn.WarehouseId,
                    LocationId = line.LocationId,
                    BatchNo = line.BatchNo,
                    QtyIn = line.Quantity,
                    QtyOut = 0,
                    UnitCost = line.Price,
                    Amount = line.Amount,
                    Remark = $"入库单 {stockIn.StockInCode}",
                    CreateTime = DateTime.UtcNow
                };
                await _ledgerRepository.AddAsync(ledger);
                changed = true;
            }

            if (changed) await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecordStockOutAsync(string stockOutId)
        {
            if (string.IsNullOrWhiteSpace(stockOutId)) return;
            var stockOut = await _stockOutRepository.GetByIdAsync(stockOutId);
            if (stockOut == null) return;

            var allLedgers = (await _ledgerRepository.GetAllAsync()).ToList();
            var lines = (await _stockOutItemRepository.GetAllAsync()).Where(x => x.StockOutId == stockOutId).ToList();
            var stocks = (await _stockRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
            var changed = false;

            foreach (var line in lines)
            {
                if (allLedgers.Any(x => x.BizType == "STOCK_OUT" && x.BizId == stockOutId && x.BizLineId == line.Id))
                    continue;

                var cost = 0m;
                if (!string.IsNullOrWhiteSpace(line.StockId) && stocks.TryGetValue(line.StockId!, out var stock))
                    cost = stock.Qty > 0 ? (stock.QtyStockOut > 0 ? 0 : 0) : 0;

                var ledger = new InventoryLedger
                {
                    Id = Guid.NewGuid().ToString(),
                    BizType = "STOCK_OUT",
                    BizId = stockOutId,
                    BizLineId = line.Id,
                    MaterialId = line.MaterialId,
                    WarehouseId = line.WarehouseId ?? stockOut.WarehouseId,
                    LocationId = line.LocationId,
                    BatchNo = line.BatchNo,
                    QtyIn = 0,
                    QtyOut = line.ActualQty > 0 ? line.ActualQty : line.Quantity,
                    UnitCost = cost,
                    Amount = -(line.ActualQty > 0 ? line.ActualQty : line.Quantity) * cost,
                    Remark = $"出库单 {stockOut.StockOutCode}",
                    CreateTime = DateTime.UtcNow
                };
                await _ledgerRepository.AddAsync(ledger);
                changed = true;
            }

            if (changed) await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<InventoryMaterialOverviewDto>> GetMaterialOverviewAsync(string? warehouseId)
        {
            List<StockInfo> stocks;
            List<InventoryLedger> ledgers;
            try
            {
                stocks = (await _stockRepository.GetAllAsync())
                    .Where(x => string.IsNullOrWhiteSpace(warehouseId) || x.WarehouseId == warehouseId)
                    .ToList();
                ledgers = (await _ledgerRepository.GetAllAsync()).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                // 新功能表未迁移时降级返回空，避免页面整体报错
                return Array.Empty<InventoryMaterialOverviewDto>();
            }

            Dictionary<string, MaterialInfo> materialById = new();
            try
            {
                materialById = (await _materialRepository.GetAllAsync()).ToDictionary(m => m.Id, m => m);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                // material 表不可用时仍返回库存行，仅不展示型号/名称
            }

            // 库存行的 MaterialId 常与销售/采购明细的 ProductId 一致（调试链等为 GUID），PN/品牌在订单行上
            Dictionary<string, (string? Pn, string? Brand)> productLineByProductId = new(StringComparer.Ordinal);
            var poItemsList = new List<PurchaseOrderItem>();
            try
            {
                foreach (var line in await _sellOrderItemRepository.GetAllAsync())
                    MergeProductDisplay(productLineByProductId, line.ProductId, line.PN, line.Brand);

                poItemsList = (await _purchaseOrderItemRepository.GetAllAsync()).ToList();
                foreach (var line in poItemsList)
                    MergeProductDisplay(productLineByProductId, line.ProductId, line.PN, line.Brand);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
            }

            // 回退：入库单 SourceId=采购单时，按入库明细 MaterialId 对齐采购行（ProductId 为空或历史数据时）
            Dictionary<string, (string? Pn, string? Brand)> displayByStockMaterialId = new(StringComparer.Ordinal);
            try
            {
                var inLines = (await _stockInItemRepository.GetAllAsync()).ToList();
                var stockInById = (await _stockInRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
                foreach (var sil in inLines)
                {
                    var mid = sil.MaterialId?.Trim();
                    if (string.IsNullOrEmpty(mid) || displayByStockMaterialId.ContainsKey(mid))
                        continue;
                    if (!stockInById.TryGetValue(sil.StockInId, out var sin))
                        continue;
                    var poId = sin.SourceId?.Trim();
                    if (string.IsNullOrEmpty(poId))
                        continue;
                    var linesForPo = poItemsList.Where(p => p.PurchaseOrderId == poId).ToList();
                    if (linesForPo.Count == 0)
                        continue;
                    var hit = linesForPo.FirstOrDefault(p =>
                                  string.Equals(p.ProductId?.Trim(), mid, StringComparison.Ordinal))
                              ?? (linesForPo.Count == 1 ? linesForPo[0] : null);
                    if (hit == null)
                        continue;
                    displayByStockMaterialId[mid] = (hit.PN, hit.Brand);
                }
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
            }

            Dictionary<string, string> warehouseCodeById = new(StringComparer.Ordinal);
            try
            {
                foreach (var w in await _warehouseRepository.GetAllAsync())
                {
                    var id = w.Id?.Trim();
                    if (string.IsNullOrEmpty(id)) continue;
                    var code = string.IsNullOrWhiteSpace(w.WarehouseCode) ? null : w.WarehouseCode.Trim();
                    warehouseCodeById[id] = code ?? id;
                }
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
            }

            return stocks
                .GroupBy(x => new { x.MaterialId, x.WarehouseId })
                .Select(g =>
                {
                    var material = g.Key.MaterialId;
                    var warehouse = g.Key.WarehouseId;
                    materialById.TryGetValue(material, out var mat);
                    var model = string.IsNullOrWhiteSpace(mat?.MaterialModel) ? null : mat!.MaterialModel!.Trim();
                    var name = string.IsNullOrWhiteSpace(mat?.MaterialName) ? null : mat!.MaterialName!.Trim();
                    if ((model == null || name == null) && productLineByProductId.TryGetValue(material, out var pl))
                    {
                        if (model == null && !string.IsNullOrWhiteSpace(pl.Pn))
                            model = pl.Pn.Trim();
                        if (name == null && !string.IsNullOrWhiteSpace(pl.Brand))
                            name = pl.Brand.Trim();
                    }

                    if ((model == null || name == null) && displayByStockMaterialId.TryGetValue(material, out var sl))
                    {
                        if (model == null && !string.IsNullOrWhiteSpace(sl.Pn))
                            model = sl.Pn.Trim();
                        if (name == null && !string.IsNullOrWhiteSpace(sl.Brand))
                            name = sl.Brand.Trim();
                    }
                    var onHand = g.Sum(x => x.QtyRepertory);
                    var available = g.Sum(x => x.QtyRepertoryAvailable);
                    var locked = g.Sum(x => x.QtyOccupy + x.QtySales);
                    var avgCost = ledgers
                        .Where(x => x.MaterialId == material && x.WarehouseId == warehouse && x.BizType == "STOCK_IN" && x.QtyIn > 0)
                        .OrderByDescending(x => x.CreateTime)
                        .FirstOrDefault()?.UnitCost ?? 0m;
                    var lastMove = ledgers
                        .Where(x => x.MaterialId == material && x.WarehouseId == warehouse)
                        .OrderByDescending(x => x.CreateTime)
                        .FirstOrDefault()?.CreateTime;

                    string? whCode = null;
                    var whKey = warehouse?.Trim();
                    if (!string.IsNullOrEmpty(whKey) && warehouseCodeById.TryGetValue(whKey, out var resolved))
                        whCode = resolved;

                    return new InventoryMaterialOverviewDto
                    {
                        MaterialId = material,
                        MaterialModel = model,
                        MaterialName = name,
                        WarehouseId = warehouse ?? string.Empty,
                        WarehouseCode = whCode,
                        OnHandQty = onHand,
                        AvailableQty = available,
                        LockedQty = locked,
                        InventoryAmount = onHand * avgCost,
                        LastMoveTime = lastMove
                    };
                })
                .OrderByDescending(x => x.LastMoveTime ?? DateTime.MinValue)
                .ThenBy(x => x.WarehouseId, StringComparer.Ordinal)
                .ThenBy(x => x.MaterialId, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<IEnumerable<InventoryMaterialTraceDto>> GetMaterialTraceAsync(string materialId)
        {
            List<StockInItem> stockInLines;
            Dictionary<string, StockIn> stockInMap;
            Dictionary<string, PurchaseOrder> poMap;
            Dictionary<string, QCInfo> qcMap;
            try
            {
                stockInLines = (await _stockInItemRepository.GetAllAsync()).Where(x => x.MaterialId == materialId).ToList();
                stockInMap = (await _stockInRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
                poMap = (await _purchaseOrderRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
                qcMap = (await _qcRepository.GetAllAsync()).ToDictionary(x => x.StockInId ?? string.Empty, x => x);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return Array.Empty<InventoryMaterialTraceDto>();
            }

            Dictionary<string, string> warehouseNameById = new(StringComparer.Ordinal);
            try
            {
                var warehouses = await _warehouseRepository.GetAllAsync();
                warehouseNameById = warehouses
                    .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                    .ToDictionary(x => x.Id, x => x.WarehouseName?.Trim() ?? string.Empty, StringComparer.Ordinal);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                // ignore
            }

            return stockInLines
                .Where(x => stockInMap.ContainsKey(x.StockInId))
                .Select(line =>
                {
                    var stockIn = stockInMap[line.StockInId];
                    poMap.TryGetValue(stockIn.SourceId ?? string.Empty, out var po);
                    qcMap.TryGetValue(stockIn.Id, out var qc);
                    var wid = stockIn.WarehouseId;
                    warehouseNameById.TryGetValue(wid ?? string.Empty, out var wName);
                    return new InventoryMaterialTraceDto
                    {
                        StockInTime = stockIn.StockInDate,
                        StockInCode = stockIn.StockInCode,
                        BatchNo = line.BatchNo,
                        Quantity = line.Quantity,
                        UnitPrice = line.Price,
                        PurchaseOrderCode = po?.PurchaseOrderCode,
                        PurchaseUserName = po?.PurchaseUserName,
                        QcStatus = qc?.Status,
                        QcCode = qc?.QcCode,
                        WarehouseId = wid,
                        WarehouseName = string.IsNullOrWhiteSpace(wName) ? null : wName,
                        LocationId = line.LocationId
                    };
                })
                .OrderByDescending(x => x.StockInTime)
                .ToList();
        }

        public async Task<SellOrderLineAvailableQtyDto> GetAvailableQtyForSellOrderItemAsync(string sellOrderItemId)
        {
            if (string.IsNullOrWhiteSpace(sellOrderItemId))
                return new SellOrderLineAvailableQtyDto { AvailableQty = 0 };

            var id = sellOrderItemId.Trim();
            var soItem = await _sellOrderItemRepository.GetByIdAsync(id);
            if (soItem == null)
                return new SellOrderLineAvailableQtyDto { AvailableQty = 0 };

            var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(soItem.ProductId)) keySet.Add(soItem.ProductId.Trim());
            if (!string.IsNullOrWhiteSpace(soItem.PN)) keySet.Add(soItem.PN.Trim());

            try
            {
                var linkedPo = (await _purchaseOrderItemRepository.GetAllAsync())
                    .Where(p => string.Equals(p.SellOrderItemId, id, StringComparison.OrdinalIgnoreCase));
                foreach (var k in StockMaterialMatch.LinkedPurchaseMaterialKeys(linkedPo))
                    keySet.Add(k);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                // 无采购表明细时仅按销售行 ProductId / PN 匹配
            }

            if (keySet.Count == 0)
                return new SellOrderLineAvailableQtyDto { AvailableQty = 0 };

            List<StockInfo> stocks;
            try
            {
                stocks = (await _stockRepository.GetAllAsync()).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return new SellOrderLineAvailableQtyDto { AvailableQty = 0 };
            }

            var sum = stocks
                .Where(s => keySet.Contains(s.MaterialId?.Trim() ?? string.Empty))
                .Sum(s => s.QtyRepertoryAvailable);

            return new SellOrderLineAvailableQtyDto { AvailableQty = sum };
        }

        public async Task<InventoryFinanceSummaryDto> GetFinanceSummaryAsync(int stagnantDays = 90)
        {
            var overview = (await GetMaterialOverviewAsync(null)).ToList();
            List<InventoryLedger> ledgers;
            try
            {
                ledgers = (await _ledgerRepository.GetAllAsync()).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                ledgers = new List<InventoryLedger>();
            }
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var inventoryCapital = overview.Sum(x => x.InventoryAmount);
            var outCost = ledgers.Where(x => x.BizType == "STOCK_OUT" && x.CreateTime >= monthStart)
                .Sum(x => Math.Abs(x.Amount));
            var avgInventory = inventoryCapital;
            var turnoverRate = avgInventory <= 0 ? 0 : outCost / avgInventory;
            var turnoverDays = turnoverRate <= 0 ? 0 : 30 / turnoverRate;
            var staleThreshold = now.AddDays(-Math.Abs(stagnantDays));
            var stagnantCount = overview.Count(x => !x.LastMoveTime.HasValue || x.LastMoveTime.Value < staleThreshold);

            return new InventoryFinanceSummaryDto
            {
                InventoryCapital = decimal.Round(inventoryCapital, 2),
                MonthlyOutCost = decimal.Round(outCost, 2),
                AverageInventoryCapital = decimal.Round(avgInventory, 2),
                TurnoverRate = decimal.Round(turnoverRate, 4),
                TurnoverDays = decimal.Round(turnoverDays, 2),
                StagnantMaterialCount = stagnantCount
            };
        }

        public async Task<IEnumerable<WarehouseInfo>> GetWarehousesAsync()
        {
            try
            {
                return (await _warehouseRepository.GetAllAsync()).OrderBy(x => x.WarehouseCode).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return Array.Empty<WarehouseInfo>();
            }
        }

        public async Task<WarehouseInfo> SaveWarehouseAsync(WarehouseInfo warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.WarehouseCode))
                throw new ArgumentException("仓库编码不能为空");
            if (string.IsNullOrWhiteSpace(warehouse.WarehouseName))
                throw new ArgumentException("仓库名称不能为空");

            WarehouseInfo? existing = null;
            if (!string.IsNullOrWhiteSpace(warehouse.Id))
                existing = await _warehouseRepository.GetByIdAsync(warehouse.Id);

            if (existing == null)
            {
                if (string.IsNullOrWhiteSpace(warehouse.Id))
                    warehouse.Id = Guid.NewGuid().ToString();
                warehouse.CreateTime = DateTime.UtcNow;
                await _warehouseRepository.AddAsync(warehouse);
                await _unitOfWork.SaveChangesAsync();
                return warehouse;
            }

            // 必须用已跟踪的 existing 更新，避免与 GetById 跟踪实例冲突（同一 Id 不能跟踪两个实例）
            existing.WarehouseCode = warehouse.WarehouseCode.Trim();
            existing.WarehouseName = warehouse.WarehouseName.Trim();
            existing.Address = string.IsNullOrWhiteSpace(warehouse.Address) ? null : warehouse.Address.Trim();
            existing.Status = warehouse.Status;
            existing.ModifyTime = DateTime.UtcNow;
            await _warehouseRepository.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            return existing;
        }

        public async Task<IEnumerable<PickingTaskSummaryDto>> GetPickingTasksAsync(short? status = null)
        {
            IEnumerable<PickingTask> tasks;
            try
            {
                tasks = await _pickingTaskRepository.GetAllAsync();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return Array.Empty<PickingTaskSummaryDto>();
            }
            var list = tasks
                .Where(x => !status.HasValue || x.Status == status.Value)
                .OrderByDescending(x => x.CreateTime)
                .ToList();

            List<PickingTaskItem> taskItems;
            try
            {
                taskItems = (await _pickingTaskItemRepository.GetAllAsync()).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                taskItems = new List<PickingTaskItem>();
            }

            var sumByTask = taskItems
                .GroupBy(i => i.PickingTaskId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (Plan: g.Sum(x => x.PlanQty), Picked: g.Sum(x => x.PickedQty)),
                    StringComparer.OrdinalIgnoreCase);

            return list.Select(t =>
            {
                decimal plan = 0, picked = 0;
                if (sumByTask.TryGetValue(t.Id, out var s))
                {
                    plan = s.Plan;
                    picked = s.Picked;
                }
                return new PickingTaskSummaryDto
                {
                    Id = t.Id,
                    TaskCode = t.TaskCode,
                    StockOutRequestId = t.StockOutRequestId,
                    WarehouseId = t.WarehouseId,
                    OperatorId = t.OperatorId,
                    Status = t.Status,
                    Remark = t.Remark,
                    CreateTime = t.CreateTime,
                    PlanQtyTotal = plan,
                    PickedQtyTotal = picked
                };
            }).ToList();
        }

        public async Task<PickingTask> GeneratePickingTaskAsync(GeneratePickingTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StockOutRequestId)) throw new ArgumentException("出库申请ID不能为空");
            if (string.IsNullOrWhiteSpace(request.WarehouseId)) throw new ArgumentException("仓库ID不能为空");
            if (request.Items == null || request.Items.Count == 0) throw new ArgumentException("拣货明细不能为空");

            var sorId = request.StockOutRequestId.Trim();
            var sor = await _stockOutRequestRepository.GetByIdAsync(sorId)
                ?? throw new InvalidOperationException("出库申请不存在");

            // 同一出库通知只允许存在一条未取消的拣货任务，避免重复生成导致计划量叠加超过出库数量
            var existingForSor = (await _pickingTaskRepository.GetAllAsync())
                .Where(x => string.Equals(x.StockOutRequestId, sorId, StringComparison.OrdinalIgnoreCase) && x.Status != -1)
                .ToList();
            if (existingForSor.Count > 0)
            {
                var codes = string.Join("、", existingForSor.Select(x => x.TaskCode));
                throw new InvalidOperationException($"该出库通知已存在拣货任务（{codes}），请勿重复生成。");
            }

            var soItem = await _sellOrderItemRepository.GetByIdAsync(sor.SalesOrderItemId.Trim());
            var productIdFromLine = string.IsNullOrWhiteSpace(soItem?.ProductId) ? null : soItem!.ProductId!.Trim();
            var sellLineId = sor.SalesOrderItemId.Trim();
            var linkedPoItems = (await _purchaseOrderItemRepository.GetAllAsync())
                .Where(p => string.Equals(p.SellOrderItemId, sellLineId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var linkedMaterialKeys = StockMaterialMatch.LinkedPurchaseMaterialKeys(linkedPoItems);

            var taskCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PickingTask);

            var task = new PickingTask
            {
                Id = Guid.NewGuid().ToString(),
                TaskCode = taskCode,
                StockOutRequestId = sorId,
                WarehouseId = request.WarehouseId,
                OperatorId = string.IsNullOrWhiteSpace(request.OperatorId) ? "SYSTEM" : request.OperatorId,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            await _pickingTaskRepository.AddAsync(task);

            var stocks = (await _stockRepository.GetAllAsync())
                .Where(x => x.WarehouseId == request.WarehouseId)
                .OrderBy(x => x.ProductionDate ?? x.CreateTime)
                .ThenBy(x => x.CreateTime)
                .ToList();

            foreach (var item in request.Items)
            {
                var need = item.Quantity;
                var outboundCode = item.MaterialId?.Trim() ?? "";
                foreach (var stock in stocks.Where(s =>
                             StockMaterialMatch.Matches(s, outboundCode, productIdFromLine, linkedMaterialKeys) && s.QtyRepertoryAvailable > 0))
                {
                    if (need <= 0) break;
                    var qty = Math.Min(need, stock.QtyRepertoryAvailable);
                    if (qty <= 0) continue;
                    await _pickingTaskItemRepository.AddAsync(new PickingTaskItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PickingTaskId = task.Id,
                        MaterialId = stock.MaterialId,
                        StockId = stock.Id,
                        BatchNo = stock.BatchNo,
                        LocationId = stock.LocationId,
                        PlanQty = qty,
                        PickedQty = 0,
                        CreateTime = DateTime.UtcNow
                    });
                    need -= qty;
                }
                if (need > 0)
                {
                    var availableTotal = stocks
                        .Where(s => StockMaterialMatch.Matches(s, outboundCode, productIdFromLine, linkedMaterialKeys))
                        .Sum(s => s.QtyRepertoryAvailable);
                    var wh = await _warehouseRepository.GetByIdAsync(request.WarehouseId.Trim());
                    var whLabel = wh != null ? $"{wh.WarehouseName}（{wh.WarehouseCode}）" : request.WarehouseId;
                    throw new InvalidOperationException(
                        $"仓库「{whLabel}」中物料「{outboundCode}」可用库存合计 {QuantityMessageFormatting.ForUserMessage(availableTotal)}，本单需求 {QuantityMessageFormatting.ForUserMessage(item.Quantity)}，仍缺 {QuantityMessageFormatting.ForUserMessage(need)}。" +
                        " 请入库、更换仓库或调减出库通知数量。" +
                        (string.IsNullOrEmpty(productIdFromLine)
                            ? "（若库存以 ProductId 入库，请为销售订单明细维护 ProductId）"
                            : ""));
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return task;
        }

        public async Task CompletePickingTaskAsync(string taskId)
        {
            var task = await _pickingTaskRepository.GetByIdAsync(taskId);
            if (task == null) throw new InvalidOperationException("拣货任务不存在");
            task.Status = 100;
            task.ModifyTime = DateTime.UtcNow;

            var items = (await _pickingTaskItemRepository.GetAllAsync()).Where(x => x.PickingTaskId == taskId).ToList();
            foreach (var item in items)
            {
                item.PickedQty = item.PlanQty;
                item.ModifyTime = DateTime.UtcNow;
                await _pickingTaskItemRepository.UpdateAsync(item);
            }

            await _pickingTaskRepository.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<InventoryCountPlan>> GetCountPlansAsync()
        {
            try
            {
                return (await _countPlanRepository.GetAllAsync()).OrderByDescending(x => x.PlanMonth).ThenByDescending(x => x.CreateTime).ToList();
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                return Array.Empty<InventoryCountPlan>();
            }
        }

        public async Task<InventoryCountPlan> CreateMonthlyCountPlanAsync(CreateCountPlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlanMonth)) throw new ArgumentException("盘点月份不能为空");
            if (string.IsNullOrWhiteSpace(request.WarehouseId)) throw new ArgumentException("仓库ID不能为空");

            var exists = (await _countPlanRepository.GetAllAsync())
                .Any(x => x.PlanMonth == request.PlanMonth && x.WarehouseId == request.WarehouseId && x.Status != -1);
            if (exists) throw new InvalidOperationException("该月份仓库盘点计划已存在");

            var plan = new InventoryCountPlan
            {
                Id = Guid.NewGuid().ToString(),
                PlanMonth = request.PlanMonth,
                WarehouseId = request.WarehouseId,
                CreatorId = request.CreatorId,
                Remark = request.Remark,
                Status = 10,
                CreateTime = DateTime.UtcNow
            };
            await _countPlanRepository.AddAsync(plan);
            await _unitOfWork.SaveChangesAsync();
            return plan;
        }

        public async Task SubmitCountPlanAsync(SubmitCountPlanRequest request)
        {
            var plan = await _countPlanRepository.GetByIdAsync(request.PlanId);
            if (plan == null) throw new InvalidOperationException("盘点计划不存在");
            if (request.Items == null || request.Items.Count == 0) throw new ArgumentException("盘点明细不能为空");

            var stocks = (await _stockRepository.GetAllAsync())
                .Where(x => x.WarehouseId == plan.WarehouseId)
                .ToList();

            foreach (var item in request.Items)
            {
                var stockGroup = stocks.Where(x => x.MaterialId == item.MaterialId && (x.LocationId ?? string.Empty) == (item.LocationId ?? string.Empty)).ToList();
                var bookQty = stockGroup.Sum(x => x.QtyRepertory);
                var bookAmount = 0m;
                var diffQty = item.CountQty - bookQty;
                var diffAmount = item.CountAmount - bookAmount;

                var countItem = new InventoryCountItem
                {
                    Id = Guid.NewGuid().ToString(),
                    PlanId = plan.Id,
                    MaterialId = item.MaterialId,
                    LocationId = item.LocationId,
                    BookQty = bookQty,
                    CountQty = item.CountQty,
                    DiffQty = diffQty,
                    BookAmount = bookAmount,
                    CountAmount = item.CountAmount,
                    DiffAmount = diffAmount,
                    CreateTime = DateTime.UtcNow
                };
                await _countItemRepository.AddAsync(countItem);

                if (diffQty != 0)
                {
                    var target = stockGroup.FirstOrDefault();
                    var isNewTarget = target == null;
                    if (isNewTarget)
                    {
                        target = new StockInfo
                        {
                            Id = Guid.NewGuid().ToString(),
                            MaterialId = item.MaterialId,
                            WarehouseId = plan.WarehouseId,
                            LocationId = item.LocationId,
                            Status = 1,
                            CreateTime = DateTime.UtcNow
                        };
                        await _stockRepository.AddAsync(target);
                    }
                    target.Qty += diffQty;
                    target.QtyRepertory += diffQty;
                    target.QtyRepertoryAvailable += diffQty;
                    target.Quantity = target.QtyRepertory;
                    target.AvailableQuantity = target.QtyRepertoryAvailable;
                    target.ModifyTime = DateTime.UtcNow;
                    if (!isNewTarget)
                    {
                        await _stockRepository.UpdateAsync(target);
                    }

                    await _ledgerRepository.AddAsync(new InventoryLedger
                    {
                        Id = Guid.NewGuid().ToString(),
                        BizType = "COUNT_ADJUST",
                        BizId = plan.Id,
                        BizLineId = countItem.Id,
                        MaterialId = item.MaterialId,
                        WarehouseId = plan.WarehouseId,
                        LocationId = item.LocationId,
                        QtyIn = diffQty > 0 ? diffQty : 0,
                        QtyOut = diffQty < 0 ? -diffQty : 0,
                        UnitCost = 0,
                        Amount = diffAmount,
                        Remark = $"月度盘点调整 {plan.PlanMonth}",
                        CreateTime = DateTime.UtcNow
                    });
                }
            }

            plan.SubmitterId = request.SubmitterId;
            plan.Status = 100;
            plan.ModifyTime = DateTime.UtcNow;
            await _countPlanRepository.UpdateAsync(plan);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}


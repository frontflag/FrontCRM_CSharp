using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<InventoryCenterService> _logger;
        private static bool IsTableMissingException(Exception ex)
            => (ex.Message?.Contains("42P01") ?? false)
               || (ex.InnerException?.Message?.Contains("42P01") ?? false)
               || (ex.Message?.Contains("不存在", StringComparison.Ordinal) ?? false) && (ex.Message?.Contains("关系", StringComparison.Ordinal) ?? false);

        /// <summary>
        /// 按 <c>stockinitem.MaterialId</c> 视为采购明细主键，回填 <see cref="StockInfo.PurchasePn"/> / <see cref="StockInfo.PurchaseBrand"/>；
        /// 解析不到则不改写字段（保留已有值）。
        /// </summary>
        private async Task ApplyPurchasePnBrandFromStockInLineAsync(StockInfo stock, StockInItem line)
        {
            var key = line.MaterialId?.Trim();
            if (string.IsNullOrEmpty(key))
                return;

            var poi = await _purchaseOrderItemRepository.GetByIdAsync(key);
            if (poi == null)
                return;

            stock.PurchasePn = string.IsNullOrWhiteSpace(poi.PN) ? null : poi.PN.Trim();
            stock.PurchaseBrand = string.IsNullOrWhiteSpace(poi.Brand) ? null : poi.Brand.Trim();
        }

        private async Task<PurchaseOrderItem?> TryGetPoItemByStockInLineAsync(StockInItem line)
        {
            var key = line.MaterialId?.Trim();
            if (string.IsNullOrEmpty(key))
                return null;
            return await _purchaseOrderItemRepository.GetByIdAsync(key);
        }

        /// <summary>
        /// 库存分桶键：<c>PurchasePn</c> + <c>PurchaseBrand</c> + 仓库 + <c>StockType</c> + <c>RegionType</c> + <c>SellOrderItemId</c>
        /// （文本字段忽略大小写，空串视为同一空键；地域按 <see cref="RegionTypeCode.Normalize"/> 比较）。
        /// </summary>
        private static string NormStockBucketText(string? v) =>
            string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim();

        private static bool StockMatchesInboundBucket(
            StockInfo s,
            string purchasePnKey,
            string purchaseBrandKey,
            string warehouseId,
            short stockType,
            short regionType,
            string sellOrderItemKey)
        {
            var wh = warehouseId.Trim();
            return s.StockType == stockType
                   && RegionTypeCode.Normalize(s.RegionType) == RegionTypeCode.Normalize(regionType)
                   && string.Equals(s.WarehouseId?.Trim() ?? string.Empty, wh, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(NormStockBucketText(s.PurchasePn), purchasePnKey, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(NormStockBucketText(s.PurchaseBrand), purchaseBrandKey, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(NormStockBucketText(s.SellOrderItemId), sellOrderItemKey, StringComparison.OrdinalIgnoreCase);
        }

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
            IUnitOfWork unitOfWork,
            ILogger<InventoryCenterService> logger)
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
            _logger = logger;
        }

        public async Task PostStockInAsync(string stockInId)
        {
            if (string.IsNullOrWhiteSpace(stockInId)) return;

            _logger.LogInformation("[InboundStatus2] PostStockIn begin StockInId={StockInId}", stockInId);

            var stockIn = await _stockInRepository.GetByIdAsync(stockInId);
            if (stockIn == null)
            {
                _logger.LogWarning("[InboundStatus2] PostStockIn missing stockin StockInId={StockInId}", stockInId);
                return;
            }

            var lines = (await _stockInItemRepository.GetAllAsync())
                .Where(x => x.StockInId == stockInId)
                .ToList();
            if (!lines.Any())
            {
                _logger.LogWarning(
                    "[InboundStatus2] PostStockIn no lines StockInId={StockInId} Code={Code}",
                    stockInId,
                    stockIn.StockInCode ?? "");
                return;
            }

            _logger.LogInformation(
                "[InboundStatus2] PostStockIn loaded StockInId={StockInId} Code={Code} LineCount={LineCount} PoItemId={PoItemId} PoItemCode={PoItemCode}",
                stockInId,
                stockIn.StockInCode ?? "",
                lines.Count,
                stockIn.PurchaseOrderItemId ?? "",
                stockIn.PurchaseOrderItemCode ?? "");

            var allStocks = (await _stockRepository.GetAllAsync()).ToList();
            var allLedgers = (await _ledgerRepository.GetAllAsync()).ToList();
            var changed = false;
            var inboundStockType = await ResolveStockTypeForStockInAsync(stockIn);

            foreach (var line in lines)
            {
                if (allLedgers.Any(x => x.BizType == "STOCK_IN" && x.BizId == stockInId && x.BizLineId == line.Id))
                    continue;

                var poiForBucket = await TryGetPoItemByStockInLineAsync(line);
                var pnKey = NormStockBucketText(poiForBucket?.PN);
                var brKey = NormStockBucketText(poiForBucket?.Brand);
                var soKey = NormStockBucketText(stockIn.SellOrderItemId);
                var whKey = stockIn.WarehouseId?.Trim() ?? string.Empty;
                var regionKey = RegionTypeCode.Normalize(stockIn.RegionType);

                var stock = allStocks.FirstOrDefault(s =>
                    StockMatchesInboundBucket(s, pnKey, brKey, whKey, inboundStockType, regionKey, soKey));

                var isNewStock = stock == null;
                if (isNewStock)
                {
                    var stockCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Stock);
                    stock = new StockInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockCode = stockCode,
                        MaterialId = line.MaterialId,
                        WarehouseId = stockIn.WarehouseId,
                        LocationId = line.LocationId,
                        BatchNo = line.BatchNo,
                        Unit = "PCS",
                        Status = 1,
                        StockType = inboundStockType,
                        RegionType = RegionTypeCode.Normalize(stockIn.RegionType),
                        CreateTime = DateTime.UtcNow,
                        PurchasePn = string.IsNullOrWhiteSpace(poiForBucket?.PN) ? null : poiForBucket!.PN!.Trim(),
                        PurchaseBrand = string.IsNullOrWhiteSpace(poiForBucket?.Brand) ? null : poiForBucket!.Brand!.Trim()
                    };
                    await _stockRepository.AddAsync(stock);
                    allStocks.Add(stock);
                }

                CopyStockInOrderLineHeadersToStock(stock!, stockIn);
                await ApplyPurchasePnBrandFromStockInLineAsync(stock!, line);

                var stRow = stock!;
                stRow.Qty += line.Quantity;
                stRow.QtyRepertory = stRow.Qty - stRow.QtyStockOut;
                stRow.QtyRepertoryAvailable = stRow.QtyRepertory - stRow.QtyOccupy - stRow.QtySales;
                stRow.ModifyTime = DateTime.UtcNow;
                // 新增实体保持 Added 状态即可，避免被 Update 覆盖为 Modified 触发 0 行更新并发异常
                if (!isNewStock)
                {
                    await _stockRepository.UpdateAsync(stRow);
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
                CopyStockOrderLineHeadersFromStockToLedger(ledger, stRow);
                await _ledgerRepository.AddAsync(ledger);
                changed = true;
            }

            if (changed)
            {
                _logger.LogInformation("[InboundStatus2] PostStockIn before SaveChanges StockInId={StockInId}", stockInId);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("[InboundStatus2] PostStockIn after SaveChanges StockInId={StockInId}", stockInId);
            }
            else
            {
                _logger.LogInformation("[InboundStatus2] PostStockIn no ledger changes StockInId={StockInId}", stockInId);
            }
        }

        /// <summary>入库过账时把 stockin 头上的采购/销售行冗余到库存行（与头单主行一致）。</summary>
        private static void CopyStockInOrderLineHeadersToStock(StockInfo stock, StockIn stockIn)
        {
            stock.PurchaseOrderItemId = string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemId)
                ? null
                : stockIn.PurchaseOrderItemId.Trim();
            stock.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemCode)
                ? null
                : stockIn.PurchaseOrderItemCode.Trim();
            stock.SellOrderItemId = string.IsNullOrWhiteSpace(stockIn.SellOrderItemId)
                ? null
                : stockIn.SellOrderItemId.Trim();
            stock.SellOrderItemCode = string.IsNullOrWhiteSpace(stockIn.SellOrderItemCode)
                ? null
                : stockIn.SellOrderItemCode.Trim();
            stock.RegionType = RegionTypeCode.Normalize(stockIn.RegionType);
        }

        /// <summary>流水行冗余采购/销售明细（与当前 <c>stock</c> 行一致）。</summary>
        private static void CopyStockOrderLineHeadersFromStockToLedger(InventoryLedger ledger, StockInfo? stock)
        {
            if (stock == null) return;
            ledger.PurchaseOrderItemId = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemId)
                ? null
                : stock.PurchaseOrderItemId.Trim();
            ledger.PurchaseOrderItemCode = string.IsNullOrWhiteSpace(stock.PurchaseOrderItemCode)
                ? null
                : stock.PurchaseOrderItemCode.Trim();
            ledger.SellOrderItemId = string.IsNullOrWhiteSpace(stock.SellOrderItemId)
                ? null
                : stock.SellOrderItemId.Trim();
            ledger.SellOrderItemCode = string.IsNullOrWhiteSpace(stock.SellOrderItemCode)
                ? null
                : stock.SellOrderItemCode.Trim();
        }

        private static short NormalizeStockInventoryType(short type) => type is >= 1 and <= 3 ? type : (short)1;

        /// <summary>入库过账：库存类型与采购订单头 <c>Type</c> 一致（经采购订单明细关联）。</summary>
        private async Task<short> ResolveStockTypeForStockInAsync(StockIn stockIn)
        {
            if (string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemId))
                return 1;
            var poLine = await _purchaseOrderItemRepository.GetByIdAsync(stockIn.PurchaseOrderItemId.Trim());
            if (poLine == null || string.IsNullOrWhiteSpace(poLine.PurchaseOrderId))
                return 1;
            var po = await _purchaseOrderRepository.GetByIdAsync(poLine.PurchaseOrderId.Trim());
            return po == null ? (short)1 : NormalizeStockInventoryType(po.Type);
        }

        /// <summary>按销售明细关联的采购单头类型集合（用于出库/拣货/可用量与 <c>stock.StockType</c> 对齐）。</summary>
        private async Task<HashSet<short>> ResolveStockTypesForSellOrderLineAsync(string sellOrderItemId)
        {
            var id = sellOrderItemId.Trim();
            var poById = (await _purchaseOrderRepository.GetAllAsync())
                .GroupBy(x => x.Id?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var types = new HashSet<short>();
            foreach (var pil in await _purchaseOrderItemRepository.GetAllAsync())
            {
                if (!string.Equals(pil.SellOrderItemId?.Trim(), id, StringComparison.OrdinalIgnoreCase))
                    continue;
                var poid = pil.PurchaseOrderId?.Trim();
                if (string.IsNullOrEmpty(poid) || !poById.TryGetValue(poid, out var po))
                    continue;
                types.Add(NormalizeStockInventoryType(po.Type));
            }

            if (types.Count == 0)
                types.Add(1);
            return types;
        }

        private async Task<HashSet<short>> ResolveStockTypesForStockOutAsync(StockOut stockOut)
        {
            if (string.IsNullOrWhiteSpace(stockOut.SellOrderItemId))
                return new HashSet<short> { 1 };
            return await ResolveStockTypesForSellOrderLineAsync(stockOut.SellOrderItemId.Trim());
        }

        public async Task RecordStockOutAsync(string stockOutId)
        {
            if (string.IsNullOrWhiteSpace(stockOutId)) return;
            var stockOut = await _stockOutRepository.GetByIdAsync(stockOutId);
            if (stockOut == null) return;

            var outStockTypes = await ResolveStockTypesForStockOutAsync(stockOut);

            var allLedgers = (await _ledgerRepository.GetAllAsync()).ToList();
            var lines = (await _stockOutItemRepository.GetAllAsync()).Where(x => x.StockOutId == stockOutId).ToList();
            var stocks = (await _stockRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
            var changed = false;

            foreach (var line in lines)
            {
                if (allLedgers.Any(x => x.BizType == "STOCK_OUT" && x.BizId == stockOutId && x.BizLineId == line.Id))
                    continue;

                var cost = 0m;
                StockInfo? rowStock = null;
                if (!string.IsNullOrWhiteSpace(line.StockId) && stocks.TryGetValue(line.StockId.Trim(), out var stockById))
                    rowStock = stockById;
                if (rowStock == null)
                {
                    var whOut = line.WarehouseId ?? stockOut.WarehouseId;
                    rowStock = stocks.Values.FirstOrDefault(s =>
                        s.MaterialId == line.MaterialId &&
                        s.WarehouseId == whOut &&
                        (s.LocationId ?? string.Empty) == (line.LocationId ?? string.Empty) &&
                        (s.BatchNo ?? string.Empty) == (line.BatchNo ?? string.Empty) &&
                        outStockTypes.Contains(s.StockType));
                }

                if (rowStock != null)
                    cost = rowStock.Qty > 0 ? (rowStock.QtyStockOut > 0 ? 0 : 0) : 0;

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
                CopyStockOrderLineHeadersFromStockToLedger(ledger, rowStock);
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

            // 回退：入库单头带采购行主键时，按入库明细 MaterialId 与该行对齐（ProductId 为空或历史数据时）
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
                    var poLineId = sin.PurchaseOrderItemId?.Trim();
                    if (string.IsNullOrEmpty(poLineId))
                        continue;
                    var hit = poItemsList.FirstOrDefault(p =>
                        string.Equals(p.Id?.Trim(), poLineId, StringComparison.OrdinalIgnoreCase));
                    if (hit == null)
                        continue;
                    if (!string.Equals(mid, hit.Id, StringComparison.OrdinalIgnoreCase) &&
                        !(string.Equals(mid, hit.ProductId?.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                        !(string.Equals(mid, hit.PN?.Trim(), StringComparison.OrdinalIgnoreCase)))
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

            Dictionary<string, StockIn> overviewStockInById = new(StringComparer.Ordinal);
            Dictionary<string, PurchaseOrder> overviewPoById = new(StringComparer.Ordinal);
            Dictionary<string, string> overviewPoIdByPoLineId = new(StringComparer.Ordinal);
            try
            {
                foreach (var sin in await _stockInRepository.GetAllAsync())
                {
                    var sid = sin.Id?.Trim();
                    if (!string.IsNullOrEmpty(sid))
                        overviewStockInById[sid] = sin;
                }

                foreach (var po in await _purchaseOrderRepository.GetAllAsync())
                {
                    var pid = po.Id?.Trim();
                    if (!string.IsNullOrEmpty(pid))
                        overviewPoById[pid] = po;
                }

                foreach (var pil in await _purchaseOrderItemRepository.GetAllAsync())
                {
                    var lid = pil.Id?.Trim();
                    var poid = pil.PurchaseOrderId?.Trim();
                    if (!string.IsNullOrEmpty(lid) && !string.IsNullOrEmpty(poid))
                        overviewPoIdByPoLineId[lid] = poid;
                }
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
            }

            return stocks
                .Select(s =>
                {
                    var material = s.MaterialId ?? string.Empty;
                    var warehouse = s.WarehouseId ?? string.Empty;
                    var stockType = s.StockType;
                    var model = string.IsNullOrWhiteSpace(s.PurchasePn) ? null : s.PurchasePn.Trim();
                    var name = string.IsNullOrWhiteSpace(s.PurchaseBrand) ? null : s.PurchaseBrand.Trim();
                    materialById.TryGetValue(material, out var mat);
                    if (string.IsNullOrWhiteSpace(model))
                        model = string.IsNullOrWhiteSpace(mat?.MaterialModel) ? null : mat!.MaterialModel!.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        name = string.IsNullOrWhiteSpace(mat?.MaterialName) ? null : mat!.MaterialName!.Trim();
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

                    var onHand = s.QtyRepertory;
                    var available = s.QtyRepertoryAvailable;
                    var locked = s.QtyOccupy + s.QtySales;
                    var latestIn = ledgers
                        .Where(x => x.MaterialId == material && x.WarehouseId == warehouse && x.BizType == "STOCK_IN" && x.QtyIn > 0)
                        .OrderByDescending(x => x.CreateTime)
                        .FirstOrDefault();
                    var avgCost = latestIn?.UnitCost ?? 0m;
                    short amountCurrency = 1;
                    if (latestIn != null &&
                        !string.IsNullOrWhiteSpace(latestIn.BizId) &&
                        overviewStockInById.TryGetValue(latestIn.BizId.Trim(), out var sinForCur) &&
                        !string.IsNullOrWhiteSpace(sinForCur.PurchaseOrderItemId) &&
                        overviewPoIdByPoLineId.TryGetValue(sinForCur.PurchaseOrderItemId.Trim(), out var poIdForCur) &&
                        overviewPoById.TryGetValue(poIdForCur.Trim(), out var poForCur) &&
                        poForCur.Currency > 0)
                    {
                        amountCurrency = poForCur.Currency;
                    }

                    var lastMove = ledgers
                        .Where(x => x.MaterialId == material && x.WarehouseId == warehouse)
                        .OrderByDescending(x => x.CreateTime)
                        .FirstOrDefault()?.CreateTime;

                    string? whCode = null;
                    var whKey = warehouse.Trim();
                    if (!string.IsNullOrEmpty(whKey) && warehouseCodeById.TryGetValue(whKey, out var resolved))
                        whCode = resolved;

                    var overviewRegionType = RegionTypeCode.Normalize(s.RegionType);

                    return new InventoryMaterialOverviewDto
                    {
                        StockId = s.Id ?? string.Empty,
                        StockCode = string.IsNullOrWhiteSpace(s.StockCode) ? null : s.StockCode.Trim(),
                        MaterialId = material,
                        MaterialModel = model,
                        MaterialName = name,
                        WarehouseId = warehouse,
                        WarehouseCode = whCode,
                        StockType = stockType,
                        RegionType = overviewRegionType,
                        OnHandQty = onHand,
                        AvailableQty = available,
                        LockedQty = locked,
                        InventoryAmount = onHand * avgCost,
                        Currency = amountCurrency,
                        LastMoveTime = lastMove
                    };
                })
                .OrderByDescending(x => x.LastMoveTime ?? DateTime.MinValue)
                .ThenBy(x => x.WarehouseId, StringComparer.Ordinal)
                .ThenBy(x => x.StockType)
                .ThenBy(x => x.MaterialId, StringComparer.Ordinal)
                .ThenBy(x => x.StockId, StringComparer.Ordinal)
                .ToList();
        }

        public async Task<IEnumerable<InventoryMaterialTraceDto>> GetMaterialTraceAsync(string materialId)
        {
            List<StockInItem> stockInLines;
            Dictionary<string, StockIn> stockInMap;
            Dictionary<string, PurchaseOrder> poMap;
            Dictionary<string, string> poIdByPoLineId;
            Dictionary<string, QCInfo> qcMap;
            try
            {
                stockInLines = (await _stockInItemRepository.GetAllAsync()).Where(x => x.MaterialId == materialId).ToList();
                stockInMap = (await _stockInRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
                poMap = (await _purchaseOrderRepository.GetAllAsync()).ToDictionary(x => x.Id, x => x);
                poIdByPoLineId = (await _purchaseOrderItemRepository.GetAllAsync())
                    .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                    .ToDictionary(x => x.Id.Trim(), x => x.PurchaseOrderId.Trim(), StringComparer.OrdinalIgnoreCase);
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
                    PurchaseOrder? po = null;
                    if (!string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemId) &&
                        poIdByPoLineId.TryGetValue(stockIn.PurchaseOrderItemId.Trim(), out var poid) &&
                        poMap.TryGetValue(poid, out var poHit))
                        po = poHit;
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
            if (await _sellOrderItemRepository.GetByIdAsync(id) == null)
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

            // 与拣货任务一致：仅统计 stock.sell_order_item_id = 本销售明细 且 可用量 > 0 的行的可用量之和（跨仓库汇总）
            var sum = 0m;
            foreach (var s in stocks)
            {
                if (s.QtyRepertoryAvailable <= 0m)
                    continue;
                if (!string.Equals(s.SellOrderItemId?.Trim(), id, StringComparison.OrdinalIgnoreCase))
                    continue;
                sum += s.QtyRepertoryAvailable;
            }

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

            var regionType = RegionTypeCode.Normalize(warehouse.RegionType);
            warehouse.RegionType = regionType;

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
            existing.RegionType = regionType;
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

            var linesByTask = taskItems
                .GroupBy(i => i.PickingTaskId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            Dictionary<string, StockInfo> stockById = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                stockById = (await _stockRepository.GetAllAsync())
                    .GroupBy(s => s.Id?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Key.Length > 0)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (IsTableMissingException(ex))
            {
                // 忽略，StockType 留空
            }

            return list.Select(t =>
            {
                decimal plan = 0, picked = 0;
                if (sumByTask.TryGetValue(t.Id, out var s))
                {
                    plan = s.Plan;
                    picked = s.Picked;
                }

                var lineDtos = new List<PickingTaskLineDto>();
                if (linesByTask.TryGetValue(t.Id, out var lines))
                {
                    foreach (var i in lines.OrderBy(x => x.CreateTime))
                    {
                        short? lineStockType = null;
                        var sid = i.StockId?.Trim();
                        if (!string.IsNullOrEmpty(sid) && stockById.TryGetValue(sid, out var stRow))
                            lineStockType = stRow.StockType;
                        else if (i.IsStockingSupplement)
                            lineStockType = 2;

                        lineDtos.Add(new PickingTaskLineDto
                        {
                            Id = i.Id,
                            MaterialId = i.MaterialId,
                            StockId = i.StockId,
                            StockType = lineStockType,
                            PlanQty = i.PlanQty,
                            PickedQty = i.PickedQty,
                            IsStockingSupplement = i.IsStockingSupplement
                        });
                    }
                }

                var distinctTypes = lineDtos
                    .Where(x => x.StockType.HasValue)
                    .Select(x => x.StockType!.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

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
                    PickedQtyTotal = picked,
                    DistinctStockTypes = distinctTypes,
                    Items = lineDtos
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
            var existingForSor = (await _pickingTaskRepository.GetAllAsync()).ToList()
                .Where(x => string.Equals(x.StockOutRequestId, sorId, StringComparison.OrdinalIgnoreCase) && x.Status != -1)
                .ToList();
            if (existingForSor.Count > 0)
            {
                var codes = string.Join("、", existingForSor.Select(x => x.TaskCode));
                throw new InvalidOperationException($"该出库通知已存在拣货任务（{codes}），请勿重复生成。");
            }

            var sellLineId = sor.SalesOrderItemId.Trim();

            var taskCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.PickingTask);

            var warehouseId = request.WarehouseId.Trim();
            var task = new PickingTask
            {
                Id = Guid.NewGuid().ToString(),
                TaskCode = taskCode,
                StockOutRequestId = sorId,
                WarehouseId = warehouseId,
                OperatorId = string.IsNullOrWhiteSpace(request.OperatorId) ? "SYSTEM" : request.OperatorId,
                Status = 1,
                CreateTime = DateTime.UtcNow
            };
            await _pickingTaskRepository.AddAsync(task);
            // 仅查本仓库且 AsNoTracking：避免全表 stock 被跟踪后在 SaveChanges 时误更新库存
            var whStocks = (await _stockRepository.FindAsNoTrackingAsync(x => x.WarehouseId == warehouseId)).ToList();
            var remAvail = whStocks.ToDictionary(s => s.Id, s => s.QtyRepertoryAvailable);

            static IEnumerable<StockInfo> OrderFifo(IEnumerable<StockInfo> q) =>
                q.OrderBy(s => s.ProductionDate ?? s.CreateTime).ThenBy(s => s.CreateTime);

            bool PickingStockMatchesSellLine(StockInfo s) =>
                string.Equals(s.SellOrderItemId?.Trim() ?? string.Empty, sellLineId, StringComparison.OrdinalIgnoreCase);

            decimal SumRemainingForSellLine()
            {
                decimal t = 0m;
                foreach (var s in whStocks)
                {
                    if (!PickingStockMatchesSellLine(s))
                        continue;
                    if (remAvail.TryGetValue(s.Id, out var r) && r > 0m)
                        t += r;
                }

                return t;
            }

            foreach (var item in request.Items)
            {
                var need = item.Quantity;
                if (need <= 0m) continue;
                var outboundCode = item.MaterialId?.Trim() ?? "";
                var poolAtLineStart = SumRemainingForSellLine();

                // 与出库通知销售明细一致且本仓库仍有可用量的库存（FIFO）
                foreach (var stock in OrderFifo(whStocks))
                {
                    if (need <= 0m) break;
                    if (!PickingStockMatchesSellLine(stock)) continue;
                    if (!remAvail.TryGetValue(stock.Id, out var av) || av <= 0m) continue;
                    var qty = Math.Min(need, av);
                    if (qty <= 0m) continue;
                    await _pickingTaskItemRepository.AddAsync(new PickingTaskItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        PickingTaskId = task.Id,
                        MaterialId = stock.MaterialId?.Trim() ?? outboundCode,
                        StockId = stock.Id,
                        BatchNo = stock.BatchNo,
                        LocationId = stock.LocationId,
                        PlanQty = qty,
                        PickedQty = 0,
                        IsStockingSupplement = false,
                        CreateTime = DateTime.UtcNow
                    });
                    remAvail[stock.Id] = av - qty;
                    need -= qty;
                }

                if (need > 0m)
                {
                    var wh = await _warehouseRepository.GetByIdAsync(warehouseId);
                    var whLabel = wh != null ? $"{wh.WarehouseName}（{wh.WarehouseCode}）" : warehouseId;
                    throw new InvalidOperationException(
                        $"仓库「{whLabel}」中销售明细「{sellLineId}」本行可拣数量不足（本行开始前可分配 {QuantityMessageFormatting.ForUserMessage(poolAtLineStart)}，需求 {QuantityMessageFormatting.ForUserMessage(item.Quantity)}，仍缺 {QuantityMessageFormatting.ForUserMessage(need)}；物料「{outboundCode}」）。" +
                        " 请入库、更换仓库或调减出库通知数量。");
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
                        var countStockCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.Stock);
                        target = new StockInfo
                        {
                            Id = Guid.NewGuid().ToString(),
                            StockCode = countStockCode,
                            MaterialId = item.MaterialId,
                            WarehouseId = plan.WarehouseId,
                            LocationId = item.LocationId,
                            Status = 1,
                            StockType = 1,
                            RegionType = RegionTypeCode.Domestic,
                            CreateTime = DateTime.UtcNow
                        };
                        await _stockRepository.AddAsync(target);
                    }

                    var countTarget = target!;
                    countTarget.Qty += diffQty;
                    countTarget.QtyRepertory += diffQty;
                    countTarget.QtyRepertoryAvailable += diffQty;
                    countTarget.ModifyTime = DateTime.UtcNow;
                    if (!isNewTarget)
                    {
                        await _stockRepository.UpdateAsync(countTarget);
                    }

                    var countLedger = new InventoryLedger
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
                    };
                    CopyStockOrderLineHeadersFromStockToLedger(countLedger, countTarget);
                    await _ledgerRepository.AddAsync(countLedger);
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


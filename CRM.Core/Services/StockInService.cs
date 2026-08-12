using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.System;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace CRM.Core.Services
{
    /// <summary>
    /// 入库服务实现
    /// </summary>
    public class StockInService : IStockInService
    {
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
        private readonly IRepository<StockInItemExtend> _stockInItemExtendRepository;
        private readonly IRepository<StockInBatch> _stockInBatchRepository;
        private readonly IRepository<StockItem> _stockItemRepository;
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
        private readonly IRepository<PurchaseOrderItem> _purchaseOrderItemRepository;
        private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
        private readonly IRepository<SellOrder> _sellOrderRepository;
        private readonly IRepository<QCInfo> _qcRepository;
        private readonly IRepository<StockInNotify> _stockInNotifyRepository;
        private readonly IRepository<VendorInfo> _vendorRepository;
        private readonly IRepository<WarehouseInfo> _warehouseRepository;
        private readonly IRepository<MaterialInfo> _materialRepository;
        private readonly ILogisticsService _logisticsService;
        private readonly IInventoryCenterService _inventoryCenterService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISerialNumberService _serialNumberService;
        private readonly IUserService _userService;
        private readonly ISellOrderItemExtendSyncService _sellOrderItemExtendSync;
        private readonly ISellOrderItemPurchasedStockAvailableSyncService _purchasedStockAvailableSync;
        private readonly IStockInExtendLineSeqService _stockInLineSeq;
        private readonly ILogOperationAppendService _logOperationAppend;
        private readonly ILogger<StockInService> _logger;
        private readonly IStockInListQuery _stockInListQuery;
        private readonly ICustomsV2FlowService _customsV2FlowService;
        private readonly IStockInCustomsContextQuery _stockInCustomsContextQuery;
        private readonly ICustomsTraceQuery _customsTraceQuery;

        public StockInService(
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
            IRepository<StockInItemExtend> stockInItemExtendRepository,
            IRepository<StockInBatch> stockInBatchRepository,
            IRepository<StockItem> stockItemRepository,
            IRepository<PurchaseOrder> purchaseOrderRepository,
            IRepository<PurchaseOrderItem> purchaseOrderItemRepository,
            IRepository<SellOrderItem> sellOrderItemRepository,
            IRepository<SellOrder> sellOrderRepository,
            IRepository<QCInfo> qcRepository,
            IRepository<StockInNotify> stockInNotifyRepository,
            IRepository<VendorInfo> vendorRepository,
            IRepository<WarehouseInfo> warehouseRepository,
            IRepository<MaterialInfo> materialRepository,
            ILogisticsService logisticsService,
            IInventoryCenterService inventoryCenterService,
            ISerialNumberService serialNumberService,
            IUserService userService,
            ISellOrderItemExtendSyncService sellOrderItemExtendSync,
            ISellOrderItemPurchasedStockAvailableSyncService purchasedStockAvailableSync,
            IStockInExtendLineSeqService stockInLineSeq,
            IUnitOfWork unitOfWork,
            ILogOperationAppendService logOperationAppend,
            ILogger<StockInService> logger,
            IStockInListQuery stockInListQuery,
            ICustomsV2FlowService customsV2FlowService,
            IStockInCustomsContextQuery stockInCustomsContextQuery,
            ICustomsTraceQuery customsTraceQuery)
        {
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
            _stockInItemExtendRepository = stockInItemExtendRepository;
            _stockInBatchRepository = stockInBatchRepository;
            _stockItemRepository = stockItemRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _purchaseOrderItemRepository = purchaseOrderItemRepository;
            _sellOrderItemRepository = sellOrderItemRepository;
            _sellOrderRepository = sellOrderRepository;
            _qcRepository = qcRepository;
            _stockInNotifyRepository = stockInNotifyRepository;
            _vendorRepository = vendorRepository;
            _warehouseRepository = warehouseRepository;
            _materialRepository = materialRepository;
            _logisticsService = logisticsService;
            _inventoryCenterService = inventoryCenterService;
            _serialNumberService = serialNumberService;
            _userService = userService;
            _sellOrderItemExtendSync = sellOrderItemExtendSync;
            _purchasedStockAvailableSync = purchasedStockAvailableSync;
            _stockInLineSeq = stockInLineSeq;
            _unitOfWork = unitOfWork;
            _logOperationAppend = logOperationAppend;
            _logger = logger;
            _stockInListQuery = stockInListQuery;
            _customsV2FlowService = customsV2FlowService;
            _stockInCustomsContextQuery = stockInCustomsContextQuery;
            _customsTraceQuery = customsTraceQuery;
        }

        public async Task<StockIn> CreateAsync(CreateStockInRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            var stockInCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockIn);

            var stockInId = Guid.NewGuid().ToString();

            var (arrivalId, arrivalCode, qcIdForSi, qcCodeForSi) = await ResolveStockInNotifyAndQcFieldsAsync(request);
            StockInNotify? notifyForCreate = null;
            if (!string.IsNullOrWhiteSpace(arrivalId))
                notifyForCreate = await _stockInNotifyRepository.GetByIdAsync(arrivalId.Trim());

            var purchaseOrderId = request.PurchaseOrderId?.Trim();
            if (string.IsNullOrWhiteSpace(purchaseOrderId) &&
                !string.IsNullOrWhiteSpace(notifyForCreate?.PurchaseOrderId))
                purchaseOrderId = notifyForCreate!.PurchaseOrderId.Trim();

            List<PurchaseOrderItem>? poLinesForHeader = null;
            IReadOnlyDictionary<string, PurchaseOrderItem>? poLineById = null;
            if (!string.IsNullOrWhiteSpace(purchaseOrderId))
            {
                poLinesForHeader = (await _purchaseOrderItemRepository.FindAsync(x => x.PurchaseOrderId == purchaseOrderId))
                    .ToList();
                poLineById = poLinesForHeader
                    .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                    .ToDictionary(x => x.Id!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            }
            var regionTypeForCreate = await ResolveStockInRegionTypeForCreateAsync(request);
            var stockInTypeForCreate = await ResolveStockInTypeForCreateAsync(request);

            var stockIn = new StockIn
            {
                Id = stockInId,
                StockInCode = stockInCode,
                StockInType = stockInTypeForCreate,
                RegionType = regionTypeForCreate,
                SourceId = arrivalId,
                SourceCode = arrivalCode,
                QcId = qcIdForSi,
                QcCode = qcCodeForSi,
                WarehouseId = request.WarehouseId,
                VendorId = request.VendorId,
                StockInDate = PostgreSqlDateTime.ToUtc(request.StockInDate),
                TotalQuantity = InventoryQuantity.RoundFromDecimal(request.TotalQuantity),
                Remark = request.Remark,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow,
                CreatedBy = string.IsNullOrWhiteSpace(request.OperatorId) ? null : request.OperatorId.Trim(),
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId)
            };

            if (string.IsNullOrWhiteSpace(stockIn.VendorId) &&
                !string.IsNullOrWhiteSpace(notifyForCreate?.VendorId))
                stockIn.VendorId = notifyForCreate!.VendorId.Trim();

            await _stockInRepository.AddAsync(stockIn);
            // 先落主单，避免后续插入明细时触发 FK(stockinitem.stockinid -> stockin.stockinid) 失败
            await _unitOfWork.SaveChangesAsync();

            if (request.Items != null && request.Items.Count > 0)
            {
                var firstLineSeq = await _stockInLineSeq.ReserveNextSequenceBlockAsync(stockInId, request.Items.Count);
                decimal totalAmount = 0;
                var lineIndex = 0;
                foreach (var item in request.Items)
                {
                    var materialKey = item.MaterialCode?.Trim() ?? string.Empty;
                    PurchaseOrderItem? poiForLine = null;
                    if (poLineById != null && !string.IsNullOrWhiteSpace(materialKey) &&
                        poLineById.TryGetValue(materialKey, out var poiHit))
                        poiForLine = poiHit;

                    if (poiForLine == null && notifyForCreate != null &&
                        !string.IsNullOrWhiteSpace(notifyForCreate.PurchaseOrderItemId))
                    {
                        var notifyPoItemId = notifyForCreate.PurchaseOrderItemId.Trim();
                        if (string.IsNullOrEmpty(materialKey) ||
                            string.Equals(materialKey, notifyPoItemId, StringComparison.OrdinalIgnoreCase))
                        {
                            if (poLineById != null)
                                poLineById.TryGetValue(notifyPoItemId, out poiForLine);
                            else
                                poiForLine = await _purchaseOrderItemRepository.GetByIdAsync(notifyPoItemId);
                        }
                    }

                    var price = item.UnitPrice ?? 0m;
                    // 质检生成入库等场景前端可能传 0 价：按采购明细 Id（与 MaterialId 一致）回填采购单价
                    if (price == 0m && poiForLine != null)
                        price = poiForLine.Cost;
                    if (price == 0m && poiForLine == null && notifyForCreate != null &&
                        IsCustomsArrivalNotify(notifyForCreate) && notifyForCreate.Cost > 0m)
                        price = notifyForCreate.Cost;

                    var qtyInt = InventoryQuantity.RoundFromDecimal(item.Quantity);
                    var amount = qtyInt * price;
                    totalAmount += amount;

                    var line = new StockInItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockInId = stockInId,
                        StockInItemCode = OrderLineItemCodes.StockIn(stockInCode, firstLineSeq + lineIndex),
                        MaterialId = materialKey,
                        Quantity = qtyInt,
                        OrderQty = qtyInt,
                        QtyReceived = qtyInt,
                        Price = price,
                        Amount = amount,
                        BatchNo = item.BatchNo?.Trim(),
                        LocationId = !string.IsNullOrWhiteSpace(item.WarehouseLocation) ? item.WarehouseLocation.Trim() : null,
                        CreateTime = DateTime.UtcNow
                    };
                    lineIndex++;
                    ApplyPurchaseSnapshotToStockInItem(line, poiForLine);
                    if (poiForLine == null && notifyForCreate != null && IsCustomsArrivalNotify(notifyForCreate))
                        ApplyCustomsArrivalNotifySnapshotToStockInItem(line, notifyForCreate);
                    await _stockInItemRepository.AddAsync(line);

                    string? sellCodeForExt = null;
                    string? sellIdForExt = null;
                    if (poiForLine != null && !string.IsNullOrWhiteSpace(poiForLine.SellOrderItemId))
                    {
                        sellIdForExt = poiForLine.SellOrderItemId.Trim();
                        var soLine = await _sellOrderItemRepository.GetByIdAsync(sellIdForExt);
                        if (!string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode))
                            sellCodeForExt = soLine!.SellOrderItemCode.Trim();
                    }
                    else if (notifyForCreate != null && IsCustomsArrivalNotify(notifyForCreate) &&
                             !string.IsNullOrWhiteSpace(notifyForCreate.SellOrderItemId))
                    {
                        sellIdForExt = notifyForCreate.SellOrderItemId.Trim();
                        var soLine = await _sellOrderItemRepository.GetByIdAsync(sellIdForExt);
                        if (!string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode))
                            sellCodeForExt = soLine!.SellOrderItemCode.Trim();
                    }

                    await _stockInItemExtendRepository.AddAsync(new StockInItemExtend
                    {
                        Id = line.Id,
                        StockInId = stockInId,
                        PurchaseOrderItemId = string.IsNullOrWhiteSpace(poiForLine?.Id) ? null : poiForLine!.Id.Trim(),
                        PurchaseOrderItemCode = string.IsNullOrWhiteSpace(poiForLine?.PurchaseOrderItemCode)
                            ? null
                            : poiForLine!.PurchaseOrderItemCode.Trim(),
                        SellOrderItemId = sellIdForExt,
                        SellOrderItemCode = sellCodeForExt,
                        InvoiceMatchDone = 0m,
                        InvoiceMatchToBe = amount,
                        InvoiceMatchStatus = 0,
                        InvoiceMatchCurrency = line.Currency is short c && c is > 0 and <= 3 ? (byte)c : null,
                        CreateTime = line.CreateTime
                    });
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

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                return null;

            var lines = (await _stockInItemRepository.FindAsync(x => x.StockInId == stockIn.Id))
                .OrderBy(x => x.CreateTime)
                .ThenBy(x => x.Id)
                .ToList();
            var extendRows = (await _stockInItemExtendRepository.FindAsync(x => x.StockInId == stockIn.Id)).ToList();
            var primaryExt = StockInItemExtendPrimaryPicker.PickPrimary(extendRows);
            var extByLine = extendRows.ToDictionary(e => e.Id, e => e, StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                if (extByLine.TryGetValue(line.Id, out var er))
                    line.Extend = er;
            }

            stockIn.Items = lines;

            if (!string.IsNullOrWhiteSpace(stockIn.WarehouseId))
            {
                var wh = await _warehouseRepository.GetByIdAsync(stockIn.WarehouseId.Trim());
                if (wh != null)
                {
                    stockIn.DetailWarehouseCode = string.IsNullOrWhiteSpace(wh.WarehouseCode)
                        ? null
                        : wh.WarehouseCode.Trim();
                    stockIn.DetailWarehouseName = string.IsNullOrWhiteSpace(wh.WarehouseName)
                        ? null
                        : wh.WarehouseName.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(stockIn.VendorId))
            {
                var v = await _vendorRepository.GetByIdAsync(stockIn.VendorId.Trim());
                if (v != null)
                {
                    stockIn.DetailVendorName = !string.IsNullOrWhiteSpace(v.OfficialName)
                        ? v.OfficialName.Trim()
                        : !string.IsNullOrWhiteSpace(v.NickName)
                            ? v.NickName.Trim()
                            : v.Code?.Trim();
                }
            }

            IReadOnlyList<PurchaseOrderItem>? poLinesForDetail = null;
            if (primaryExt != null && !string.IsNullOrWhiteSpace(primaryExt.PurchaseOrderItemId))
            {
                var pl0 = await _purchaseOrderItemRepository.GetByIdAsync(primaryExt.PurchaseOrderItemId.Trim());
                if (pl0 != null && !string.IsNullOrWhiteSpace(pl0.PurchaseOrderId))
                {
                    var poId = pl0.PurchaseOrderId.Trim();
                    poLinesForDetail = (await _purchaseOrderItemRepository.FindAsync(x => x.PurchaseOrderId == poId))
                        .ToList();
                }
            }

            // 头表未带来采购行时：明细 MaterialId 常为「采购明细行 Id」，据此反查整单采购行供型号/品牌解析
            if (poLinesForDetail == null && lines.Count > 0)
            {
                foreach (var line in lines)
                {
                    var lid = line.MaterialId?.Trim();
                    if (string.IsNullOrEmpty(lid)) continue;
                    var plTry = await _purchaseOrderItemRepository.GetByIdAsync(lid);
                    if (plTry != null && !string.IsNullOrWhiteSpace(plTry.PurchaseOrderId))
                    {
                        var poId = plTry.PurchaseOrderId.Trim();
                        poLinesForDetail = (await _purchaseOrderItemRepository.FindAsync(x => x.PurchaseOrderId == poId))
                            .ToList();
                        break;
                    }
                }
            }

            var midList = lines
                .Select(x => x.MaterialId?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (midList.Count > 0)
            {
                try
                {
                    var materials = (await _materialRepository.FindAsync(x => midList.Contains(x.Id))).ToList();
                    var matById = new Dictionary<string, MaterialInfo>(StringComparer.OrdinalIgnoreCase);
                    foreach (var m in materials)
                    {
                        var mid = m.Id?.Trim();
                        if (string.IsNullOrEmpty(mid)) continue;
                        if (!matById.ContainsKey(mid))
                            matById[mid] = m;
                    }

                    foreach (var line in lines)
                    {
                        var mid = line.MaterialId?.Trim();
                        if (string.IsNullOrEmpty(mid))
                            continue;

                        // 仅当 MaterialId 实为物料主键时能命中；否则仍可由下方 Resolve + 采购行补全型号/品牌
                        if (matById.TryGetValue(mid, out var mat))
                        {
                            line.DetailMaterialCode = string.IsNullOrWhiteSpace(mat.MaterialCode)
                                ? null
                                : mat.MaterialCode.Trim();
                            line.DetailMaterialName = string.IsNullOrWhiteSpace(mat.MaterialName)
                                ? null
                                : mat.MaterialName.Trim();
                            line.DetailUnit = string.IsNullOrWhiteSpace(mat.Unit) ? null : mat.Unit.Trim();
                        }

                        ResolveStockInLineModelBrand(mid, matById, poLinesForDetail, out var modelDisp, out var brandDisp);
                        line.DetailMaterialModel = modelDisp;
                        line.DetailMaterialBrand = brandDisp;
                        if (string.IsNullOrWhiteSpace(line.DetailMaterialModel) &&
                            !string.IsNullOrWhiteSpace(line.PurchasePn))
                            line.DetailMaterialModel = line.PurchasePn.Trim();
                        if (string.IsNullOrWhiteSpace(line.DetailMaterialBrand) &&
                            !string.IsNullOrWhiteSpace(line.PurchaseBrand))
                            line.DetailMaterialBrand = line.PurchaseBrand.Trim();
                    }
                }
                catch
                {
                    // 物料表不可用时仍返回明细，仅不填充编码/名称
                }
            }

            var poItemById = new Dictionary<string, PurchaseOrderItem>(StringComparer.OrdinalIgnoreCase);
            if (poLinesForDetail != null)
            {
                foreach (var pl in poLinesForDetail)
                {
                    var pid = pl.Id?.Trim();
                    if (string.IsNullOrEmpty(pid) || poItemById.ContainsKey(pid))
                        continue;
                    poItemById[pid] = pl;
                }
            }

            foreach (var er in extendRows)
            {
                var pid = er.PurchaseOrderItemId?.Trim();
                if (string.IsNullOrEmpty(pid) || poItemById.ContainsKey(pid))
                    continue;
                var pl = await _purchaseOrderItemRepository.GetByIdAsync(pid);
                if (pl != null)
                    poItemById[pid] = pl;
            }

            StockInNotify? arrivalNotify = null;
            if (!string.IsNullOrWhiteSpace(stockIn.SourceId))
                arrivalNotify = await _stockInNotifyRepository.GetByIdAsync(stockIn.SourceId.Trim());

            var postedStockItems = (await _stockItemRepository.FindAsync(x => x.StockInId == stockIn.Id)).ToList();
            var stockItemPoCodeByLineId = postedStockItems
                .Where(x => !string.IsNullOrWhiteSpace(x.StockInItemId) &&
                            !string.IsNullOrWhiteSpace(x.PurchaseOrderItemCode))
                .GroupBy(x => x.StockInItemId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().PurchaseOrderItemCode!.Trim(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                line.DetailStockInDate = stockIn.StockInDate;
                line.DetailSourceCode = string.IsNullOrWhiteSpace(stockIn.SourceCode)
                    ? null
                    : stockIn.SourceCode.Trim();
                line.DetailVendorName = stockIn.DetailVendorName;
                line.DetailWarehouseCode = stockIn.DetailWarehouseCode;
                line.DetailRegionType = stockIn.RegionType;
                line.DetailStockInType = stockIn.StockInType;

                var ext = line.Extend;
                var (poItemCode, poiForCurrency) = await ResolveStockInLinePurchaseOrderItemAsync(
                    line,
                    ext,
                    poItemById,
                    arrivalNotify,
                    stockItemPoCodeByLineId);

                line.DetailPurchaseOrderItemCode = poItemCode;
                line.DetailCurrency = ResolveStockInLineCurrency(line, poiForCurrency);
            }

            if (stockIn.StockInType == StockInTypeCode.Customs)
                stockIn.CustomsContext = await _stockInCustomsContextQuery.LoadAsync(stockIn);

            await PopulateStockInCreateUserNameAsync(stockIn);

            return stockIn;
        }

        private async Task PopulateStockInCreateUserNameAsync(StockIn stockIn)
        {
            var creatorId = !string.IsNullOrWhiteSpace(stockIn.CreateByUserId)
                ? stockIn.CreateByUserId.Trim()
                : stockIn.CreatedBy?.Trim();
            if (string.IsNullOrWhiteSpace(creatorId))
                return;

            try
            {
                var u = await _userService.GetByIdAsync(creatorId);
                stockIn.CreateUserName = EntityLookupService.FormatUserLoginName(u);
            }
            catch
            {
                // 用户表不可用时仍返回详情，仅不填充创建人账号
            }
        }

        /// <inheritdoc />
        public Task<bool> CanUserAccessStockInAsync(
            string? userId,
            string stockInId,
            CancellationToken cancellationToken = default)
            => _stockInListQuery.IsVisibleToUserAsync(userId, stockInId, cancellationToken);

        /// <summary>
        /// 解析入库明细对应的采购订单明细编号：扩展表 → 物料/采购行 Id → 库存层冗余 → 到货通知 → 同单 PN 匹配。
        /// </summary>
        private async Task<(string? PurchaseOrderItemCode, PurchaseOrderItem? PoItem)> ResolveStockInLinePurchaseOrderItemAsync(
            StockInItem line,
            StockInItemExtend? ext,
            Dictionary<string, PurchaseOrderItem> poItemById,
            StockInNotify? arrivalNotify,
            IReadOnlyDictionary<string, string> stockItemPoCodeByLineId)
        {
            if (!string.IsNullOrWhiteSpace(ext?.PurchaseOrderItemCode))
                return (ext!.PurchaseOrderItemCode.Trim(), await TryGetPoItemAsync(ext.PurchaseOrderItemId, poItemById));

            PurchaseOrderItem? poi = null;
            if (ext != null && !string.IsNullOrWhiteSpace(ext.PurchaseOrderItemId))
                poi = await TryGetPoItemAsync(ext.PurchaseOrderItemId, poItemById);

            if (poi == null)
            {
                var mid = line.MaterialId?.Trim();
                if (!string.IsNullOrEmpty(mid))
                    poi = await TryGetPoItemAsync(mid, poItemById);
            }

            if (!string.IsNullOrWhiteSpace(poi?.PurchaseOrderItemCode))
                return (poi!.PurchaseOrderItemCode.Trim(), poi);

            var lineId = line.Id?.Trim();
            if (!string.IsNullOrEmpty(lineId) &&
                stockItemPoCodeByLineId.TryGetValue(lineId, out var fromStockItem) &&
                !string.IsNullOrWhiteSpace(fromStockItem))
                return (fromStockItem.Trim(), poi);

            if (arrivalNotify != null && !string.IsNullOrWhiteSpace(arrivalNotify.PurchaseOrderItemId))
            {
                poi = await TryGetPoItemAsync(arrivalNotify.PurchaseOrderItemId, poItemById);
                if (!string.IsNullOrWhiteSpace(poi?.PurchaseOrderItemCode))
                    return (poi!.PurchaseOrderItemCode.Trim(), poi);
            }

            var poId = arrivalNotify?.PurchaseOrderId?.Trim();
            if (string.IsNullOrEmpty(poId) && poi != null)
                poId = poi.PurchaseOrderId?.Trim();

            if (!string.IsNullOrEmpty(poId))
            {
                var pn = (line.DetailMaterialModel ?? line.PurchasePn)?.Trim();
                if (!string.IsNullOrEmpty(pn))
                {
                    var match = await FindPurchaseOrderItemByPnOnOrderAsync(poId, pn, poItemById);
                    if (!string.IsNullOrWhiteSpace(match?.PurchaseOrderItemCode))
                        return (match!.PurchaseOrderItemCode.Trim(), match);
                }
            }

            return (null, poi);
        }

        private async Task<PurchaseOrderItem?> TryGetPoItemAsync(
            string? purchaseOrderItemId,
            Dictionary<string, PurchaseOrderItem> poItemById)
        {
            var pid = purchaseOrderItemId?.Trim();
            if (string.IsNullOrEmpty(pid))
                return null;
            if (poItemById.TryGetValue(pid, out var cached))
                return cached;
            var pl = await _purchaseOrderItemRepository.GetByIdAsync(pid);
            if (pl != null)
                poItemById[pid] = pl;
            return pl;
        }

        private async Task<PurchaseOrderItem?> FindPurchaseOrderItemByPnOnOrderAsync(
            string purchaseOrderId,
            string pn,
            Dictionary<string, PurchaseOrderItem> poItemById)
        {
            var poId = purchaseOrderId.Trim();
            var pnNorm = pn.Trim();
            var cachedOnOrder = poItemById.Values
                .FirstOrDefault(x => string.Equals(x.PurchaseOrderId?.Trim(), poId, StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(x.PN?.Trim(), pnNorm, StringComparison.OrdinalIgnoreCase));
            if (cachedOnOrder != null)
                return cachedOnOrder;

            var found = (await _purchaseOrderItemRepository.FindAsync(x => x.PurchaseOrderId == poId)).ToList();
            foreach (var pl in found)
            {
                if (!string.IsNullOrWhiteSpace(pl.Id))
                    poItemById[pl.Id.Trim()] = pl;
            }

            return found.FirstOrDefault(x =>
                string.Equals(x.PN?.Trim(), pnNorm, StringComparison.OrdinalIgnoreCase));
        }

        private static short ResolveStockInLineCurrency(StockInItem line, PurchaseOrderItem? poItem)
        {
            if (line.Currency is > 0)
                return line.Currency.Value;
            if (poItem != null && poItem.Currency > 0)
                return poItem.Currency;
            return 1;
        }

        private List<StockInListItemDto> ApplyStockInMemoryFilters(
            List<StockInListItemDto> result,
            StockInQueryRequest? request,
            Dictionary<string, List<StockInItem>> stockInItemsMap)
        {
            var modelKeyword = request?.Model?.Trim();
            var vendorKeyword = request?.VendorName?.Trim();
            var poCodeKeyword = request?.PurchaseOrderCode?.Trim();
            var soCodeKeyword = request?.SalesOrderCode?.Trim();
            var stockInCodeKeyword = request?.StockInCode?.Trim();
            var sourceDisplayKeyword = request?.SourceDisplayNo?.Trim();
            var warehouseId = request?.WarehouseId?.Trim();
            var stockInDateStart = request?.StockInDateStart?.Date;
            var stockInDateEnd = request?.StockInDateEnd?.Date;
            var remarkKeyword = request?.Remark?.Trim();

            return result.Where(x =>
            {
                if (!string.IsNullOrWhiteSpace(modelKeyword))
                {
                    var idHit = stockInItemsMap.TryGetValue(x.Id, out var items)
                        && items.Any(i => !string.IsNullOrWhiteSpace(i.MaterialId)
                                          && i.MaterialId.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase));
                    var textHit =
                        (x.MaterialModelSummary?.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (x.MaterialBrandSummary?.Contains(modelKeyword, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (!idHit && !textHit) return false;
                }
                if (!string.IsNullOrWhiteSpace(vendorKeyword)
                    && !(x.VendorName?.Contains(vendorKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(poCodeKeyword)
                    && !((x.PurchaseOrderCode?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false)
                         || (x.SourceDisplayNo?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false)))
                    return false;
                if (!string.IsNullOrWhiteSpace(soCodeKeyword)
                    && !(x.SalesOrderCode?.Contains(soCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(stockInCodeKeyword)
                    && !(x.StockInCode?.Contains(stockInCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(sourceDisplayKeyword)
                    && !(x.SourceDisplayNo?.Contains(sourceDisplayKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(warehouseId)
                    && !string.Equals(x.WarehouseId?.Trim(), warehouseId, StringComparison.OrdinalIgnoreCase))
                    return false;
                var stockInDate = x.StockInDate.Date;
                if (stockInDateStart.HasValue && stockInDate < stockInDateStart.Value)
                    return false;
                if (stockInDateEnd.HasValue && stockInDate > stockInDateEnd.Value)
                    return false;
                if (!string.IsNullOrWhiteSpace(remarkKeyword)
                    && !(x.Remark?.Contains(remarkKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                return true;
            }).ToList();
        }

        private async Task<(List<StockInListItemDto> Dtos, Dictionary<string, List<StockInItem>> ItemsMap)> BuildStockInListDtosCoreAsync(List<StockIn> raw)
        {
            if (raw.Count == 0)
                return (new List<StockInListItemDto>(), new Dictionary<string, List<StockInItem>>(StringComparer.Ordinal));

            var stockInIds = raw.Select(x => x.Id).ToList();
            var vendorIds = raw.Select(x => x.VendorId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var stockInItems = (await _stockInItemRepository.FindAsync(x => stockInIds.Contains(x.StockInId))).ToList();
            var stockInItemsMap = stockInItems
                .GroupBy(x => x.StockInId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var allItemIds = stockInItems
                .Select(i => i.Id?.Trim())
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var stockInIdByItemId = stockInItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Id) && !string.IsNullOrWhiteSpace(i.StockInId))
                .ToDictionary(i => i.Id.Trim(), i => i.StockInId.Trim(), StringComparer.OrdinalIgnoreCase);
            var stockInIdsWithBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allItemIds.Count > 0)
            {
                var batchRows = (await _stockInBatchRepository.FindAsync(b => allItemIds.Contains(b.StockInItemId))).ToList();
                foreach (var b in batchRows)
                {
                    var itemKey = (b.StockInItemId ?? string.Empty).Trim();
                    if (stockInIdByItemId.TryGetValue(itemKey, out var sid))
                        stockInIdsWithBatch.Add(sid);
                }
            }

            var allExtends = (await _stockInItemExtendRepository.FindAsync(e => stockInIds.Contains(e.StockInId))).ToList();
            var primaryByStockIn = StockInItemExtendPrimaryPicker.PrimaryByStockInId(allExtends);

            var poLineIds = allExtends
                .Select(x => x.PurchaseOrderItemId)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var poItemsForStockIns = poLineIds.Count == 0
                ? new List<PurchaseOrderItem>()
                : (await _purchaseOrderItemRepository.FindAsync(x => poLineIds.Contains(x.Id))).ToList();
            var poLineEntityById = poItemsForStockIns
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .ToDictionary(x => x.Id!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            var sellOnlyIds = allExtends
                .Where(e => string.IsNullOrWhiteSpace(e.PurchaseOrderItemId) && !string.IsNullOrWhiteSpace(e.SellOrderItemId))
                .Select(e => e.SellOrderItemId!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (sellOnlyIds.Count > 0)
            {
                var extraPoLines = (await _purchaseOrderItemRepository.FindAsync(x => x.SellOrderItemId != null && sellOnlyIds.Contains(x.SellOrderItemId)))
                    .ToList();
                foreach (var pl in extraPoLines)
                {
                    if (string.IsNullOrWhiteSpace(pl.Id)) continue;
                    var k = pl.Id.Trim();
                    if (!poLineEntityById.ContainsKey(k))
                        poLineEntityById[k] = pl;
                }
            }

            var poIdsForDisplay = poLineEntityById.Values
                .Select(v => v.PurchaseOrderId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var poDict = poIdsForDisplay.Count == 0
                ? new Dictionary<string, PurchaseOrder>(StringComparer.OrdinalIgnoreCase)
                : (await _purchaseOrderRepository.FindAsync(p => poIdsForDisplay.Contains(p.Id)))
                    .ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

            var poItemsMap = poIdsForDisplay.Count == 0
                ? new Dictionary<string, List<PurchaseOrderItem>>(StringComparer.OrdinalIgnoreCase)
                : (await _purchaseOrderItemRepository.FindAsync(x => poIdsForDisplay.Contains(x.PurchaseOrderId)))
                    .GroupBy(x => x.PurchaseOrderId, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            Dictionary<string, QCInfo> qcByStockInId;
            try
            {
                qcByStockInId = (await _qcRepository.FindAsync(q => q.StockInId != null && stockInIds.Contains(q.StockInId!)))
                    .GroupBy(q => q.StockInId!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            }
            catch (Exception ex) when (PostgreSqlExceptionHelper.IsUndefinedObject(ex))
            {
                _logger.LogWarning(
                    ex,
                    "质检主表批量查询失败（多为 qcinfo 缺少 StockInPlanDate 列，请执行迁移 20260623100000_QcInfoStockInPlanDate 或 scripts/add_qc_stock_in_plan_date_postgresql.sql）；入库单列表将不展示关联质检信息。");
                qcByStockInId = new Dictionary<string, QCInfo>(StringComparer.Ordinal);
            }

            var customsNotifyIds = raw
                .Where(s => s.StockInType == StockInTypeCode.Customs)
                .SelectMany(s => new[] { s.SourceId, qcByStockInId.TryGetValue(s.Id, out var q0) ? q0.StockInNotifyId : null })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var customsTraceByNotifyId = customsNotifyIds.Count == 0
                ? new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase)
                : (await _customsTraceQuery.GetByStockInNotifyIdsAsync(customsNotifyIds)).ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value,
                    StringComparer.OrdinalIgnoreCase);
            if (customsTraceByNotifyId.Count > 0)
            {
                var mergedVendorIds = vendorIds.ToList();
                foreach (var trace in customsTraceByNotifyId.Values)
                {
                    if (!string.IsNullOrWhiteSpace(trace.VendorId))
                        mergedVendorIds.Add(trace.VendorId.Trim());
                }
                vendorIds = mergedVendorIds
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var venDict = vendorIds.Count == 0
                ? new Dictionary<string, VendorInfo>()
                : (await _vendorRepository.FindAsync(v => vendorIds.Contains(v.Id))).ToDictionary(v => v.Id);

            Dictionary<string, MaterialInfo> materialById = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var m in await _materialRepository.GetAllAsync())
                {
                    var id = m.Id?.Trim();
                    if (string.IsNullOrEmpty(id) || materialById.ContainsKey(id)) continue;
                    materialById[id] = m;
                }
            }
            catch
            {
                // 物料表不可用时仍返回列表，仅不展示型号/品牌
            }

            var sellOrderItemIds = poItemsMap.Values
                .SelectMany(x => x)
                .Select(x => x.SellOrderItemId)
                .Concat(allExtends.Select(e => e.SellOrderItemId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var soItems = sellOrderItemIds.Count == 0
                ? new List<SellOrderItem>()
                : (await _sellOrderItemRepository.FindAsync(x => sellOrderItemIds.Contains(x.Id))).ToList();
            var soIdByItemId = soItems
                .Where(x => !string.IsNullOrWhiteSpace(x.SellOrderId))
                .ToDictionary(x => x.Id, x => x.SellOrderId!);
            var soIds = soIdByItemId.Values.Distinct().ToList();
            var soDict = soIds.Count == 0
                ? new Dictionary<string, SellOrder>()
                : (await _sellOrderRepository.FindAsync(x => soIds.Contains(x.Id))).ToDictionary(x => x.Id);

            var allUsers = (await _userService.GetAllAsync()).ToList();
            var userById = allUsers
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .ToDictionary(u => u.Id!.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

            var result = new List<StockInListItemDto>(raw.Count);
            foreach (var s in raw)
            {
                var prim = primaryByStockIn.TryGetValue(s.Id, out var pe) ? pe : null;
                PurchaseOrderItem? headerPoLine = null;
                if (!string.IsNullOrWhiteSpace(prim?.PurchaseOrderItemId) &&
                    poLineEntityById.TryGetValue(prim.PurchaseOrderItemId.Trim(), out var plHeader))
                    headerPoLine = plHeader;
                else if (!string.IsNullOrWhiteSpace(prim?.SellOrderItemId))
                {
                    headerPoLine = poLineEntityById.Values.FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(v.SellOrderItemId) &&
                        string.Equals(v.SellOrderItemId.Trim(), prim.SellOrderItemId!.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                var headerPoId = headerPoLine != null && !string.IsNullOrWhiteSpace(headerPoLine.PurchaseOrderId)
                    ? headerPoLine.PurchaseOrderId.Trim()
                    : (string?)null;

                string? sourceDisplay = null;
                if (!string.IsNullOrWhiteSpace(s.SourceCode))
                    sourceDisplay = s.SourceCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && headerPoId != null &&
                    poDict.TryGetValue(headerPoId, out var poForDisp) &&
                    !string.IsNullOrWhiteSpace(poForDisp.PurchaseOrderCode))
                    sourceDisplay = poForDisp.PurchaseOrderCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && qcByStockInId.TryGetValue(s.Id, out var qcLinked))
                    sourceDisplay = qcLinked.QcCode;
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(s.QcCode))
                    sourceDisplay = s.QcCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(prim?.PurchaseOrderItemCode))
                    sourceDisplay = prim.PurchaseOrderItemCode.Trim();

                string? vendorName = null;
                string? vendorEnglishName = null;
                string? vendorCode = null;
                var vendorIdForLookup = s.VendorId;
                if (s.StockInType == StockInTypeCode.Customs && string.IsNullOrWhiteSpace(vendorIdForLookup))
                {
                    var notifyKey = !string.IsNullOrWhiteSpace(s.SourceId)
                        ? s.SourceId.Trim()
                        : (qcByStockInId.TryGetValue(s.Id, out var qcNotify) ? qcNotify.StockInNotifyId?.Trim() : null);
                    if (!string.IsNullOrEmpty(notifyKey)
                        && customsTraceByNotifyId.TryGetValue(notifyKey, out var traceVendor)
                        && !string.IsNullOrWhiteSpace(traceVendor.VendorId))
                        vendorIdForLookup = traceVendor.VendorId;
                }
                if (!string.IsNullOrWhiteSpace(vendorIdForLookup) && venDict.TryGetValue(vendorIdForLookup!, out var v))
                {
                    vendorName = !string.IsNullOrWhiteSpace(v.OfficialName) ? v.OfficialName
                        : !string.IsNullOrWhiteSpace(v.NickName) ? v.NickName
                        : v.Code;
                    if (!string.IsNullOrWhiteSpace(v.EnglishOfficialName))
                        vendorEnglishName = v.EnglishOfficialName.Trim();
                    if (!string.IsNullOrWhiteSpace(v.Code))
                        vendorCode = v.Code.Trim();
                }

                var salesOrderCodes = new List<string>();
                if (headerPoLine != null && !string.IsNullOrWhiteSpace(headerPoLine.SellOrderItemId) &&
                    soIdByItemId.TryGetValue(headerPoLine.SellOrderItemId.Trim(), out var soId0) &&
                    soDict.TryGetValue(soId0, out var so0) && !string.IsNullOrWhiteSpace(so0.SellOrderCode))
                    salesOrderCodes.Add(so0.SellOrderCode!);
                if (!string.IsNullOrWhiteSpace(prim?.SellOrderItemId) &&
                    (!headerPoLine?.SellOrderItemId?.Trim().Equals(prim.SellOrderItemId.Trim(), StringComparison.OrdinalIgnoreCase) ?? true) &&
                    soIdByItemId.TryGetValue(prim.SellOrderItemId.Trim(), out var soId1) &&
                    soDict.TryGetValue(soId1, out var so1) && !string.IsNullOrWhiteSpace(so1.SellOrderCode))
                    salesOrderCodes.Add(so1.SellOrderCode!);

                if (headerPoId != null &&
                    poItemsMap.TryGetValue(headerPoId, out var thisPoItems))
                {
                    foreach (var poi in thisPoItems)
                    {
                        if (string.IsNullOrWhiteSpace(poi.SellOrderItemId)) continue;
                        if (!soIdByItemId.TryGetValue(poi.SellOrderItemId.Trim(), out var soId)) continue;
                        if (!soDict.TryGetValue(soId, out var so)) continue;
                        if (string.IsNullOrWhiteSpace(so.SellOrderCode)) continue;
                        salesOrderCodes.Add(so.SellOrderCode);
                    }
                }

                var salesOrderCode = string.Join(", ", salesOrderCodes.Distinct(StringComparer.OrdinalIgnoreCase));

                string? purchaseOrderCode = null;
                if (headerPoId != null &&
                    poDict.TryGetValue(headerPoId, out var poForPoCode) &&
                    !string.IsNullOrWhiteSpace(poForPoCode.PurchaseOrderCode))
                    purchaseOrderCode = poForPoCode.PurchaseOrderCode.Trim();

                string? freightForwarderOrderNo = null;
                if (headerPoId != null &&
                    poDict.TryGetValue(headerPoId, out var poForFf))
                    freightForwarderOrderNo = PurchaseOrderFreightForwarderOrderNoRules.Normalize(poForFf.FreightForwarderOrderNo);

                string? modelSummary = null;
                string? brandSummary = null;
                if (stockInItemsMap.TryGetValue(s.Id, out var silForDisplay) && silForDisplay.Count > 0)
                {
                    IReadOnlyList<PurchaseOrderItem>? poLinesForS = null;
                    if (headerPoId != null &&
                        poItemsMap.TryGetValue(headerPoId, out var pl0))
                        poLinesForS = pl0;

                    var models = new List<string>();
                    var brands = new List<string>();
                    foreach (var line in silForDisplay)
                    {
                        var mid = line.MaterialId?.Trim();
                        if (string.IsNullOrEmpty(mid)) continue;
                        ResolveStockInLineModelBrand(mid, materialById, poLinesForS, out var m1, out var b1);
                        if (!string.IsNullOrWhiteSpace(m1)) models.Add(m1);
                        if (!string.IsNullOrWhiteSpace(b1)) brands.Add(b1);
                    }

                    if (models.Count > 0)
                        modelSummary = string.Join(", ", models.Distinct(StringComparer.OrdinalIgnoreCase));
                    if (brands.Count > 0)
                        brandSummary = string.Join(", ", brands.Distinct(StringComparer.OrdinalIgnoreCase));
                }

                decimal displayTotalAmount = s.TotalAmount;
                if (displayTotalAmount == 0m && stockInItemsMap.TryGetValue(s.Id, out var silAmt) && silAmt.Count > 0)
                {
                    displayTotalAmount = silAmt.Sum(line =>
                        line.Amount != 0m ? line.Amount : line.Quantity * line.Price);
                    if (displayTotalAmount == 0m && headerPoId != null &&
                        poItemsMap.TryGetValue(headerPoId, out var poLinesForAmt) &&
                        poLinesForAmt.Count > 0)
                    {
                        var poById = poLinesForAmt
                            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
                            .ToDictionary(p => p.Id!.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
                        displayTotalAmount = silAmt.Sum(line =>
                        {
                            var mid = line.MaterialId?.Trim();
                            if (!string.IsNullOrEmpty(mid) && poById.TryGetValue(mid, out var poi))
                                return line.Quantity * poi.Cost;
                            return line.Amount != 0m ? line.Amount : line.Quantity * line.Price;
                        });
                    }
                }

                string? createUserName = null;
                if (!string.IsNullOrWhiteSpace(s.CreatedBy) && userById.TryGetValue(s.CreatedBy.Trim(), out var cu))
                    createUserName = EntityLookupService.FormatUserLoginName(cu) ?? s.CreatedBy.Trim();

                short? currencyCode = null;
                if (headerPoLine != null)
                    currencyCode = headerPoLine.Currency;
                else if (stockInItemsMap.TryGetValue(s.Id, out var silCur) && silCur.Count > 0)
                {
                    foreach (var line in silCur)
                    {
                        var midCur = line.MaterialId?.Trim();
                        if (string.IsNullOrEmpty(midCur)) continue;
                        if (poLineEntityById.TryGetValue(midCur, out var plCur))
                        {
                            currencyCode = plCur.Currency;
                            break;
                        }
                    }
                }

                string? customsDeclarationId = null;
                string? customsDeclarationCode = null;
                if (s.StockInType == StockInTypeCode.Customs)
                {
                    var notifyKey = !string.IsNullOrWhiteSpace(s.SourceId)
                        ? s.SourceId.Trim()
                        : (qcByStockInId.TryGetValue(s.Id, out var qcDec) ? qcDec.StockInNotifyId?.Trim() : null);
                    if (!string.IsNullOrEmpty(notifyKey)
                        && customsTraceByNotifyId.TryGetValue(notifyKey, out var traceDec))
                    {
                        customsDeclarationId = traceDec.CustomsDeclarationId;
                        customsDeclarationCode = traceDec.CustomsDeclarationCode;
                    }
                }

                result.Add(new StockInListItemDto
                {
                    Id = s.Id,
                    StockInCode = s.StockInCode,
                    StockInType = s.StockInType,
                    SourceDisplayNo = sourceDisplay,
                    WarehouseId = s.WarehouseId,
                    VendorId = vendorIdForLookup ?? s.VendorId,
                    VendorName = vendorName,
                    VendorEnglishName = vendorEnglishName,
                    VendorCode = vendorCode,
                    PurchaseOrderCode = purchaseOrderCode,
                    FreightForwarderOrderNo = freightForwarderOrderNo,
                    SalesOrderCode = string.IsNullOrWhiteSpace(salesOrderCode) ? null : salesOrderCode,
                    MaterialModelSummary = modelSummary,
                    MaterialBrandSummary = brandSummary,
                    StockInDate = s.StockInDate,
                    TotalQuantity = s.TotalQuantity,
                    TotalAmount = displayTotalAmount,
                    CurrencyCode = currencyCode,
                    Status = s.Status,
                    Remark = s.Remark,
                    CreateTime = s.CreateTime,
                    CreateUserName = createUserName,
                    HasBatchEntered = stockInIdsWithBatch.Contains(s.Id.Trim()),
                    CustomsDeclarationId = customsDeclarationId,
                    CustomsDeclarationCode = customsDeclarationCode
                });
            }

            return (result, stockInItemsMap);
        }

        public async Task<PagedResult<StockInListItemDto>> GetListPagedAsync(
            StockInQueryRequest? request,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pagedIds = await _stockInListQuery.GetPagedStockInIdsAsync(request, page, pageSize, cancellationToken);
            if (pagedIds.TotalCount == 0)
            {
                return new PagedResult<StockInListItemDto>
                {
                    Items = Array.Empty<StockInListItemDto>(),
                    TotalCount = 0,
                    PageIndex = pagedIds.PageIndex,
                    PageSize = pagedIds.PageSize
                };
            }

            var idOrder = pagedIds.Items.ToList();
            var idSet = idOrder.ToHashSet(StringComparer.Ordinal);
            var loaded = (await _stockInRepository.FindAsync(x => idSet.Contains(x.Id))).ToList();
            var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<StockIn>();
            foreach (var id in idOrder)
            {
                var key = id.Trim();
                if (byId.TryGetValue(key, out var ent))
                    ordered.Add(ent);
            }

            var (dtos, _) = await BuildStockInListDtosCoreAsync(ordered);
            return new PagedResult<StockInListItemDto>
            {
                Items = dtos,
                TotalCount = pagedIds.TotalCount,
                PageIndex = pagedIds.PageIndex,
                PageSize = pagedIds.PageSize
            };
        }

        public async Task<List<StockInListItemDto>> GetStockInListItemsByIdsAsync(
            IReadOnlyList<string> ids,
            CancellationToken cancellationToken = default)
        {
            if (ids == null || ids.Count == 0)
                return new List<StockInListItemDto>();

            var idList = ids
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList();
            if (idList.Count == 0)
                return new List<StockInListItemDto>();

            var loaded = (await _stockInRepository.FindAsync(x => idList.Contains(x.Id))).ToList();
            if (loaded.Count == 0)
                return new List<StockInListItemDto>();

            var byId = loaded.ToDictionary(x => x.Id.Trim(), x => x, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<StockIn>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ids)
            {
                var key = id.Trim();
                if (!seen.Add(key))
                    continue;
                if (byId.TryGetValue(key, out var ent))
                    ordered.Add(ent);
            }

            var (dtos, _) = await BuildStockInListDtosCoreAsync(ordered);
            return dtos;
        }

        public async Task<IReadOnlyList<StockInListItemDto>> GetListAsync(StockInQueryRequest? request = null)
        {
            const short transferStockInType = StockInTypeCode.Transfer;
            var raw = (await _stockInRepository.GetAllAsync())
                .Where(x => x.StockInType != transferStockInType)
                .OrderByDescending(x => x.CreateTime)
                .ThenByDescending(x => x.Id)
                .ToList();

            if (raw.Count == 0)
                return Array.Empty<StockInListItemDto>();

            var (dtos, itemsMap) = await BuildStockInListDtosCoreAsync(raw);
            return ApplyStockInMemoryFilters(dtos, request, itemsMap);
        }

        public async Task<StockIn> UpdateAsync(string id, UpdateStockInRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            if (!string.IsNullOrWhiteSpace(request.Remark))
                stockIn.Remark = request.Remark;

            stockIn.ModifyTime = DateTime.UtcNow;
            stockIn.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

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

            await DeleteInternalAsync(stockIn);

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.StockIn,
                RecordId = stockIn.Id,
                RecordCode = stockIn.StockInCode,
                EntityDisplayName = DeleteLogEntityNames.StockIn
            });
        }

        private async Task DeleteInternalAsync(StockIn stockIn)
        {
            var sid = stockIn.Id.Trim();
            var sidLower = sid.ToLowerInvariant();
            var downstreamStockItems = (await _stockItemRepository.FindAsync(x =>
                    x.StockInId != null &&
                    x.StockInId.ToLower() == sidLower))
                .Where(x => !x.IsDeleted)
                .ToList();
            if (downstreamStockItems.Count > 0)
            {
                var stockItemCodes = downstreamStockItems
                    .Select(x => string.IsNullOrWhiteSpace(x.StockItemCode) ? x.Id : x.StockItemCode.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();
                throw new ArgumentException(
                    $"存在下游业务节点：库存明细；下游数据单号：{string.Join("、", stockItemCodes)}");
            }

            var aggregateIdsToRecalc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var items = (await _stockInItemRepository.FindAsync(i => i.StockInId == sid)).ToList();
            foreach (var item in items)
            {
                foreach (var batch in (await _stockInBatchRepository.FindAsync(b => b.StockInItemId == item.Id)).ToList())
                    await _stockInBatchRepository.DeleteAsync(batch.Id);

                foreach (var si in (await _stockItemRepository.FindIgnoreFiltersAsync(x => x.StockInItemId == item.Id)).ToList())
                {
                    var aggId = si.StockAggregateId?.Trim();
                    if (!string.IsNullOrWhiteSpace(aggId))
                        aggregateIdsToRecalc.Add(aggId);
                    if (!si.IsDeleted)
                        await _stockItemRepository.DeleteAsync(si.Id);
                }

                await _stockInItemExtendRepository.DeleteAsync(item.Id);
                await _stockInItemRepository.DeleteAsync(item.Id);
            }

            foreach (var aggId in aggregateIdsToRecalc)
                await _inventoryCenterService.RecalculateStockAggregateTotalsAsync(aggId);

            await _unitOfWork.ExecuteNonQueryAsync(
                $@"UPDATE public.stock_in_extend SET is_deleted = true, ""ModifyTime"" = NOW() WHERE ""StockInId"" = '{EscapeSqlLiteral(sid)}'");

            foreach (var qc in (await _qcRepository.FindAsync(q => q.StockInId == sid)).ToList())
            {
                qc.StockInId = null;
                qc.StockInStatus = 1;
                await _qcRepository.UpdateAsync(qc);
            }

            await _stockInRepository.DeleteAsync(sid);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task ForceDeleteAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName)
        {
            if (string.IsNullOrWhiteSpace(confirmBillCode))
                throw new ArgumentException("请填写 confirmBillCode", nameof(confirmBillCode));
            if (string.IsNullOrWhiteSpace(actingUserId))
                throw new ArgumentException("操作人不能为空", nameof(actingUserId));

            var stockIn = await _stockInRepository.GetByIdAsync(id.Trim())
                ?? throw new InvalidOperationException("入库单不存在");
            if (!string.Equals(confirmBillCode.Trim(), stockIn.StockInCode?.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("确认单号不匹配，已拒绝删除");

            await DeleteInternalAsync(stockIn);

            await _logOperationAppend.AppendDeleteAsync(new DeleteOperationLogEntry
            {
                BizType = BusinessLogTypes.StockIn,
                RecordId = stockIn.Id,
                RecordCode = stockIn.StockInCode,
                EntityDisplayName = DeleteLogEntityNames.StockIn,
                IsForceDelete = true,
                ForceConfirmBillCode = confirmBillCode.Trim(),
                OperatorUserId = actingUserId.Trim(),
                OperatorUserName = actingUserName?.Trim(),
                OperationDescOverride = $"强制删除入库单 StockInId={stockIn.Id}，确认单号={stockIn.StockInCode}"
            });
        }

        private static string EscapeSqlLiteral(string s) => s.Replace("'", "''", StringComparison.Ordinal);

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

            const short transferStockInType = StockInTypeCode.Transfer;
            if (stockIn.StockInType == transferStockInType)
            {
                if (stockIn.Status == status)
                    return;
                throw new InvalidOperationException("调拨类虚拟入库单不可通过常规入库流程变更状态。");
            }

            // 幂等保护：状态未变化时直接返回，避免重复点击导致重复推进回写
            if (stockIn.Status == status)
            {
                _logger.LogInformation(
                    "[InboundStatus2] Skip status update (already target) StockInId={StockInId} Status={Status}",
                    id, status);
                return;
            }

            stockIn.Status = status;
            stockIn.ModifyTime = DateTime.UtcNow;
            stockIn.ModifyByUserId = ActingUserIdNormalizer.Normalize(actingUserId);

            if (status == 2)
            {
                if (stockIn.StockInType == StockInTypeCode.Customs)
                    await _customsV2FlowService.ValidateCustomsStockInFeesAsync(stockIn.Id);

                await SyncStockInRegionTypeFromNotifyAsync(stockIn);
            }

            await _stockInRepository.UpdateAsync(stockIn);
            await _unitOfWork.SaveChangesAsync();

            if (status == 2)
            {
                var logExt = StockInItemExtendPrimaryPicker.PickPrimary(
                    (await _stockInItemExtendRepository.FindAsync(e => e.StockInId == stockIn.Id)).ToList());
                _logger.LogInformation(
                    "[InboundStatus2] StockIn status→2 chain start StockInId={StockInId} StockInCode={Code} PoItemId={PoItemId} PoItemCode={PoItemCode}",
                    stockIn.Id,
                    stockIn.StockInCode ?? "",
                    logExt?.PurchaseOrderItemId ?? "",
                    logExt?.PurchaseOrderItemCode ?? "");

                try
                {
                    _logger.LogInformation("[InboundStatus2] Step=PostStockIn begin StockInId={StockInId}", stockIn.Id);
                    await _inventoryCenterService.PostStockInAsync(stockIn.Id);
                    _logger.LogInformation("[InboundStatus2] Step=PostStockIn ok StockInId={StockInId}", stockIn.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[InboundStatus2] Step=PostStockIn failed StockInId={StockInId}", stockIn.Id);
                    throw;
                }

                if (stockIn.StockInType == StockInTypeCode.Customs)
                {
                    await _customsV2FlowService.OnCustomsStockInCompletedAsync(stockIn.Id, actingUserId);
                }

                string? hookPoId;
                try
                {
                    _logger.LogInformation("[InboundStatus2] Step=ResolveHookPoId begin StockInId={StockInId}", stockIn.Id);
                    hookPoId = await ResolvePurchaseOrderIdForStockInCompletedHookAsync(stockIn);
                    _logger.LogInformation(
                        "[InboundStatus2] Step=ResolveHookPoId ok StockInId={StockInId} HookPoId={HookPoId}",
                        stockIn.Id,
                        hookPoId ?? "(null)");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[InboundStatus2] Step=ResolveHookPoId failed StockInId={StockInId}", stockIn.Id);
                    throw;
                }

                try
                {
                    _logger.LogInformation(
                        "[InboundStatus2] Step=HandleStockInCompleted begin StockInId={StockInId} HookPoId={HookPoId}",
                        stockIn.Id,
                        hookPoId ?? "(null)");
                    await _logisticsService.HandleStockInCompletedAsync(stockIn.Id, hookPoId);
                    _logger.LogInformation("[InboundStatus2] Step=HandleStockInCompleted ok StockInId={StockInId}", stockIn.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[InboundStatus2] Step=HandleStockInCompleted failed StockInId={StockInId}", stockIn.Id);
                    throw;
                }

                await TryRefreshSellOrderItemExtendAfterStockInCompletedAsync(stockIn);
                try
                {
                    await _purchasedStockAvailableSync.TryRecalculateFromCompletedStockInAsync(stockIn);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[PurchasedStockAvail] TryRecalculateFromCompletedStockIn failed StockInId={StockInId}",
                        stockIn.Id);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("[InboundStatus2] StockIn status→2 chain end StockInId={StockInId}", stockIn.Id);
            }
        }

        /// <summary>
        /// 物流回写主要更新采购侧扩展；销售明细「入库进度」依赖 <see cref="SellOrderItemExtendSyncService"/>，且扩展更新需 SaveChanges 才落库。
        /// </summary>
        private async Task TryRefreshSellOrderItemExtendAfterStockInCompletedAsync(StockIn stockIn)
        {
            var extRows = (await _stockInItemExtendRepository.FindAsync(e => e.StockInId == stockIn.Id)).ToList();
            var sellLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in extRows)
            {
                if (!string.IsNullOrWhiteSpace(e.SellOrderItemId))
                    sellLineIds.Add(e.SellOrderItemId.Trim());
                else if (!string.IsNullOrWhiteSpace(e.PurchaseOrderItemId))
                {
                    var pl = await _purchaseOrderItemRepository.GetByIdAsync(e.PurchaseOrderItemId.Trim());
                    if (!string.IsNullOrWhiteSpace(pl?.SellOrderItemId))
                        sellLineIds.Add(pl.SellOrderItemId.Trim());
                }
            }

            foreach (var sid in sellLineIds)
                await _sellOrderItemExtendSync.RecalculateAsync(sid);
        }

        private async Task<string?> ResolvePurchaseOrderIdForStockInCompletedHookAsync(StockIn stockIn)
        {
            var pick = StockInItemExtendPrimaryPicker.PickPrimary(
                (await _stockInItemExtendRepository.FindAsync(e => e.StockInId == stockIn.Id)).ToList());
            if (!string.IsNullOrWhiteSpace(pick?.PurchaseOrderItemId))
            {
                _logger.LogInformation(
                    "[InboundStatus2] ResolveHook branch=PoItemId PoItemId={PoItemId}",
                    pick.PurchaseOrderItemId.Trim());
                var pl = await _purchaseOrderItemRepository.GetByIdAsync(pick.PurchaseOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(pl?.PurchaseOrderId))
                    return pl!.PurchaseOrderId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(pick?.PurchaseOrderItemCode))
            {
                _logger.LogInformation(
                    "[InboundStatus2] ResolveHook branch=PoItemCode PoItemCode={PoItemCode}",
                    pick.PurchaseOrderItemCode.Trim());
                var poId = await _unitOfWork.GetPurchaseOrderIdByPurchaseOrderItemLineCodeAsync(
                    pick.PurchaseOrderItemCode.Trim());
                if (!string.IsNullOrWhiteSpace(poId))
                    return poId.Trim();
            }

            _logger.LogInformation("[InboundStatus2] ResolveHook branch=none (no Po id resolved)");
            return null;
        }

        /// <summary>新建入库单时：<c>RegionType</c> 与关联到货通知一致。</summary>
        private async Task<short> ResolveStockInRegionTypeForCreateAsync(CreateStockInRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.StockInNotifyId))
            {
                var n = await _stockInNotifyRepository.GetByIdAsync(request.StockInNotifyId.Trim());
                if (n != null)
                    return RegionTypeCode.Normalize(n.RegionType);
            }

            if (!string.IsNullOrWhiteSpace(request.QcId))
            {
                var qc = await _qcRepository.GetByIdAsync(request.QcId.Trim());
                if (qc != null && !string.IsNullOrWhiteSpace(qc.StockInNotifyId))
                {
                    var n2 = await _stockInNotifyRepository.GetByIdAsync(qc.StockInNotifyId.Trim());
                    if (n2 != null)
                        return RegionTypeCode.Normalize(n2.RegionType);
                }
            }

            return RegionTypeCode.Domestic;
        }

        /// <summary>新建入库单时：<c>stock_in.StockInType</c> 取自关联到货通知（<see cref="StockInTypeCode"/>）。</summary>
        private async Task<short> ResolveStockInTypeForCreateAsync(CreateStockInRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.StockInNotifyId))
            {
                var n = await _stockInNotifyRepository.GetByIdAsync(request.StockInNotifyId.Trim());
                if (n != null)
                    return StockInTypeCode.NormalizeForNotify(n.StockInType);
            }

            if (!string.IsNullOrWhiteSpace(request.QcId))
            {
                var qc = await _qcRepository.GetByIdAsync(request.QcId.Trim());
                if (qc != null && !string.IsNullOrWhiteSpace(qc.StockInNotifyId))
                {
                    var n = await _stockInNotifyRepository.GetByIdAsync(qc.StockInNotifyId.Trim());
                    if (n != null)
                        return StockInTypeCode.NormalizeForNotify(n.StockInType);
                }
            }

            return StockInTypeCode.Purchase;
        }

        /// <summary>确认入库过账前：按 <c>SourceId</c> / 质检关联通知刷新地域，与通知当前值一致。</summary>
        private async Task SyncStockInRegionTypeFromNotifyAsync(StockIn stockIn)
        {
            if (!string.IsNullOrWhiteSpace(stockIn.SourceId))
            {
                var n = await _stockInNotifyRepository.GetByIdAsync(stockIn.SourceId.Trim());
                if (n != null)
                {
                    stockIn.RegionType = RegionTypeCode.Normalize(n.RegionType);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(stockIn.QcId))
            {
                var qc = await _qcRepository.GetByIdAsync(stockIn.QcId.Trim());
                if (qc != null && !string.IsNullOrWhiteSpace(qc.StockInNotifyId))
                {
                    var n2 = await _stockInNotifyRepository.GetByIdAsync(qc.StockInNotifyId.Trim());
                    if (n2 != null)
                        stockIn.RegionType = RegionTypeCode.Normalize(n2.RegionType);
                }
            }
        }

        private async Task<(string? sourceId, string? sourceCode, string? qcId, string? qcCode)> ResolveStockInNotifyAndQcFieldsAsync(
            CreateStockInRequest request)
        {
            string? sourceId = null;
            string? sourceCode = null;
            string? qcId = null;
            string? qcCode = null;

            if (!string.IsNullOrWhiteSpace(request.StockInNotifyId))
            {
                var n = await _stockInNotifyRepository.GetByIdAsync(request.StockInNotifyId.Trim());
                if (n != null)
                {
                    sourceId = string.IsNullOrWhiteSpace(n.Id) ? null : n.Id.Trim();
                    sourceCode = string.IsNullOrWhiteSpace(n.NoticeCode) ? null : n.NoticeCode.Trim();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.QcId))
            {
                var qc = await _qcRepository.GetByIdAsync(request.QcId.Trim());
                if (qc != null)
                {
                    qcId = string.IsNullOrWhiteSpace(qc.Id) ? null : qc.Id.Trim();
                    qcCode = string.IsNullOrWhiteSpace(qc.QcCode) ? null : qc.QcCode.Trim();
                    if (string.IsNullOrWhiteSpace(sourceId) && !string.IsNullOrWhiteSpace(qc.StockInNotifyId))
                    {
                        var n2 = await _stockInNotifyRepository.GetByIdAsync(qc.StockInNotifyId.Trim());
                        if (n2 != null)
                        {
                            sourceId = string.IsNullOrWhiteSpace(n2.Id) ? null : n2.Id.Trim();
                            sourceCode = string.IsNullOrWhiteSpace(n2.NoticeCode) ? null : n2.NoticeCode.Trim();
                        }
                    }
                }
            }

            return (sourceId, sourceCode, qcId, qcCode);
        }

        /// <summary>将采购明细 PN/品牌/币别快照写入入库明细行（采销行主键与业务编号见 <c>stockinitemextend</c>）。</summary>
        private static void ApplyPurchaseSnapshotToStockInItem(StockInItem line, PurchaseOrderItem? poi)
        {
            if (poi == null) return;
            line.PurchasePn = string.IsNullOrWhiteSpace(poi.PN) ? null : poi.PN.Trim();
            line.PurchaseBrand = string.IsNullOrWhiteSpace(poi.Brand) ? null : poi.Brand.Trim();
            line.Currency = poi.Currency;
        }

        private static bool IsCustomsArrivalNotify(StockInNotify notify) =>
            StockInTypeCode.NormalizeForNotify(notify.StockInType) == StockInTypeCode.Customs
            || !string.IsNullOrWhiteSpace(notify.CustomsDeclarationItemId);

        /// <summary>报关到货通知生成入库：无采购明细时从通知快照 PN/品牌/币别。</summary>
        private static void ApplyCustomsArrivalNotifySnapshotToStockInItem(StockInItem line, StockInNotify notify)
        {
            if (string.IsNullOrWhiteSpace(line.PurchasePn) && !string.IsNullOrWhiteSpace(notify.Pn))
                line.PurchasePn = notify.Pn.Trim();
            if (string.IsNullOrWhiteSpace(line.PurchaseBrand) && !string.IsNullOrWhiteSpace(notify.Brand))
                line.PurchaseBrand = notify.Brand.Trim();
            line.Currency ??= (short)CurrencyCode.RMB;
        }

        /// <summary>
        /// 入库明细 MaterialId 对齐物料主数据；来源为采购单时再按采购行补 PN/品牌（与库存总览逻辑一致）。
        /// </summary>
        private static void ResolveStockInLineModelBrand(
            string materialIdTrimmed,
            Dictionary<string, MaterialInfo> materials,
            IReadOnlyList<PurchaseOrderItem>? poLinesForSource,
            out string? model,
            out string? brand)
        {
            model = null;
            brand = null;
            if (materials.TryGetValue(materialIdTrimmed, out var mat))
            {
                if (!string.IsNullOrWhiteSpace(mat.MaterialModel))
                    model = mat.MaterialModel.Trim();
                if (!string.IsNullOrWhiteSpace(mat.MaterialName))
                    brand = mat.MaterialName.Trim();
            }

            PurchaseOrderItem? hit = null;
            if (poLinesForSource is { Count: > 0 })
            {
                hit = poLinesForSource.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.ProductId) &&
                    string.Equals(p.ProductId.Trim(), materialIdTrimmed, StringComparison.OrdinalIgnoreCase));
                if (hit == null)
                    hit = poLinesForSource.FirstOrDefault(p =>
                        string.Equals(p.Id, materialIdTrimmed, StringComparison.OrdinalIgnoreCase));
                if (hit == null && poLinesForSource.Count == 1)
                    hit = poLinesForSource[0];
            }

            if (hit != null)
            {
                if (string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(hit.PN))
                    model = hit.PN!.Trim();
                if (string.IsNullOrWhiteSpace(brand) && !string.IsNullOrWhiteSpace(hit.Brand))
                    brand = hit.Brand!.Trim();
            }
        }
    }
}

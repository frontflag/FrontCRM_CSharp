using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Models.Vendor;
using CRM.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace CRM.Core.Services
{
    /// <summary>
    /// 入库服务实现
    /// </summary>
    public class StockInService : IStockInService
    {
        private readonly IRepository<StockIn> _stockInRepository;
        private readonly IRepository<StockInItem> _stockInItemRepository;
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
        private readonly ILogger<StockInService> _logger;

        public StockInService(
            IRepository<StockIn> stockInRepository,
            IRepository<StockInItem> stockInItemRepository,
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
            IUnitOfWork unitOfWork,
            ILogger<StockInService> logger)
        {
            _stockInRepository = stockInRepository;
            _stockInItemRepository = stockInItemRepository;
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
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<StockIn> CreateAsync(CreateStockInRequest request, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(request.WarehouseId))
                throw new ArgumentException("仓库ID不能为空", nameof(request.WarehouseId));

            var stockInCode = await _serialNumberService.GenerateNextAsync(ModuleCodes.StockIn);

            var stockInId = Guid.NewGuid().ToString();
            var purchaseOrderId = request.PurchaseOrderId?.Trim();

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

            var primaryLine = ResolvePrimaryPurchaseOrderLineForStockInHeader(request.Items, poLineById, poLinesForHeader);
            string? sellLineCode = null;
            if (primaryLine != null && !string.IsNullOrWhiteSpace(primaryLine.SellOrderItemId))
            {
                var soLine = await _sellOrderItemRepository.GetByIdAsync(primaryLine.SellOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(soLine?.SellOrderItemCode))
                    sellLineCode = soLine!.SellOrderItemCode.Trim();
            }

            var (arrivalId, arrivalCode, qcIdForSi, qcCodeForSi) = await ResolveStockInNotifyAndQcFieldsAsync(request);

            var stockIn = new StockIn
            {
                Id = stockInId,
                StockInCode = stockInCode,
                StockInType = 1, // 采购入库
                PurchaseOrderItemId = string.IsNullOrWhiteSpace(primaryLine?.Id) ? null : primaryLine!.Id.Trim(),
                PurchaseOrderItemCode = string.IsNullOrWhiteSpace(primaryLine?.PurchaseOrderItemCode)
                    ? null
                    : primaryLine!.PurchaseOrderItemCode.Trim(),
                SellOrderItemId = string.IsNullOrWhiteSpace(primaryLine?.SellOrderItemId)
                    ? null
                    : primaryLine!.SellOrderItemId.Trim(),
                SellOrderItemCode = sellLineCode,
                SourceId = arrivalId,
                SourceCode = arrivalCode,
                QcId = qcIdForSi,
                QcCode = qcCodeForSi,
                WarehouseId = request.WarehouseId,
                VendorId = request.VendorId,
                StockInDate = PostgreSqlDateTime.ToUtc(request.StockInDate),
                TotalQuantity = request.TotalQuantity,
                Remark = request.Remark,
                Status = 0, // 草稿
                CreateTime = DateTime.UtcNow,
                CreatedBy = string.IsNullOrWhiteSpace(request.OperatorId) ? null : request.OperatorId.Trim(),
                CreateByUserId = ActingUserIdNormalizer.Normalize(actingUserId ?? request.OperatorId)
            };

            await _stockInRepository.AddAsync(stockIn);
            // 先落主单，避免后续插入明细时触发 FK(stockinitem.stockinid -> stockin.stockinid) 失败
            await _unitOfWork.SaveChangesAsync();

            if (request.Items != null && request.Items.Count > 0)
            {
                decimal totalAmount = 0;
                foreach (var item in request.Items)
                {
                    var materialKey = item.MaterialCode?.Trim() ?? string.Empty;
                    var price = item.UnitPrice ?? 0m;
                    // 质检生成入库等场景前端可能传 0 价：按采购明细 Id（与 MaterialId 一致）回填采购单价
                    if (price == 0m && poLineById != null && !string.IsNullOrWhiteSpace(materialKey)
                        && poLineById.TryGetValue(materialKey, out var poi))
                        price = poi.Cost;

                    var amount = item.Quantity * price;
                    totalAmount += amount;
                    var line = new StockInItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        StockInId = stockInId,
                        MaterialId = materialKey,
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

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                return null;

            var lines = (await _stockInItemRepository.FindAsync(x => x.StockInId == stockIn.Id))
                .OrderBy(x => x.CreateTime)
                .ThenBy(x => x.Id)
                .ToList();
            stockIn.Items = lines;

            if (!string.IsNullOrWhiteSpace(stockIn.WarehouseId))
            {
                var wh = await _warehouseRepository.GetByIdAsync(stockIn.WarehouseId.Trim());
                stockIn.DetailWarehouseCode = string.IsNullOrWhiteSpace(wh?.WarehouseCode)
                    ? null
                    : wh!.WarehouseCode.Trim();
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
                        if (string.IsNullOrEmpty(mid) || !matById.TryGetValue(mid, out var mat))
                            continue;
                        line.DetailMaterialCode = string.IsNullOrWhiteSpace(mat.MaterialCode)
                            ? null
                            : mat.MaterialCode.Trim();
                        line.DetailMaterialName = string.IsNullOrWhiteSpace(mat.MaterialName)
                            ? null
                            : mat.MaterialName.Trim();
                        line.DetailUnit = string.IsNullOrWhiteSpace(mat.Unit) ? null : mat.Unit.Trim();
                    }
                }
                catch
                {
                    // 物料表不可用时仍返回明细，仅不填充编码/名称
                }
            }

            return stockIn;
        }

        public async Task<IReadOnlyList<StockInListItemDto>> GetListAsync(StockInQueryRequest? request = null)
        {
            var raw = (await _stockInRepository.GetAllAsync())
                .OrderByDescending(x => x.StockInDate)
                .ThenByDescending(x => x.CreateTime)
                .ToList();

            if (raw.Count == 0)
                return Array.Empty<StockInListItemDto>();

            var stockInIds = raw.Select(x => x.Id).ToList();
            var vendorIds = raw.Select(x => x.VendorId).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            var stockInItems = (await _stockInItemRepository.FindAsync(x => stockInIds.Contains(x.StockInId))).ToList();
            var stockInItemsMap = stockInItems
                .GroupBy(x => x.StockInId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var poLineIds = raw
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

            var sellOnlyIds = raw
                .Where(s => string.IsNullOrWhiteSpace(s.PurchaseOrderItemId) && !string.IsNullOrWhiteSpace(s.SellOrderItemId))
                .Select(s => s.SellOrderItemId!.Trim())
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
                .Select(v => v.PurchaseOrderId.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
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

            var qcByStockInId = (await _qcRepository.FindAsync(q => q.StockInId != null && stockInIds.Contains(q.StockInId!)))
                .GroupBy(q => q.StockInId!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

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
                .Concat(raw.Select(r => r.SellOrderItemId))
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
                PurchaseOrderItem? headerPoLine = null;
                if (!string.IsNullOrWhiteSpace(s.PurchaseOrderItemId) &&
                    poLineEntityById.TryGetValue(s.PurchaseOrderItemId.Trim(), out var plHeader))
                    headerPoLine = plHeader;
                else if (!string.IsNullOrWhiteSpace(s.SellOrderItemId))
                {
                    headerPoLine = poLineEntityById.Values.FirstOrDefault(v =>
                        !string.IsNullOrWhiteSpace(v.SellOrderItemId) &&
                        string.Equals(v.SellOrderItemId.Trim(), s.SellOrderItemId!.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                string? sourceDisplay = null;
                if (!string.IsNullOrWhiteSpace(s.SourceCode))
                    sourceDisplay = s.SourceCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && headerPoLine != null &&
                    poDict.TryGetValue(headerPoLine.PurchaseOrderId.Trim(), out var poForDisp) &&
                    !string.IsNullOrWhiteSpace(poForDisp.PurchaseOrderCode))
                    sourceDisplay = poForDisp.PurchaseOrderCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && qcByStockInId.TryGetValue(s.Id, out var qcLinked))
                    sourceDisplay = qcLinked.QcCode;
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(s.QcCode))
                    sourceDisplay = s.QcCode.Trim();
                if (string.IsNullOrWhiteSpace(sourceDisplay) && !string.IsNullOrWhiteSpace(s.PurchaseOrderItemCode))
                    sourceDisplay = s.PurchaseOrderItemCode.Trim();

                string? vendorName = null;
                if (!string.IsNullOrWhiteSpace(s.VendorId) && venDict.TryGetValue(s.VendorId!, out var v))
                {
                    vendorName = !string.IsNullOrWhiteSpace(v.OfficialName) ? v.OfficialName
                        : !string.IsNullOrWhiteSpace(v.NickName) ? v.NickName
                        : v.Code;
                }

                var salesOrderCodes = new List<string>();
                if (headerPoLine != null && !string.IsNullOrWhiteSpace(headerPoLine.SellOrderItemId) &&
                    soIdByItemId.TryGetValue(headerPoLine.SellOrderItemId.Trim(), out var soId0) &&
                    soDict.TryGetValue(soId0, out var so0) && !string.IsNullOrWhiteSpace(so0.SellOrderCode))
                    salesOrderCodes.Add(so0.SellOrderCode!);
                if (!string.IsNullOrWhiteSpace(s.SellOrderItemId) &&
                    (!headerPoLine?.SellOrderItemId?.Trim().Equals(s.SellOrderItemId.Trim(), StringComparison.OrdinalIgnoreCase) ?? true) &&
                    soIdByItemId.TryGetValue(s.SellOrderItemId.Trim(), out var soId1) &&
                    soDict.TryGetValue(soId1, out var so1) && !string.IsNullOrWhiteSpace(so1.SellOrderCode))
                    salesOrderCodes.Add(so1.SellOrderCode!);

                if (headerPoLine != null &&
                    poItemsMap.TryGetValue(headerPoLine.PurchaseOrderId.Trim(), out var thisPoItems))
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

                string? modelSummary = null;
                string? brandSummary = null;
                if (stockInItemsMap.TryGetValue(s.Id, out var silForDisplay) && silForDisplay.Count > 0)
                {
                    IReadOnlyList<PurchaseOrderItem>? poLinesForS = null;
                    if (headerPoLine != null &&
                        poItemsMap.TryGetValue(headerPoLine.PurchaseOrderId.Trim(), out var pl0))
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
                    // 历史数据：明细单价为 0 但 MaterialId 为采购行 Id 时，用采购单价回算展示金额
                    if (displayTotalAmount == 0m && headerPoLine != null &&
                        poItemsMap.TryGetValue(headerPoLine.PurchaseOrderId.Trim(), out var poLinesForAmt) &&
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
                    createUserName = EntityLookupService.FormatUserDisplayName(cu);

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

                result.Add(new StockInListItemDto
                {
                    Id = s.Id,
                    StockInCode = s.StockInCode,
                    StockInType = s.StockInType,
                    SourceDisplayNo = sourceDisplay,
                    WarehouseId = s.WarehouseId,
                    VendorId = s.VendorId,
                    VendorName = vendorName,
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
                    CreateUserName = createUserName
                });
            }

            var modelKeyword = request?.Model?.Trim();
            var vendorKeyword = request?.VendorName?.Trim();
            var poCodeKeyword = request?.PurchaseOrderCode?.Trim();
            var soCodeKeyword = request?.SalesOrderCode?.Trim();

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
                    && !(x.SourceDisplayNo?.Contains(poCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                if (!string.IsNullOrWhiteSpace(soCodeKeyword)
                    && !(x.SalesOrderCode?.Contains(soCodeKeyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    return false;
                return true;
            }).ToList();
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

            await _stockInRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(string id, short status, string? actingUserId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID不能为空", nameof(id));

            var stockIn = await _stockInRepository.GetByIdAsync(id);
            if (stockIn == null)
                throw new InvalidOperationException($"入库单 {id} 不存在");

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

            await _stockInRepository.UpdateAsync(stockIn);
            await _unitOfWork.SaveChangesAsync();

            if (status == 2)
            {
                _logger.LogInformation(
                    "[InboundStatus2] StockIn status→2 chain start StockInId={StockInId} StockInCode={Code} PoItemId={PoItemId} PoItemCode={PoItemCode}",
                    stockIn.Id,
                    stockIn.StockInCode ?? "",
                    stockIn.PurchaseOrderItemId ?? "",
                    stockIn.PurchaseOrderItemCode ?? "");

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
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("[InboundStatus2] StockIn status→2 chain end StockInId={StockInId}", stockIn.Id);
            }
        }

        /// <summary>
        /// 物流回写主要更新采购侧扩展；销售明细「入库进度」依赖 <see cref="SellOrderItemExtendSyncService"/>，且扩展更新需 SaveChanges 才落库。
        /// </summary>
        private async Task TryRefreshSellOrderItemExtendAfterStockInCompletedAsync(StockIn stockIn)
        {
            string? sellLineId = null;
            if (!string.IsNullOrWhiteSpace(stockIn.SellOrderItemId))
                sellLineId = stockIn.SellOrderItemId.Trim();
            else if (!string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemId))
            {
                var pl = await _purchaseOrderItemRepository.GetByIdAsync(stockIn.PurchaseOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(pl?.SellOrderItemId))
                    sellLineId = pl.SellOrderItemId.Trim();
            }

            if (string.IsNullOrWhiteSpace(sellLineId)) return;

            await _sellOrderItemExtendSync.RecalculateAsync(sellLineId);
        }

        private async Task<string?> ResolvePurchaseOrderIdForStockInCompletedHookAsync(StockIn stockIn)
        {
            if (!string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemId))
            {
                _logger.LogInformation(
                    "[InboundStatus2] ResolveHook branch=PoItemId PoItemId={PoItemId}",
                    stockIn.PurchaseOrderItemId.Trim());
                var pl = await _purchaseOrderItemRepository.GetByIdAsync(stockIn.PurchaseOrderItemId.Trim());
                if (!string.IsNullOrWhiteSpace(pl?.PurchaseOrderId))
                    return pl!.PurchaseOrderId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(stockIn.PurchaseOrderItemCode))
            {
                _logger.LogInformation(
                    "[InboundStatus2] ResolveHook branch=PoItemCode PoItemCode={PoItemCode}",
                    stockIn.PurchaseOrderItemCode.Trim());
                // 库列名为 purchase_order_item_code；用 LINQ 会生成 i."PurchaseOrderItemCode" 导致 42703
                var poId = await _unitOfWork.GetPurchaseOrderIdByPurchaseOrderItemLineCodeAsync(
                    stockIn.PurchaseOrderItemCode.Trim());
                if (!string.IsNullOrWhiteSpace(poId))
                    return poId.Trim();
            }

            _logger.LogInformation("[InboundStatus2] ResolveHook branch=none (no Po id resolved)");
            return null;
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

        private static PurchaseOrderItem? ResolvePrimaryPurchaseOrderLineForStockInHeader(
            List<CreateStockInItemRequest>? items,
            IReadOnlyDictionary<string, PurchaseOrderItem>? poLineById,
            List<PurchaseOrderItem>? poLines)
        {
            if (items is { Count: > 0 } && poLineById != null)
            {
                foreach (var item in items)
                {
                    var key = item.MaterialCode?.Trim();
                    if (string.IsNullOrEmpty(key)) continue;
                    if (poLineById.TryGetValue(key, out var pl))
                        return pl;
                }
            }

            if (poLines is { Count: 1 })
                return poLines[0];

            return null;
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

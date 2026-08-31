using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Material;
using CRM.Core.Models.Purchase;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class StockOutNotifyFlowService : IStockOutNotifyFlowService
{
    private readonly IInventoryStockItemListQuery _stockItemListQuery;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockInfo> _stockRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<MaterialInfo> _materialRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutExtendRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly ICustomsTraceQuery _customsTraceQuery;

    public StockOutNotifyFlowService(
        IInventoryStockItemListQuery stockItemListQuery,
        IDataPermissionService dataPermission,
        IRepository<StockOutRequest> notifyRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<StockInfo> stockRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<MaterialInfo> materialRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<StockOutItemExtend> stockOutExtendRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        ICustomsTraceQuery customsTraceQuery)
    {
        _stockItemListQuery = stockItemListQuery;
        _dataPermission = dataPermission;
        _notifyRepo = notifyRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _stockItemRepo = stockItemRepo;
        _stockRepo = stockRepo;
        _stockInRepo = stockInRepo;
        _materialRepo = materialRepo;
        _poItemRepo = poItemRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _stockOutExtendRepo = stockOutExtendRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutRepo = stockOutRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _customsTraceQuery = customsTraceQuery;
    }

    public async Task<StockOutNotifyFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockOutNotifyId,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var id = stockOutNotifyId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("出库通知ID不能为空", nameof(stockOutNotifyId));

        var notify = await _notifyRepo.GetByIdAsync(id);
        if (notify == null || notify.IsDeleted)
            throw new InvalidOperationException("出库通知不存在或无权查看");

        SellOrder? sellOrder = null;
        if (!string.IsNullOrWhiteSpace(notify.SalesOrderId))
            sellOrder = await _sellOrderRepo.GetByIdAsync(notify.SalesOrderId.Trim());

        if (sellOrder != null && !string.IsNullOrWhiteSpace(currentUserId))
        {
            if (!await _dataPermission.CanAccessSalesOrderAsync(currentUserId.Trim(), sellOrder))
                throw new InvalidOperationException("出库通知不存在或无权查看");
        }

        SellOrderItem? sellLine = null;
        var sellLineId = notify.SalesOrderItemId?.Trim() ?? string.Empty;
        if (sellLineId.Length > 0)
            sellLine = await _sellOrderItemRepo.GetByIdAsync(sellLineId);

        var dto = new StockOutNotifyFlowAggregatesDto { StockOutNotifyId = id };

        var userIds = new List<string>();
        AddId(userIds, sellOrder?.SalesUserId);
        AddId(userIds, notify.CreateByUserId);
        AddId(userIds, notify.RequestUserId);

        var customerIds = new List<string>();
        AddId(customerIds, notify.CustomerId);
        AddId(customerIds, sellOrder?.CustomerId);

        var packingItems = (await _packingItemRepo.FindAsync(pi =>
                !pi.IsDeleted && pi.StockOutNotifyId == id)).ToList();
        var packingIds = packingItems
            .Select(pi => pi.PackingId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var packings = packingIds.Count == 0
            ? new List<Packing>()
            : (await _packingRepo.FindAsync(p => packingIds.Contains(p.Id) && !p.IsDeleted)).ToList();
        var packingMap = packings.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        var stockOuts = await LoadStockOutsForThisNotifyAsync(packingIds, cancellationToken);
        foreach (var p in packings)
        {
            AddId(userIds, p.SalesId);
            AddId(userIds, p.CreateByUserId);
            AddId(customerIds, p.CustomerId);
        }
        foreach (var s in stockOuts)
            AddId(userIds, s.CreateByUserId);

        var users = await LoadUsersAsync(userIds);
        var customers = await LoadCustomersAsync(customerIds);

        dto.SellOrderItem = sellLine == null || sellLine.IsDeleted
            ? null
            : MapSellOrderItem(sellLine, sellOrder, customers, users);

        var packingForNotify = FirstPackingForNotify(packingItems, packingMap, id);
        dto.StockOutNotify = MapNotify(notify, sellOrder, packingForNotify, customers, users);

        var boundLayers = sellLineId.Length == 0
            ? new List<StockItem>()
            : (await _stockItemRepo.FindAsync(si =>
                si.SellOrderItemId == sellLineId)).ToList();
        var boundIds = boundLayers
            .Select(x => x.Id?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        dto.StockItems = await MapStockItemDocsAsync(boundIds, currentUserId, cancellationToken);

        dto.StockingStockItems = await LoadStockingDocsAsync(
            sellLine,
            boundIds,
            currentUserId,
            cancellationToken);

        await ApplyInboundCustomsAsync(dto.StockItems, cancellationToken);
        await ApplyInboundCustomsAsync(dto.StockingStockItems, cancellationToken);

        var outTraceNotifyIds = new List<string>();
        if (StockOutTypeCode.NormalizeForNotify(notify.StockOutType) == StockOutTypeCode.Customs)
            AddId(outTraceNotifyIds, notify.Id);
        foreach (var s in stockOuts)
        {
            if (StockOutTypeCode.NormalizeForNotify(s.StockOutType) == StockOutTypeCode.Customs)
                AddId(outTraceNotifyIds, s.SourceId);
        }
        foreach (var p in packings)
        {
            if (StockOutTypeCode.NormalizeForNotify(p.StockOutType) != StockOutTypeCode.Customs)
                continue;
            AddId(outTraceNotifyIds, id);
        }
        var outTrace = outTraceNotifyIds.Count == 0
            ? (IReadOnlyDictionary<string, CustomsTraceLinkDto>)new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase)
            : await _customsTraceQuery.GetByStockOutNotifyIdsAsync(outTraceNotifyIds, cancellationToken);
        var packingDeclCode = await LoadPackingDeclarationCodesAsync(packings, outTrace, cancellationToken);

        ApplyStockOutCustoms(
            dto.StockOutNotify,
            notify.StockOutType,
            notify.Id,
            packingForNotify?.CustomsDeclarationId,
            outTrace,
            packingDeclCode);

        foreach (var item in dto.StockItems)
            item.StockOutType = notify.StockOutType;

        var packingItemStockItemIds = packingItems
            .Select(pi => pi.StockItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var extendRows = packingItemStockItemIds.Count == 0
            ? new List<StockOutItemExtend>()
            : (await _stockOutExtendRepo.FindAsync(x =>
                !x.IsDeleted
                && x.StockItemId != null
                && packingItemStockItemIds.Contains(x.StockItemId))).ToList();
        var outItemIds = extendRows
            .Select(x => x.Id?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var outItems = outItemIds.Count == 0
            ? new List<StockOutItem>()
            : (await _stockOutItemRepo.FindAsync(i => outItemIds.Contains(i.Id) && !i.IsDeleted)).ToList();
        var outItemMap = outItems.ToDictionary(i => i.Id, StringComparer.OrdinalIgnoreCase);

        dto.Packings = packings
            .OrderBy(p => p.CreateTime)
            .Select(p =>
            {
                var layerQty = packingItems
                    .Where(pi => string.Equals(pi.PackingId?.Trim(), p.Id, StringComparison.OrdinalIgnoreCase))
                    .Sum(pi => pi.Qty);
                var doc = new StockItemFlowDocDto
                {
                    Id = p.Id,
                    DocCode = p.Code,
                    Status = p.Status,
                    CreateTime = p.CreateTime,
                    CustomerName = CustomerNameOf(customers, p.CustomerId)
                        ?? CustomerNameOf(customers, notify.CustomerId),
                    CustomerCode = CustomerCodeOf(customers, p.CustomerId)
                        ?? CustomerCodeOf(customers, notify.CustomerId),
                    PersonName = UserDisplay(users, p.SalesId)
                        ?? UserDisplay(users, sellOrder?.SalesUserId),
                    Qty = layerQty,
                    StockOutType = p.StockOutType
                };
                ApplyStockOutCustoms(doc, p.StockOutType, id, p.CustomsDeclarationId, outTrace, packingDeclCode);
                return doc;
            })
            .ToList();

        dto.StockOuts = stockOuts
            .OrderBy(s => s.CreateTime)
            .Select(s =>
            {
                var layerQty = extendRows
                    .Where(ex =>
                    {
                        if (!outItemMap.TryGetValue(ex.Id, out var item))
                            return false;
                        return string.Equals(item.StockOutId?.Trim(), s.Id, StringComparison.OrdinalIgnoreCase);
                    })
                    .Sum(ex => ex.QtyStockOut);
                if (layerQty == 0)
                {
                    var packingId = EmptyToNull(s.SourceId);
                    if (!string.IsNullOrEmpty(packingId))
                    {
                        layerQty = packingItems
                            .Where(pi => string.Equals(pi.PackingId?.Trim(), packingId, StringComparison.OrdinalIgnoreCase))
                            .Sum(pi => pi.Qty);
                    }
                }
                var doc = new StockItemFlowDocDto
                {
                    Id = s.Id,
                    DocCode = s.StockOutCode,
                    Status = s.Status,
                    CreateTime = s.CreateTime,
                    CustomerName = CustomerNameOf(customers, notify.CustomerId),
                    CustomerCode = CustomerCodeOf(customers, notify.CustomerId),
                    PersonName = UserDisplay(users, s.CreateByUserId)
                        ?? UserDisplay(users, sellOrder?.SalesUserId),
                    Qty = layerQty,
                    StockOutType = s.StockOutType
                };
                var packingOfOut = EmptyToNull(s.SourceId) is string pid && packingMap.TryGetValue(pid, out var pOut)
                    ? pOut
                    : packingForNotify;
                ApplyStockOutCustoms(doc, s.StockOutType, id, packingOfOut?.CustomsDeclarationId, outTrace, packingDeclCode);
                return doc;
            })
            .ToList();

        return dto;
    }

    private async Task<List<StockOut>> LoadStockOutsForThisNotifyAsync(
        List<string> packingIds,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (packingIds.Count == 0)
            return new List<StockOut>();

        var bySource = (await _stockOutRepo.FindAsync(s =>
                !s.IsDeleted && s.SourceId != null && packingIds.Contains(s.SourceId))).ToList();
        var bySourceIds = bySource
            .Select(s => s.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = (await _stockOutItemRepo.FindAsync(i =>
                !i.IsDeleted && i.PackingId != null && packingIds.Contains(i.PackingId))).ToList();
        var extraIds = items
            .Select(i => i.StockOutId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x) && !bySourceIds.Contains(x!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (extraIds.Count == 0)
            return bySource.OrderBy(s => s.CreateTime).ToList();

        var extra = (await _stockOutRepo.FindAsync(s => extraIds.Contains(s.Id) && !s.IsDeleted)).ToList();
        bySource.AddRange(extra);
        return bySource
            .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(s => s.CreateTime)
            .ToList();
    }

    private async Task<List<StockItemFlowDocDto>> LoadStockingDocsAsync(
        SellOrderItem? sellLine,
        IReadOnlyList<string> boundIds,
        string? currentUserId,
        CancellationToken cancellationToken)
    {
        if (sellLine == null)
            return new List<StockItemFlowDocDto>();

        var soPn = sellLine.PN?.Trim() ?? string.Empty;
        var soBrand = sellLine.Brand?.Trim() ?? string.Empty;
        if (soPn.Length == 0 && soBrand.Length == 0)
            return new List<StockItemFlowDocDto>();

        var boundSet = boundIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var layers = (await _stockItemRepo.FindAsync(si =>
                si.StockType == 2 && si.QtyRepertory > 0)).ToList();
        layers = layers
            .Where(si =>
            {
                var lid = si.Id?.Trim() ?? string.Empty;
                if (lid.Length == 0 || boundSet.Contains(lid))
                    return false;
                var sellId = si.SellOrderItemId?.Trim() ?? string.Empty;
                return !string.Equals(sellId, sellLine.Id, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();
        if (layers.Count == 0)
            return new List<StockItemFlowDocDto>();

        var aggIds = layers
            .Select(si => si.StockAggregateId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var stocks = aggIds.Count == 0
            ? new List<StockInfo>()
            : (await _stockRepo.FindAsync(s => aggIds.Contains(s.Id))).ToList();
        var stocksById = stocks
            .Where(s => s.StockType == 2)
            .GroupBy(s => s.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var poiIds = new List<string>();
        foreach (var si in layers)
            AddId(poiIds, si.PurchaseOrderItemId);
        foreach (var s in stocksById.Values)
        {
            AddId(poiIds, s.PurchaseOrderItemId);
            AddId(poiIds, s.MaterialId);
        }
        var poItems = poiIds.Count == 0
            ? new List<PurchaseOrderItem>()
            : (await _poItemRepo.FindAsync(p => poiIds.Contains(p.Id))).ToList();
        var poItemById = poItems
            .GroupBy(p => p.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var materialIds = new List<string>();
        foreach (var si in layers)
            AddId(materialIds, si.MaterialId);
        foreach (var s in stocksById.Values)
            AddId(materialIds, s.MaterialId);
        foreach (var p in poItems)
            AddId(materialIds, p.ProductId);

        List<MaterialInfo> materials;
        if (soPn.Length > 0)
        {
            materials = (await _materialRepo.FindAsync(m =>
                materialIds.Contains(m.Id)
                || m.MaterialCode == soPn
                || m.MaterialModel == soPn)).ToList();
        }
        else
        {
            materials = materialIds.Count == 0
                ? new List<MaterialInfo>()
                : (await _materialRepo.FindAsync(m => materialIds.Contains(m.Id))).ToList();
        }
        var materialById = materials
            .GroupBy(m => m.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var materialByAltKey = StockMaterialMatch.BuildMaterialCodeModelIndex(materials);

        var matchedIds = new List<string>();
        foreach (var si in layers)
        {
            var aggId = si.StockAggregateId?.Trim() ?? string.Empty;
            if (aggId.Length == 0 || !stocksById.TryGetValue(aggId, out var stock))
                continue;
            if (!StockMaterialMatch.StockingSupplementEligible(
                    stock, sellLine, materialById, materialByAltKey, poItemById))
                continue;
            AddId(matchedIds, si.Id);
        }

        return await MapStockItemDocsAsync(matchedIds, currentUserId, cancellationToken);
    }

    private async Task<List<StockItemFlowDocDto>> MapStockItemDocsAsync(
        IReadOnlyList<string> orderedIds,
        string? currentUserId,
        CancellationToken cancellationToken)
    {
        if (orderedIds.Count == 0)
            return new List<StockItemFlowDocDto>();

        var rows = await _stockItemListQuery.GetByIdsAsync(
            orderedIds,
            currentUserId,
            applyDataScope: true,
            cancellationToken);
        var byId = rows.ToDictionary(r => r.StockItemId, StringComparer.OrdinalIgnoreCase);
        var result = new List<StockItemFlowDocDto>();
        foreach (var id in orderedIds)
        {
            if (!byId.TryGetValue(id, out var row))
                continue;
            result.Add(MapStockItemStation(row));
        }
        return result;
    }

    private static StockItemFlowDocDto MapStockItemStation(InventoryStockItemListRowDto row)
    {
        return new StockItemFlowDocDto
        {
            Id = row.StockItemId,
            DocCode = row.StockItemCode,
            Status = row.OutboundStatus,
            CreateTime = row.CreateTime,
            BizDate = row.StockInDate,
            VendorName = row.VendorName,
            VendorCode = row.VendorCode,
            CustomerName = row.CustomerName,
            PersonName = row.SalespersonName,
            UnitPrice = row.PurchasePrice,
            Currency = row.PurchaseCurrency,
            SalesUnitPrice = row.SalesPrice,
            SalesCurrency = row.SalesCurrency,
            Qty = row.QtyInbound,
            Qty2 = row.QtyStockOut,
            StockInType = row.StockInType,
            StockAggregateId = row.StockAggregateId
        };
    }

    private static StockItemFlowDocDto MapSellOrderItem(
        SellOrderItem line,
        SellOrder? order,
        Dictionary<string, CustomerInfo> customers,
        Dictionary<string, User> users)
    {
        var customerId = order?.CustomerId;
        return new StockItemFlowDocDto
        {
            Id = line.Id,
            DocCode = line.SellOrderItemCode,
            Status = (short)(order?.Status ?? 0),
            CreateTime = line.CreateTime,
            CustomerName = FirstNonEmpty(
                CustomerNameOf(customers, customerId),
                order?.CustomerName),
            CustomerCode = CustomerCodeOf(customers, customerId),
            PersonName = UserDisplay(users, order?.SalesUserId)
                ?? FirstNonEmpty(order?.SalesUserRealName, order?.SalesUserName),
            SalesUnitPrice = line.Price,
            SalesCurrency = line.Currency,
            Qty = line.Qty,
            SellOrderId = line.SellOrderId
        };
    }

    private static StockItemFlowDocDto MapNotify(
        StockOutRequest notify,
        SellOrder? order,
        Packing? packing,
        Dictionary<string, CustomerInfo> customers,
        Dictionary<string, User> users)
    {
        return new StockItemFlowDocDto
        {
            Id = notify.Id,
            DocCode = notify.RequestCode,
            Status = notify.Status,
            CreateTime = notify.CreateTime,
            CustomerName = CustomerNameOf(customers, notify.CustomerId)
                ?? FirstNonEmpty(order?.CustomerName),
            CustomerCode = CustomerCodeOf(customers, notify.CustomerId),
            PersonName = packing != null
                ? UserDisplay(users, packing.SalesId) ?? UserDisplay(users, order?.SalesUserId)
                : UserDisplay(users, order?.SalesUserId) ?? UserDisplay(users, notify.RequestUserId),
            Qty = notify.Quantity,
            StockOutType = notify.StockOutType,
            SellOrderId = notify.SalesOrderId,
            PurchaseOrderItemId = notify.SalesOrderItemId
        };
    }

    private async Task ApplyInboundCustomsAsync(
        List<StockItemFlowDocDto> docs,
        CancellationToken cancellationToken)
    {
        if (docs.Count == 0)
            return;

        var ids = docs.Select(d => d.Id).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (ids.Count == 0)
            return;

        var layers = (await _stockItemRepo.FindAsync(si => ids.Contains(si.Id))).ToList();
        var stockInIds = layers
            .Select(si => si.StockInId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (stockInIds.Count == 0)
            return;

        var stockIns = (await _stockInRepo.FindAsync(s => stockInIds.Contains(s.Id))).ToList();
        var stockInById = stockIns.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        var notifyKeys = new List<string>();
        foreach (var s in stockIns)
        {
            if (s.StockInType != StockInTypeCode.Customs)
                continue;
            AddId(notifyKeys, s.SourceId);
        }
        var trace = notifyKeys.Count == 0
            ? (IReadOnlyDictionary<string, CustomsTraceLinkDto>)new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase)
            : await _customsTraceQuery.GetByStockInNotifyIdsAsync(notifyKeys, cancellationToken);

        var layerById = layers.ToDictionary(si => si.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var doc in docs)
        {
            if (!layerById.TryGetValue(doc.Id, out var layer))
                continue;
            var sinId = layer.StockInId?.Trim();
            if (string.IsNullOrEmpty(sinId) || !stockInById.TryGetValue(sinId, out var stockIn))
                continue;
            if (stockIn.StockInType != StockInTypeCode.Customs)
                continue;
            var notifyKey = EmptyToNull(stockIn.SourceId);
            if (notifyKey == null || !trace.TryGetValue(notifyKey, out var link))
                continue;
            doc.CustomsDeclarationId = EmptyToNull(link.CustomsDeclarationId);
            doc.CustomsDeclarationCode = EmptyToNull(link.CustomsDeclarationCode);
        }
    }

    private static Packing? FirstPackingForNotify(
        List<PackingItem> items,
        Dictionary<string, Packing> packingMap,
        string notifyId)
    {
        foreach (var pi in items)
        {
            if (!string.Equals(pi.StockOutNotifyId?.Trim(), notifyId, StringComparison.OrdinalIgnoreCase))
                continue;
            var pid = pi.PackingId?.Trim();
            if (!string.IsNullOrEmpty(pid) && packingMap.TryGetValue(pid, out var packing))
                return packing;
        }
        return null;
    }

    private async Task<Dictionary<string, User>> LoadUsersAsync(List<string> ids)
    {
        if (ids.Count == 0)
            return new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var list = (await _userRepo.FindAsync(u => ids.Contains(u.Id))).ToList();
        return list.ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, CustomerInfo>> LoadCustomersAsync(List<string> ids)
    {
        if (ids.Count == 0)
            return new Dictionary<string, CustomerInfo>(StringComparer.OrdinalIgnoreCase);
        var list = (await _customerRepo.FindAsync(c => ids.Contains(c.Id))).ToList();
        return list.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddId(List<string> ids, string? id)
    {
        var v = id?.Trim();
        if (string.IsNullOrEmpty(v))
            return;
        if (ids.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase)))
            return;
        ids.Add(v);
    }

    private static string? UserDisplay(Dictionary<string, User> users, string? id)
    {
        var key = id?.Trim();
        if (string.IsNullOrEmpty(key) || !users.TryGetValue(key, out var u))
            return null;
        var real = u.RealName?.Trim();
        if (!string.IsNullOrEmpty(real))
            return real;
        var login = u.UserName?.Trim();
        return string.IsNullOrEmpty(login) ? null : login;
    }

    private static string? CustomerNameOf(Dictionary<string, CustomerInfo> map, string? id)
    {
        var key = id?.Trim();
        if (string.IsNullOrEmpty(key) || !map.TryGetValue(key, out var c))
            return null;
        return FirstNonEmpty(c.OfficialName, c.NickName);
    }

    private static string? CustomerCodeOf(Dictionary<string, CustomerInfo> map, string? id)
    {
        var key = id?.Trim();
        if (string.IsNullOrEmpty(key) || !map.TryGetValue(key, out var c))
            return null;
        var code = c.CustomerCode?.Trim();
        return string.IsNullOrEmpty(code) ? null : code;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            var s = v?.Trim();
            if (!string.IsNullOrEmpty(s))
                return s;
        }
        return null;
    }

    private static string? EmptyToNull(string? v)
    {
        var s = v?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static void ApplyStockOutCustoms(
        StockItemFlowDocDto dto,
        short stockOutType,
        string? notifyId,
        string? packingDeclarationId,
        IReadOnlyDictionary<string, CustomsTraceLinkDto> outTrace,
        IReadOnlyDictionary<string, string> packingDeclCode)
    {
        if (StockOutTypeCode.NormalizeForNotify(stockOutType) != StockOutTypeCode.Customs)
            return;

        var packingId = EmptyToNull(packingDeclarationId);
        if (!string.IsNullOrEmpty(packingId))
        {
            dto.CustomsDeclarationId = packingId;
            if (packingDeclCode.TryGetValue(packingId, out var packingCode))
                dto.CustomsDeclarationCode = packingCode;
        }

        var nid = EmptyToNull(notifyId);
        if (string.IsNullOrEmpty(nid) || !outTrace.TryGetValue(nid, out var trace))
            return;
        dto.CustomsDeclarationId ??= EmptyToNull(trace.CustomsDeclarationId);
        dto.CustomsDeclarationCode ??= EmptyToNull(trace.CustomsDeclarationCode);
    }

    private async Task<Dictionary<string, string>> LoadPackingDeclarationCodesAsync(
        List<Packing> packings,
        IReadOnlyDictionary<string, CustomsTraceLinkDto> outTrace,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var trace in outTrace.Values)
        {
            var declId = EmptyToNull(trace.CustomsDeclarationId);
            var code = EmptyToNull(trace.CustomsDeclarationCode);
            if (declId == null || code == null || result.ContainsKey(declId))
                continue;
            result[declId] = code;
        }

        var missingIds = packings
            .Select(p => EmptyToNull(p.CustomsDeclarationId))
            .Where(declId => !string.IsNullOrEmpty(declId) && !result.ContainsKey(declId!))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var declId in missingIds)
        {
            var summary = await _customsTraceQuery.ResolveCustomsSummaryByDeclarationIdAsync(declId, cancellationToken);
            var code = EmptyToNull(summary?.DeclarationCode);
            if (code != null)
                result[declId] = code;
        }
        return result;
    }
}

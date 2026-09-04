using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Purchase;

namespace CRM.Core.Services;

public class StockItemFlowService : IStockItemFlowService
{
    private readonly IInventoryStockItemListQuery _stockItemListQuery;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<PurchaseOrderItem> _poItemRepo;
    private readonly IRepository<PurchaseOrder> _poRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<QCInfo> _qcRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PickingTaskItem> _pickingItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutExtendRepo;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly ICustomsTraceQuery _customsTraceQuery;

    public StockItemFlowService(
        IInventoryStockItemListQuery stockItemListQuery,
        IRepository<StockItem> stockItemRepo,
        IRepository<PurchaseOrderItem> poItemRepo,
        IRepository<PurchaseOrder> poRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<QCInfo> qcRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<PickingTaskItem> pickingItemRepo,
        IRepository<StockOutItemExtend> stockOutExtendRepo,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutRequest> notifyRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        ICustomsTraceQuery customsTraceQuery)
    {
        _stockItemListQuery = stockItemListQuery;
        _stockItemRepo = stockItemRepo;
        _poItemRepo = poItemRepo;
        _poRepo = poRepo;
        _stockInRepo = stockInRepo;
        _qcRepo = qcRepo;
        _packingItemRepo = packingItemRepo;
        _packingRepo = packingRepo;
        _pickingItemRepo = pickingItemRepo;
        _stockOutExtendRepo = stockOutExtendRepo;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutRepo = stockOutRepo;
        _notifyRepo = notifyRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _customsTraceQuery = customsTraceQuery;
    }

    public async Task<StockItemFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockItemId,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var id = stockItemId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("库存明细ID不能为空", nameof(stockItemId));

        var scoped = await _stockItemListQuery.GetByIdsAsync(
            new[] { id },
            currentUserId,
            applyDataScope: true,
            cancellationToken);
        var row = scoped.FirstOrDefault();
        if (row == null)
            throw new InvalidOperationException("库存明细不存在或无权查看");

        var entity = await _stockItemRepo.GetByIdAsync(id);
        if (entity == null)
            throw new InvalidOperationException("库存明细不存在或无权查看");

        var dto = new StockItemFlowAggregatesDto
        {
            StockItemId = id,
            StockItem = MapStockItemStation(row)
        };

        await FillPurchaseAsync(dto, entity, row, cancellationToken);
        await FillStockInAndQcAsync(dto, entity, cancellationToken);
        var downstream = await CollectDownstreamSliceAsync(id, row, cancellationToken);
        ApplyDownstreamSlice(dto, downstream);
        if (!string.IsNullOrWhiteSpace(row.CustomerId))
        {
            var customers = await LoadCustomersAsync(new List<string> { row.CustomerId.Trim() });
            dto.StockItem.CustomerCode = CustomerCodeOf(customers, row.CustomerId);
        }
        return dto;
    }

    /// <inheritdoc />
    public Task<StockItemFlowDownstreamSliceDto> GetDownstreamSliceAsync(
        string stockItemId,
        InventoryStockItemListRowDto row,
        CancellationToken cancellationToken = default) =>
        CollectDownstreamSliceAsync(stockItemId, row, cancellationToken);

    private static void ApplyDownstreamSlice(StockItemFlowAggregatesDto dto, StockItemFlowDownstreamSliceDto slice)
    {
        dto.StockOutNotifies = slice.StockOutNotifies;
        dto.Packings = slice.Packings;
        dto.StockOuts = slice.StockOuts;
    }

    private async Task<StockItemFlowDownstreamSliceDto> CollectDownstreamSliceAsync(
        string stockItemId,
        InventoryStockItemListRowDto row,
        CancellationToken cancellationToken)
    {
        var slice = new StockItemFlowDownstreamSliceDto();
        await FillDownstreamIntoSliceAsync(slice, stockItemId, row, cancellationToken);
        return slice;
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

    private async Task FillPurchaseAsync(
        StockItemFlowAggregatesDto dto,
        StockItem entity,
        InventoryStockItemListRowDto row,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var poItemId = entity.PurchaseOrderItemId?.Trim();
        if (string.IsNullOrEmpty(poItemId))
            return;

        var poItem = await _poItemRepo.GetByIdAsync(poItemId);
        if (poItem == null || poItem.IsDeleted)
            return;

        var po = string.IsNullOrWhiteSpace(poItem.PurchaseOrderId)
            ? null
            : await _poRepo.GetByIdAsync(poItem.PurchaseOrderId.Trim());

        dto.PurchaseOrderItem = new StockItemFlowDocDto
        {
            Id = poItem.Id,
            DocCode = string.IsNullOrWhiteSpace(poItem.PurchaseOrderItemCode)
                ? row.PurchaseOrderItemCode
                : poItem.PurchaseOrderItemCode,
            Status = poItem.Status,
            CreateTime = poItem.CreateTime,
            VendorName = row.VendorName,
            VendorCode = row.VendorCode,
            PersonName = FirstNonEmpty(po?.PurchaseUserName, row.PurchaserName),
            UnitPrice = poItem.Cost,
            Currency = poItem.Currency,
            Qty = poItem.Qty,
            PurchaseOrderId = poItem.PurchaseOrderId,
            PurchaseOrderItemId = poItem.Id
        };
    }

    private async Task FillStockInAndQcAsync(
        StockItemFlowAggregatesDto dto,
        StockItem entity,
        CancellationToken cancellationToken)
    {
        var stockInId = entity.StockInId?.Trim();
        if (string.IsNullOrEmpty(stockInId))
            return;

        var stockIn = await _stockInRepo.GetByIdAsync(stockInId);
        if (stockIn == null)
            return;

        var creatorIds = new List<string>();
        AddId(creatorIds, stockIn.CreateByUserId);
        AddId(creatorIds, stockIn.CreatedBy);

        QCInfo? qc = null;
        var qcId = stockIn.QcId?.Trim();
        if (!string.IsNullOrEmpty(qcId))
        {
            qc = await _qcRepo.GetByIdAsync(qcId);
            if (qc != null)
                AddId(creatorIds, qc.CreateByUserId);
        }

        var users = await LoadUsersAsync(creatorIds);

        string? customsDeclarationId = null;
        string? customsDeclarationCode = null;
        if (stockIn.StockInType == StockInTypeCode.Customs)
        {
            var notifyKey = !string.IsNullOrWhiteSpace(stockIn.SourceId)
                ? stockIn.SourceId.Trim()
                : qc?.StockInNotifyId?.Trim();
            if (!string.IsNullOrEmpty(notifyKey))
            {
                var traceMap = await _customsTraceQuery.GetByStockInNotifyIdsAsync(
                    new[] { notifyKey },
                    cancellationToken);
                if (traceMap.TryGetValue(notifyKey, out var trace))
                {
                    customsDeclarationId = EmptyToNull(trace.CustomsDeclarationId);
                    customsDeclarationCode = EmptyToNull(trace.CustomsDeclarationCode);
                }
            }
        }

        dto.StockIn = new StockItemFlowDocDto
        {
            Id = stockIn.Id,
            DocCode = stockIn.StockInCode,
            Status = stockIn.Status,
            CreateTime = stockIn.CreateTime,
            BizDate = stockIn.StockInDate,
            PersonName = UserDisplay(users, stockIn.CreateByUserId) ?? UserDisplay(users, stockIn.CreatedBy),
            Qty = entity.QtyInbound,
            StockInType = stockIn.StockInType,
            CustomsDeclarationId = customsDeclarationId,
            CustomsDeclarationCode = customsDeclarationCode
        };
        dto.StockItem.CustomsDeclarationId = customsDeclarationId;
        dto.StockItem.CustomsDeclarationCode = customsDeclarationCode;

        if (qc == null)
            return;

        dto.Qc = new StockItemFlowDocDto
        {
            Id = qc.Id,
            DocCode = qc.QcCode,
            Status = qc.Status,
            CreateTime = qc.CreateTime,
            PersonName = UserDisplay(users, qc.CreateByUserId),
            PassQty = qc.PassQty,
            RejectQty = qc.RejectQty,
            Qty = qc.PassQty,
            StockInNotifyId = qc.StockInNotifyId
        };
    }

    private async Task FillDownstreamIntoSliceAsync(
        StockItemFlowDownstreamSliceDto dto,
        string stockItemId,
        InventoryStockItemListRowDto row,
        CancellationToken cancellationToken)
    {
        var packingById = new Dictionary<string, PackingItem>(StringComparer.OrdinalIgnoreCase);

        var packingDirect = (await _packingItemRepo.FindAsync(pi =>
                pi.StockItemId == stockItemId && !pi.IsDeleted)).ToList();
        foreach (var pi in packingDirect)
            packingById[pi.Id] = pi;

        var pickingLines = (await _pickingItemRepo.FindAsync(p =>
                p.StockItemId == stockItemId && !p.IsDeleted)).ToList();
        var extraPackingIds = pickingLines
            .Select(p => p.PackingItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x) && !packingById.ContainsKey(x!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (extraPackingIds.Count > 0)
        {
            var extra = (await _packingItemRepo.FindAsync(pi =>
                    extraPackingIds.Contains(pi.Id) && !pi.IsDeleted)).ToList();
            foreach (var pi in extra)
                packingById[pi.Id] = pi;
        }

        var packingItems = packingById.Values.ToList();
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

        var notifyIds = packingItems
            .Select(pi => pi.StockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var notifies = notifyIds.Count == 0
            ? new List<StockOutRequest>()
            : (await _notifyRepo.FindAsync(n => notifyIds.Contains(n.Id) && !n.IsDeleted)).ToList();

        var extendRows = (await _stockOutExtendRepo.FindAsync(x =>
                x.StockItemId == stockItemId && !x.IsDeleted)).ToList();
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

        // 移库虚拟出库等：明细直挂 StockItemId，无 stock_out_item_extend，流程站需一并展示。
        var directOutItems = (await _stockOutItemRepo.FindAsync(i =>
                i.StockItemId == stockItemId && !i.IsDeleted)).ToList();
        foreach (var line in directOutItems)
        {
            if (!outItemMap.ContainsKey(line.Id))
            {
                outItems.Add(line);
                outItemMap[line.Id] = line;
            }
        }

        var stockOutIds = outItems
            .Select(i => i.StockOutId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var stockOuts = stockOutIds.Count == 0
            ? new List<StockOut>()
            : (await _stockOutRepo.FindAsync(s => stockOutIds.Contains(s.Id) && !s.IsDeleted)).ToList();

        var userIds = new List<string>();
        foreach (var p in packings)
            AddId(userIds, p.SalesId);
        foreach (var p in packings)
            AddId(userIds, p.CreateByUserId);
        foreach (var s in stockOuts)
            AddId(userIds, s.CreateByUserId);
        var users = await LoadUsersAsync(userIds);

        var customerIds = new List<string>();
        AddId(customerIds, row.CustomerId);
        foreach (var n in notifies)
            AddId(customerIds, n.CustomerId);
        foreach (var p in packings)
            AddId(customerIds, p.CustomerId);
        var customers = await LoadCustomersAsync(customerIds);

        var customerCode = CustomerCodeOf(customers, row.CustomerId);

        var outTraceNotifyIds = new List<string>();
        foreach (var n in notifies)
        {
            if (StockOutTypeCode.NormalizeForNotify(n.StockOutType) == StockOutTypeCode.Customs)
                AddId(outTraceNotifyIds, n.Id);
        }
        foreach (var s in stockOuts)
        {
            if (StockOutTypeCode.NormalizeForNotify(s.StockOutType) == StockOutTypeCode.Customs)
                AddId(outTraceNotifyIds, s.SourceId);
        }
        foreach (var p in packings)
        {
            if (StockOutTypeCode.NormalizeForNotify(p.StockOutType) != StockOutTypeCode.Customs)
                continue;
            foreach (var pi in packingItems)
            {
                if (string.Equals(pi.PackingId?.Trim(), p.Id, StringComparison.OrdinalIgnoreCase))
                    AddId(outTraceNotifyIds, pi.StockOutNotifyId);
            }
        }
        var outTrace = outTraceNotifyIds.Count == 0
            ? (IReadOnlyDictionary<string, CustomsTraceLinkDto>)new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase)
            : await _customsTraceQuery.GetByStockOutNotifyIdsAsync(outTraceNotifyIds, cancellationToken);
        var packingDeclCode = await LoadPackingDeclarationCodesAsync(packings, outTrace, cancellationToken);

        dto.StockOutNotifies = notifies
            .OrderBy(n => n.CreateTime)
            .Select(n =>
            {
                var layerQty = packingItems
                    .Where(pi => string.Equals(pi.StockOutNotifyId?.Trim(), n.Id, StringComparison.OrdinalIgnoreCase))
                    .Sum(pi => pi.Qty);
                var packing = FirstPackingForNotify(packingItems, packingMap, n.Id);
                var doc = new StockItemFlowDocDto
                {
                    Id = n.Id,
                    DocCode = n.RequestCode,
                    Status = n.Status,
                    CreateTime = n.CreateTime,
                    CustomerName = CustomerNameOf(customers, n.CustomerId) ?? row.CustomerName,
                    CustomerCode = CustomerCodeOf(customers, n.CustomerId) ?? customerCode,
                    PersonName = packing != null
                        ? UserDisplay(users, packing.SalesId) ?? row.SalespersonName
                        : row.SalespersonName,
                    Qty = layerQty,
                    StockOutType = n.StockOutType
                };
                ApplyStockOutCustoms(doc, n.StockOutType, n.Id, packing?.CustomsDeclarationId, outTrace, packingDeclCode);
                return doc;
            })
            .ToList();

        dto.Packings = packings
            .OrderBy(p => p.CreateTime)
            .Select(p =>
            {
                var layerQty = packingItems
                    .Where(pi => string.Equals(pi.PackingId?.Trim(), p.Id, StringComparison.OrdinalIgnoreCase))
                    .Sum(pi => pi.Qty);
                var notifyId = packingItems
                    .Select(pi =>
                        string.Equals(pi.PackingId?.Trim(), p.Id, StringComparison.OrdinalIgnoreCase)
                            ? pi.StockOutNotifyId
                            : null)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                var doc = new StockItemFlowDocDto
                {
                    Id = p.Id,
                    DocCode = p.Code,
                    Status = p.Status,
                    CreateTime = p.CreateTime,
                    CustomerName = CustomerNameOf(customers, p.CustomerId) ?? row.CustomerName,
                    CustomerCode = CustomerCodeOf(customers, p.CustomerId) ?? customerCode,
                    PersonName = UserDisplay(users, p.SalesId) ?? row.SalespersonName,
                    Qty = layerQty,
                    StockOutType = p.StockOutType
                };
                ApplyStockOutCustoms(doc, p.StockOutType, notifyId, p.CustomsDeclarationId, outTrace, packingDeclCode);
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
                    layerQty = outItems
                        .Where(i => string.Equals(i.StockOutId?.Trim(), s.Id, StringComparison.OrdinalIgnoreCase))
                        .Sum(i => i.ActualQty > 0 ? i.ActualQty : i.Quantity);
                }
                var doc = new StockItemFlowDocDto
                {
                    Id = s.Id,
                    DocCode = s.StockOutCode,
                    Status = s.Status,
                    CreateTime = s.CreateTime,
                    CustomerName = s.StockOutType == StockOutTypeCode.Transfer ? null : row.CustomerName,
                    CustomerCode = s.StockOutType == StockOutTypeCode.Transfer ? null : customerCode,
                    PersonName = s.StockOutType == StockOutTypeCode.Transfer
                        ? null
                        : UserDisplay(users, s.CreateByUserId) ?? row.SalespersonName,
                    Qty = layerQty,
                    StockOutType = s.StockOutType
                };
                ApplyStockOutCustoms(doc, s.StockOutType, s.SourceId, null, outTrace, packingDeclCode);
                return doc;
            })
            .ToList();
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
            var id = EmptyToNull(trace.CustomsDeclarationId);
            var code = EmptyToNull(trace.CustomsDeclarationCode);
            if (id == null || code == null || result.ContainsKey(id))
                continue;
            result[id] = code;
        }

        var missingIds = packings
            .Select(p => EmptyToNull(p.CustomsDeclarationId))
            .Where(id => !string.IsNullOrEmpty(id) && !result.ContainsKey(id!))
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

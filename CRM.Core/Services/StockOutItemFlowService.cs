using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models;
using CRM.Core.Models.Customer;
using CRM.Core.Models.Finance;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public class StockOutItemFlowService : IStockOutItemFlowService
{
    private readonly IInventoryStockItemListQuery _stockItemListQuery;
    private readonly IDataPermissionService _dataPermission;
    private readonly IRepository<StockOutItem> _stockOutItemRepo;
    private readonly IRepository<StockOutItemExtend> _stockOutExtendRepo;
    private readonly IRepository<StockOut> _stockOutRepo;
    private readonly IRepository<StockOutRequest> _notifyRepo;
    private readonly IRepository<SellOrder> _sellOrderRepo;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepo;
    private readonly IRepository<Packing> _packingRepo;
    private readonly IRepository<PackingItem> _packingItemRepo;
    private readonly IRepository<PickingTaskItem> _pickingItemRepo;
    private readonly IRepository<StockItem> _stockItemRepo;
    private readonly IRepository<StockIn> _stockInRepo;
    private readonly IRepository<CustomerInfo> _customerRepo;
    private readonly IRepository<User> _userRepo;
    private readonly ICustomsTraceQuery _customsTraceQuery;
    private readonly IRepository<FinanceReceivable> _receivableRepo;
    private readonly IFinanceReceivableService _financeReceivableService;

    public StockOutItemFlowService(
        IInventoryStockItemListQuery stockItemListQuery,
        IDataPermissionService dataPermission,
        IRepository<StockOutItem> stockOutItemRepo,
        IRepository<StockOutItemExtend> stockOutExtendRepo,
        IRepository<StockOut> stockOutRepo,
        IRepository<StockOutRequest> notifyRepo,
        IRepository<SellOrder> sellOrderRepo,
        IRepository<SellOrderItem> sellOrderItemRepo,
        IRepository<Packing> packingRepo,
        IRepository<PackingItem> packingItemRepo,
        IRepository<PickingTaskItem> pickingItemRepo,
        IRepository<StockItem> stockItemRepo,
        IRepository<StockIn> stockInRepo,
        IRepository<CustomerInfo> customerRepo,
        IRepository<User> userRepo,
        ICustomsTraceQuery customsTraceQuery,
        IRepository<FinanceReceivable> receivableRepo,
        IFinanceReceivableService financeReceivableService)
    {
        _stockItemListQuery = stockItemListQuery;
        _dataPermission = dataPermission;
        _stockOutItemRepo = stockOutItemRepo;
        _stockOutExtendRepo = stockOutExtendRepo;
        _stockOutRepo = stockOutRepo;
        _notifyRepo = notifyRepo;
        _sellOrderRepo = sellOrderRepo;
        _sellOrderItemRepo = sellOrderItemRepo;
        _packingRepo = packingRepo;
        _packingItemRepo = packingItemRepo;
        _pickingItemRepo = pickingItemRepo;
        _stockItemRepo = stockItemRepo;
        _stockInRepo = stockInRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _customsTraceQuery = customsTraceQuery;
        _receivableRepo = receivableRepo;
        _financeReceivableService = financeReceivableService;
    }

    public async Task<StockOutItemFlowAggregatesDto> GetFlowAggregatesAsync(
        string stockOutItemId,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var id = stockOutItemId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("出库明细ID不能为空", nameof(stockOutItemId));

        var item = await _stockOutItemRepo.GetByIdAsync(id);
        if (item == null || item.IsDeleted)
            throw new InvalidOperationException("出库明细不存在或无权查看");

        var header = await _stockOutRepo.GetByIdAsync(item.StockOutId);
        if (header == null || header.IsDeleted)
            throw new InvalidOperationException("出库单不存在或无权查看");

        var extend = await _stockOutExtendRepo.GetByIdAsync(id);

        PickingTaskItem? picking = null;
        var pickId = EmptyToNull(item.PickingTaskItemId);
        if (pickId != null)
            picking = await _pickingItemRepo.GetByIdAsync(pickId);

        PackingItem? packingLine = null;
        var packingLineId = EmptyToNull(picking?.PackingItemId);
        if (packingLineId != null)
            packingLine = await _packingItemRepo.GetByIdAsync(packingLineId);

        var packingId = EmptyToNull(item.PackingId) ?? EmptyToNull(packingLine?.PackingId);
        Packing? packing = null;
        if (packingId != null)
            packing = await _packingRepo.GetByIdAsync(packingId);

        if (packingLine == null && packingId != null)
        {
            var packingItems = (await _packingItemRepo.FindAsync(pi =>
                !pi.IsDeleted && pi.PackingId == packingId)).ToList();
            packingLine = PickPackingLine(packingItems, item, extend);
        }

        var sellLineId = FirstNonEmpty(
            extend?.SellOrderItemId,
            packingLine?.SellOrderItemId,
            header.SellOrderItemId);
        SellOrderItem? sellLine = null;
        if (sellLineId != null)
            sellLine = await _sellOrderItemRepo.GetByIdAsync(sellLineId);

        SellOrder? sellOrder = null;
        var sellOrderId = FirstNonEmpty(sellLine?.SellOrderId, packingLine?.SellOrderId);
        if (sellOrderId != null)
            sellOrder = await _sellOrderRepo.GetByIdAsync(sellOrderId);

        if (sellOrder != null && !string.IsNullOrWhiteSpace(currentUserId))
        {
            if (!await _dataPermission.CanAccessSalesOrderAsync(currentUserId.Trim(), sellOrder))
                throw new InvalidOperationException("出库明细不存在或无权查看");
        }

        StockOutRequest? notify = null;
        var notifyId = EmptyToNull(packingLine?.StockOutNotifyId);
        if (notifyId != null)
            notify = await _notifyRepo.GetByIdAsync(notifyId);
        if ((notify == null || notify.IsDeleted) && sellLineId != null)
            notify = await ResolveNotifyForSellLineAsync(sellLineId);

        var dto = new StockOutItemFlowAggregatesDto { StockOutItemId = id };

        var userIds = new List<string>();
        AddId(userIds, sellOrder?.SalesUserId);
        AddId(userIds, notify?.CreateByUserId);
        AddId(userIds, notify?.RequestUserId);
        AddId(userIds, packing?.SalesId);
        AddId(userIds, packing?.CreateByUserId);
        AddId(userIds, header.CreateByUserId);

        var customerIds = new List<string>();
        AddId(customerIds, notify?.CustomerId);
        AddId(customerIds, sellOrder?.CustomerId);
        AddId(customerIds, packing?.CustomerId);
        AddId(customerIds, header.CustomerId);

        var users = await LoadUsersAsync(userIds);
        var customers = await LoadCustomersAsync(customerIds);

        dto.SellOrderItem = sellLine == null || sellLine.IsDeleted
            ? null
            : MapSellOrderItem(sellLine, sellOrder, customers, users);

        if (notify != null && !notify.IsDeleted)
            dto.StockOutNotify = MapNotify(notify, sellOrder, packing, customers, users);

        var stockItemIds = new List<string>();
        AddId(stockItemIds, item.StockItemId);
        AddId(stockItemIds, extend?.StockItemId);
        AddId(stockItemIds, picking?.StockItemId);
        AddId(stockItemIds, packingLine?.StockItemId);
        dto.StockItems = await MapStockItemDocsAsync(stockItemIds, currentUserId, cancellationToken);
        await ApplyInboundCustomsAsync(dto.StockItems, cancellationToken);

        var outTraceNotifyIds = new List<string>();
        if (notify != null && StockOutTypeCode.NormalizeForNotify(notify.StockOutType) == StockOutTypeCode.Customs)
            AddId(outTraceNotifyIds, notify.Id);
        if (StockOutTypeCode.NormalizeForNotify(header.StockOutType) == StockOutTypeCode.Customs)
            AddId(outTraceNotifyIds, header.SourceId);
        if (notify != null
            && packing != null
            && StockOutTypeCode.NormalizeForNotify(packing.StockOutType) == StockOutTypeCode.Customs)
            AddId(outTraceNotifyIds, notify.Id);

        var outTrace = outTraceNotifyIds.Count == 0
            ? (IReadOnlyDictionary<string, CustomsTraceLinkDto>)new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase)
            : await _customsTraceQuery.GetByStockOutNotifyIdsAsync(outTraceNotifyIds, cancellationToken);
        var packingDeclCode = packing == null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await LoadPackingDeclarationCodesAsync(new List<Packing> { packing }, outTrace, cancellationToken);

        if (dto.StockOutNotify != null && notify != null)
        {
            ApplyStockOutCustoms(
                dto.StockOutNotify,
                notify.StockOutType,
                notify.Id,
                packing?.CustomsDeclarationId,
                outTrace,
                packingDeclCode);
        }

        var outboundType = header.StockOutType != 0
            ? header.StockOutType
            : packing?.StockOutType ?? notify?.StockOutType ?? (short)0;
        foreach (var layer in dto.StockItems)
            layer.StockOutType = outboundType;

        if (packing != null && !packing.IsDeleted)
        {
            var packingDoc = new StockItemFlowDocDto
            {
                Id = packing.Id,
                DocCode = packing.Code,
                Status = packing.Status,
                CreateTime = packing.CreateTime,
                CustomerName = CustomerNameOf(customers, packing.CustomerId)
                    ?? CustomerNameOf(customers, header.CustomerId)
                    ?? CustomerNameOf(customers, notify?.CustomerId),
                CustomerCode = CustomerCodeOf(customers, packing.CustomerId)
                    ?? CustomerCodeOf(customers, header.CustomerId)
                    ?? CustomerCodeOf(customers, notify?.CustomerId),
                PersonName = UserDisplay(users, packing.SalesId)
                    ?? UserDisplay(users, sellOrder?.SalesUserId),
                Qty = packingLine?.Qty ?? LineQty(item),
                StockOutType = packing.StockOutType
            };
            ApplyStockOutCustoms(
                packingDoc,
                packing.StockOutType,
                notify?.Id,
                packing.CustomsDeclarationId,
                outTrace,
                packingDeclCode);
            dto.Packings.Add(packingDoc);
        }

        var outDoc = new StockItemFlowDocDto
        {
            Id = header.Id,
            DocCode = header.StockOutCode,
            LineDocCode = EmptyToNull(item.StockOutItemCode),
            Status = header.Status,
            CreateTime = item.CreateTime,
            CustomerName = CustomerNameOf(customers, header.CustomerId)
                ?? CustomerNameOf(customers, packing?.CustomerId)
                ?? CustomerNameOf(customers, notify?.CustomerId),
            CustomerCode = CustomerCodeOf(customers, header.CustomerId)
                ?? CustomerCodeOf(customers, packing?.CustomerId)
                ?? CustomerCodeOf(customers, notify?.CustomerId),
            PersonName = UserDisplay(users, header.CreateByUserId)
                ?? UserDisplay(users, sellOrder?.SalesUserId),
            Qty = LineQty(item),
            StockOutType = outboundType
        };
        ApplyStockOutCustoms(
            outDoc,
            outboundType,
            notify?.Id,
            packing?.CustomsDeclarationId,
            outTrace,
            packingDeclCode);
        dto.StockOuts.Add(outDoc);

        await AppendFinanceStationsAsync(dto, header, sellLineId, customers, currentUserId, cancellationToken);

        return dto;
    }

    private async Task AppendFinanceStationsAsync(
        StockOutItemFlowAggregatesDto dto,
        StockOut header,
        string? sellLineId,
        Dictionary<string, CustomerInfo> customers,
        string? currentUserId,
        CancellationToken cancellationToken)
    {
        var lineId = EmptyToNull(sellLineId);
        if (lineId == null || !StockOutTypeCode.IsSalesStockOut(header.StockOutType))
            return;

        var receivables = (await _receivableRepo.FindAsync(r =>
                !r.IsDeleted
                && r.StockOutId == header.Id
                && r.SellOrderItemId == lineId))
            .OrderBy(r => r.ReceivableCode)
            .ThenBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (receivables.Count == 0)
            return;

        var receivable = receivables[0];
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            if (!await _dataPermission.CanAccessFinanceReceivableAsync(currentUserId.Trim(), receivable))
                return;
        }

        var itemCodes = await ResolveStockOutItemCodesForSellLineAsync(header, lineId);
        dto.Receivables.Add(new StockOutItemFlowReceivableDto
        {
            Id = receivable.Id,
            ReceivableCode = receivable.ReceivableCode,
            VerificationStatus = receivable.VerificationStatus,
            Amount = receivable.Amount,
            VerifiedToBe = receivable.VerifiedToBe,
            Currency = receivable.Currency,
            StockOutDate = receivable.StockOutDate,
            CreateTime = receivable.CreateTime,
            CustomerId = receivable.CustomerId,
            CustomerName = FirstNonEmpty(
                CustomerNameOf(customers, receivable.CustomerId),
                receivable.CustomerName),
            CustomerCode = CustomerCodeOf(customers, receivable.CustomerId),
            StockOutItemLineCount = itemCodes.Count,
            StockOutItemCodes = itemCodes
        });

        var writeOffs = await _financeReceivableService.GetWriteOffLedgerBySellOrderItemIdsAsync(
            new[] { lineId },
            currentUserId,
            cancellationToken);
        foreach (var row in writeOffs
                     .Where(w => string.Equals(w.StockOutId?.Trim(), header.Id, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(w => w.CreateTime))
        {
            dto.ReceiptWriteOffs.Add(new StockOutItemFlowReceiptWriteOffDto
            {
                Id = row.Id,
                Amount = row.Amount,
                Currency = row.Currency,
                CreateTime = row.CreateTime,
                FinanceReceiptId = row.FinanceReceiptId,
                FinanceReceiptCode = row.FinanceReceiptCode,
                ReceivableCode = row.ReceivableCode,
                CustomerName = row.CustomerName,
                OperatorUserName = row.OperatorUserName
            });
        }
    }

    private async Task<List<string>> ResolveStockOutItemCodesForSellLineAsync(StockOut header, string sellLineId)
    {
        var items = (await _stockOutItemRepo.FindAsync(i =>
                !i.IsDeleted && i.StockOutId == header.Id))
            .ToList();
        if (items.Count == 0)
            return new List<string>();

        var itemIds = items.Select(i => i.Id.Trim()).ToList();
        var extends = (await _stockOutExtendRepo.FindAsync(e =>
                !e.IsDeleted && itemIds.Contains(e.Id)))
            .ToList();
        var extByItemId = extends
            .GroupBy(e => e.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var codes = new List<string>();
        foreach (var item in items)
        {
            var extLineId = extByItemId.TryGetValue(item.Id.Trim(), out var ext)
                ? EmptyToNull(ext.SellOrderItemId)
                : null;
            if (extLineId == null)
                continue;
            if (!string.Equals(extLineId, sellLineId, StringComparison.OrdinalIgnoreCase))
                continue;
            var code = EmptyToNull(item.StockOutItemCode);
            if (code != null)
                codes.Add(code);
        }

        if (codes.Count == 0
            && string.Equals(EmptyToNull(header.SellOrderItemId), sellLineId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var item in items)
            {
                var code = EmptyToNull(item.StockOutItemCode);
                if (code != null)
                    codes.Add(code);
            }
        }

        return codes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PackingItem? PickPackingLine(
        List<PackingItem> packingItems,
        StockOutItem item,
        StockOutItemExtend? extend)
    {
        if (packingItems.Count == 0)
            return null;
        var stockItemId = FirstNonEmpty(item.StockItemId, extend?.StockItemId);
        if (stockItemId != null)
        {
            var byStock = packingItems.FirstOrDefault(pi =>
                string.Equals(pi.StockItemId?.Trim(), stockItemId, StringComparison.OrdinalIgnoreCase));
            if (byStock != null)
                return byStock;
        }
        var sellLineId = EmptyToNull(extend?.SellOrderItemId);
        if (sellLineId != null)
        {
            var bySell = packingItems.FirstOrDefault(pi =>
                string.Equals(pi.SellOrderItemId?.Trim(), sellLineId, StringComparison.OrdinalIgnoreCase));
            if (bySell != null)
                return bySell;
        }
        return packingItems.Count == 1 ? packingItems[0] : null;
    }

    private async Task<StockOutRequest?> ResolveNotifyForSellLineAsync(string sellLineId)
    {
        var list = (await _notifyRepo.FindAsync(r =>
            !r.IsDeleted && r.SalesOrderItemId == sellLineId)).ToList();
        if (list.Count == 0)
            return null;
        return list
            .OrderBy(r => r.Status == StockOutRequestStatusCode.StockedOut ? 0 : 1)
            .ThenBy(r => r.Status == StockOutRequestStatusCode.Packed ? 1 : 2)
            .ThenBy(r => r.RequestCode)
            .First();
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
        foreach (var layerId in orderedIds)
        {
            if (!byId.TryGetValue(layerId, out var row))
                continue;
            result.Add(new StockItemFlowDocDto
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
            });
        }
        return result;
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
            CustomerName = FirstNonEmpty(CustomerNameOf(customers, customerId), order?.CustomerName),
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

    private static int LineQty(StockOutItem item)
    {
        return item.ActualQty > 0 ? item.ActualQty : item.Quantity;
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

        var packingDecl = EmptyToNull(packingDeclarationId);
        if (!string.IsNullOrEmpty(packingDecl))
        {
            dto.CustomsDeclarationId = packingDecl;
            if (packingDeclCode.TryGetValue(packingDecl, out var packingCode))
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

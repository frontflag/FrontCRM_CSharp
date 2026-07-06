using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customs;

public sealed class CustomsDeclarationBusinessRecordsQuery : ICustomsDeclarationBusinessRecordsQuery
{
    private readonly ApplicationDbContext _db;
    private readonly ISalesOrderService _salesOrderService;
    private readonly IStockOutService _stockOutService;
    private readonly IPackingService _packingService;
    private readonly IArrivalNoticeListQuery _arrivalNoticeListQuery;
    private readonly IPurchaseOrderItemListQuery _purchaseOrderItemListQuery;

    public CustomsDeclarationBusinessRecordsQuery(
        ApplicationDbContext db,
        ISalesOrderService salesOrderService,
        IStockOutService stockOutService,
        IPackingService packingService,
        IArrivalNoticeListQuery arrivalNoticeListQuery,
        IPurchaseOrderItemListQuery purchaseOrderItemListQuery)
    {
        _db = db;
        _salesOrderService = salesOrderService;
        _stockOutService = stockOutService;
        _packingService = packingService;
        _arrivalNoticeListQuery = arrivalNoticeListQuery;
        _purchaseOrderItemListQuery = purchaseOrderItemListQuery;
    }

    public async Task<CustomsDeclarationBusinessRecordsDto?> LoadAsync(
        string declarationId,
        CancellationToken cancellationToken = default)
    {
        var key = declarationId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return null;

        var dec = await _db.CustomsDeclarations.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == key && !d.IsDeleted, cancellationToken);
        if (dec == null)
            return null;

        var items = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => i.DeclarationId == key && !i.IsDeleted)
            .OrderBy(i => i.LineNo)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return new CustomsDeclarationBusinessRecordsDto();

        var salesSorIds = items
            .Select(i => i.StockOutRequestId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var customsSorIds = items
            .Select(i => i.CustomsStockOutNotifyId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var cdiIds = items.Select(i => i.Id.Trim()).ToList();
        var allSorIds = salesSorIds
            .Concat(customsSorIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sors = allSorIds.Count == 0
            ? new List<StockOutRequest>()
            : await _db.StockOutRequests.AsNoTracking()
                .Where(s => allSorIds.Contains(s.Id))
                .ToListAsync(cancellationToken);
        var sorById = sors.ToDictionary(s => s.Id.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

        var packingItemIds = items
            .Select(i => i.PackingItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pendlistIds = items
            .Select(i => i.CustomsPendlistId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var packingItems = packingItemIds.Count == 0
            ? new List<PackingItem>()
            : await _db.PackingItems.AsNoTracking()
                .Where(pi => !pi.IsDeleted && packingItemIds.Contains(pi.Id))
                .ToListAsync(cancellationToken);
        var pendlists = pendlistIds.Count == 0
            ? new List<CustomsPendlist>()
            : await _db.CustomsPendlists.AsNoTracking()
                .Where(p => !p.IsDeleted && pendlistIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
        var packingItemById = packingItems.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
        var pendlistById = pendlists.ToDictionary(p => p.Id.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var sellLineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.SellOrderItemId))
                sellLineIds.Add(item.SellOrderItemId.Trim());

            var sorKey = item.StockOutRequestId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(sorKey) && sorById.TryGetValue(sorKey, out var sor)
                && !string.IsNullOrWhiteSpace(sor.SalesOrderItemId))
            {
                sellLineIds.Add(sor.SalesOrderItemId.Trim());
            }

            var piKey = item.PackingItemId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(piKey) && packingItemById.TryGetValue(piKey, out var pi)
                && !string.IsNullOrWhiteSpace(pi.SellOrderItemId))
            {
                sellLineIds.Add(pi.SellOrderItemId.Trim());
            }

            var plKey = item.CustomsPendlistId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(plKey) && pendlistById.TryGetValue(plKey, out var pl)
                && !string.IsNullOrWhiteSpace(pl.SellOrderItemId))
            {
                sellLineIds.Add(pl.SellOrderItemId.Trim());
            }
        }

        var sellLineIdList = sellLineIds.ToList();
        var sellLinesById = sellLineIdList.Count == 0
            ? new List<SellOrderItem>()
            : await _db.SellOrderItems.AsNoTracking()
                .Where(i => sellLineIdList.Contains(i.Id))
                .ToListAsync(cancellationToken);

        var sellLineCodes = items
            .Select(i => i.SellOrderItemCode?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        var resolvedCodes = new HashSet<string>(
            sellLinesById
                .Select(l => l.SellOrderItemCode?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))!,
            StringComparer.OrdinalIgnoreCase);
        var missingCodes = sellLineCodes
            .Where(c => !resolvedCodes.Contains(c))
            .ToList();

        var sellLinesByCode = new List<SellOrderItem>();
        if (missingCodes.Count > 0)
        {
            sellLinesByCode = await _db.SellOrderItems.AsNoTracking()
                .Where(i => i.SellOrderItemCode != null && missingCodes.Contains(i.SellOrderItemCode))
                .ToListAsync(cancellationToken);

            var stillMissing = missingCodes
                .Where(c => !sellLinesByCode.Any(l =>
                    string.Equals(l.SellOrderItemCode?.Trim(), c, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (stillMissing.Count > 0)
            {
                foreach (var code in stillMissing)
                {
                    var line = await _db.SellOrderItems.AsNoTracking()
                        .FirstOrDefaultAsync(
                            i => i.SellOrderItemCode != null
                                 && EF.Functions.ILike(i.SellOrderItemCode, code),
                            cancellationToken);
                    if (line != null)
                        sellLinesByCode.Add(line);
                }
            }
        }

        var sellLines = sellLinesById
            .Concat(sellLinesByCode)
            .GroupBy(l => l.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        var sellLineById = sellLines.ToDictionary(l => l.Id.Trim(), l => l, StringComparer.OrdinalIgnoreCase);
        var sellLineByCode = sellLines
            .Where(l => !string.IsNullOrWhiteSpace(l.SellOrderItemCode))
            .GroupBy(l => l.SellOrderItemCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var packingIdsFromItems = allSorIds.Count == 0
            ? new List<string>()
            : await _db.PackingItems.AsNoTracking()
                .Where(pi => !pi.IsDeleted && pi.StockOutNotifyId != null && allSorIds.Contains(pi.StockOutNotifyId))
                .Select(pi => pi.PackingId!)
                .Distinct()
                .ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(dec.PackingId)
            && !packingIdsFromItems.Contains(dec.PackingId.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            packingIdsFromItems.Add(dec.PackingId.Trim());
        }

        var packings = packingIdsFromItems.Count == 0
            ? new List<Packing>()
            : await _db.Packings.AsNoTracking()
                .Where(p => packingIdsFromItems.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

        var arrivalNotifies = await _db.StockInNotifies.AsNoTracking()
            .Where(n => !n.IsDeleted && n.CustomsDeclarationItemId != null && cdiIds.Contains(n.CustomsDeclarationItemId))
            .ToListAsync(cancellationToken);
        var notifyIds = arrivalNotifies.Select(n => n.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var qcStockInIds = notifyIds.Count == 0
            ? new List<string>()
            : await _db.QCInfos.AsNoTracking()
                .Where(q => !q.IsDeleted && q.StockInNotifyId != null && notifyIds.Contains(q.StockInNotifyId))
                .Select(q => q.StockInId)
                .Where(x => x != null)
                .Select(x => x!.Trim())
                .Distinct()
                .ToListAsync(cancellationToken);

        var customsStockIns = notifyIds.Count == 0 && qcStockInIds.Count == 0
            ? new List<StockIn>()
            : await _db.StockIns.AsNoTracking()
                .Where(s => !s.IsDeleted
                            && s.StockInType == StockInTypeCode.Customs
                            && ((s.SourceId != null && notifyIds.Contains(s.SourceId))
                                || qcStockInIds.Contains(s.Id)))
                .ToListAsync(cancellationToken);

        var customsStockOuts = customsSorIds.Count == 0
            ? new List<StockOut>()
            : await _db.StockOuts.AsNoTracking()
                .Where(o => !o.IsDeleted
                            && o.StockOutType == StockOutTypeCode.Customs
                            && o.SourceId != null
                            && customsSorIds.Contains(o.SourceId))
                .ToListAsync(cancellationToken);

        var salesStockOuts = salesSorIds.Count == 0
            ? new List<StockOut>()
            : await _db.StockOuts.AsNoTracking()
                .Where(o => !o.IsDeleted
                            && (o.StockOutType == StockOutTypeCode.Sales
                                || o.StockOutType == StockOutTypeCode.LegacySales)
                            && o.SourceId != null
                            && salesSorIds.Contains(o.SourceId))
                .ToListAsync(cancellationToken);

        var result = new CustomsDeclarationBusinessRecordsDto();
        var salesOrderItemIds = new List<string>();

        foreach (var cdi in items.OrderBy(i => i.LineNo))
            AddSalesOrderItemRow(result.SalesOrders, salesOrderItemIds, cdi, sellLineById, sellLineByCode, sorById, packingItemById, pendlistById);

        if (salesOrderItemIds.Count > 0)
        {
            var lines = await _salesOrderService.GetSellOrderItemLinesByIdsAsync(salesOrderItemIds, cancellationToken);
            var lineById = lines.ToDictionary(l => l.SellOrderItemId.Trim(), l => l, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in salesOrderItemIds)
            {
                var lineKey = id.Trim();
                if (!seen.Add(lineKey))
                    continue;
                if (lineById.TryGetValue(lineKey, out var line))
                    result.SalesOrderItems.Add(line);
            }
        }

        var stockOutNotifyIds = new List<string>();
        var customsStockOutNotifyIds = new List<string>();

        foreach (var sor in sors
                     .Where(s => StockOutTypeCode.NormalizeForNotify(s.StockOutType) == StockOutTypeCode.Sales)
                     .OrderByDescending(x => x.CreateTime))
        {
            if (AddRow(result.StockOutNotifies, sor.Id, sor.RequestCode, sor.Status, SorOccurredAt(sor)))
                stockOutNotifyIds.Add(sor.Id.Trim());
        }

        foreach (var sor in sors
                     .Where(s => StockOutTypeCode.NormalizeForNotify(s.StockOutType) == StockOutTypeCode.Customs)
                     .OrderByDescending(x => x.CreateTime))
        {
            if (AddRow(result.CustomsStockOutNotifies, sor.Id, sor.RequestCode, sor.Status, SorOccurredAt(sor)))
                customsStockOutNotifyIds.Add(sor.Id.Trim());
        }

        if (stockOutNotifyIds.Count > 0)
        {
            result.StockOutNotifyItems = await _stockOutService.GetStockOutRequestListItemsByIdsAsync(
                stockOutNotifyIds,
                cancellationToken);
        }

        if (customsStockOutNotifyIds.Count > 0)
        {
            result.CustomsStockOutNotifyItems = await _stockOutService.GetStockOutRequestListItemsByIdsAsync(
                customsStockOutNotifyIds,
                cancellationToken);
        }

        var customsPackingIds = new List<string>();
        foreach (var p in packings
                     .Where(p => StockOutTypeCode.NormalizeForNotify(p.StockOutType) == StockOutTypeCode.Customs)
                     .OrderByDescending(x => x.ModifyTime ?? x.CreateTime))
        {
            if (AddRow(result.CustomsPackings, p.Id, p.Code, p.Status, p.ModifyTime ?? p.CreateTime))
                customsPackingIds.Add(p.Id.Trim());
        }

        if (customsPackingIds.Count > 0)
        {
            result.CustomsPackingItems = await _packingService.GetPackingListItemsByIdsAsync(
                customsPackingIds,
                cancellationToken);
        }

        await PopulatePurchaseOrderItemsFromCustomsPackingAsync(
            result,
            customsPackingIds,
            cancellationToken);

        var customsStockOutIds = new List<string>();
        foreach (var o in customsStockOuts.OrderByDescending(x => x.StockOutDate ?? x.CreateTime))
        {
            if (AddRow(result.CustomsStockOuts, o.Id, o.StockOutCode, o.Status, o.StockOutDate ?? o.CreateTime))
                customsStockOutIds.Add(o.Id.Trim());
        }

        if (customsStockOutIds.Count > 0)
        {
            result.CustomsStockOutItems = await _stockOutService.GetStockOutListItemsByIdsAsync(
                customsStockOutIds,
                cancellationToken);
        }

        var customsArrivalNotifyIds = new List<string>();
        foreach (var n in arrivalNotifies.OrderByDescending(x => x.CreateTime))
        {
            if (AddRow(result.CustomsArrivalNotifies, n.Id, n.NoticeCode ?? n.Id, n.Status, n.CreateTime))
                customsArrivalNotifyIds.Add(n.Id.Trim());
        }

        if (customsArrivalNotifyIds.Count > 0)
        {
            result.CustomsArrivalNotifyItems = await _arrivalNoticeListQuery.GetByIdsAsync(
                customsArrivalNotifyIds,
                applyDataScope: false,
                cancellationToken: cancellationToken);
        }

        foreach (var si in customsStockIns.OrderByDescending(x => x.StockInDate != default ? x.StockInDate : x.CreateTime))
        {
            var at = si.StockInDate != default ? si.StockInDate : si.CreateTime;
            AddRow(result.CustomsStockIns, si.Id, si.StockInCode, si.Status, at);
        }

        var packingIds = new List<string>();
        foreach (var p in packings
                     .Where(p => StockOutTypeCode.NormalizeForNotify(p.StockOutType) == StockOutTypeCode.Sales)
                     .OrderByDescending(x => x.ModifyTime ?? x.CreateTime))
        {
            if (AddRow(result.Packings, p.Id, p.Code, p.Status, p.ModifyTime ?? p.CreateTime))
                packingIds.Add(p.Id.Trim());
        }

        if (packingIds.Count > 0)
        {
            result.PackingItems = await _packingService.GetPackingListItemsByIdsAsync(
                packingIds,
                cancellationToken);
        }

        var stockOutIds = new List<string>();
        foreach (var o in salesStockOuts.OrderByDescending(x => x.StockOutDate ?? x.CreateTime))
        {
            if (AddRow(result.StockOuts, o.Id, o.StockOutCode, o.Status, o.StockOutDate ?? o.CreateTime))
                stockOutIds.Add(o.Id.Trim());
        }

        if (stockOutIds.Count > 0)
        {
            result.StockOutItems = await _stockOutService.GetStockOutListItemsByIdsAsync(
                stockOutIds,
                cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// 采购订单明细：报关装箱单 → 拣货单 → 在库明细（对应入库明细）→ 采购订单明细。
    /// </summary>
    private async Task PopulatePurchaseOrderItemsFromCustomsPackingAsync(
        CustomsDeclarationBusinessRecordsDto result,
        IReadOnlyList<string> customsPackingIds,
        CancellationToken cancellationToken)
    {
        if (customsPackingIds.Count == 0)
            return;

        var pickingTaskIds = await _db.PickingTasks.AsNoTracking()
            .Where(pt => !pt.IsDeleted
                         && pt.PackingId != null
                         && customsPackingIds.Contains(pt.PackingId))
            .Select(pt => pt.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (pickingTaskIds.Count == 0)
            return;

        var stockItemIds = await _db.PickingTaskItems.AsNoTracking()
            .Where(pi => !pi.IsDeleted
                         && pickingTaskIds.Contains(pi.PickingTaskId)
                         && pi.StockItemId != null)
            .Select(pi => pi.StockItemId!)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (stockItemIds.Count == 0)
            return;

        var stockItems = await _db.StockItems.AsNoTracking()
            .Where(si => !si.IsDeleted && stockItemIds.Contains(si.Id))
            .Select(si => new { si.PurchaseOrderItemId, si.StockInItemId })
            .ToListAsync(cancellationToken);

        var purchaseOrderItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stockInItemIdsForExtend = new List<string>();

        foreach (var si in stockItems)
        {
            var poItemId = si.PurchaseOrderItemId?.Trim();
            if (!string.IsNullOrEmpty(poItemId))
            {
                purchaseOrderItemIds.Add(poItemId);
                continue;
            }

            var stockInItemId = si.StockInItemId?.Trim();
            if (!string.IsNullOrEmpty(stockInItemId))
                stockInItemIdsForExtend.Add(stockInItemId);
        }

        if (stockInItemIdsForExtend.Count > 0)
        {
            var fromExtend = await _db.StockInItemExtends.AsNoTracking()
                .Where(e => !e.IsDeleted
                            && stockInItemIdsForExtend.Contains(e.Id)
                            && e.PurchaseOrderItemId != null)
                .Select(e => e.PurchaseOrderItemId!)
                .ToListAsync(cancellationToken);
            foreach (var id in fromExtend)
            {
                var key = id.Trim();
                if (!string.IsNullOrEmpty(key))
                    purchaseOrderItemIds.Add(key);
            }
        }

        if (purchaseOrderItemIds.Count == 0)
            return;

        var orderedIds = purchaseOrderItemIds.ToList();
        var rawLines = await _purchaseOrderItemListQuery.GetByIdsAsync(
            orderedIds,
            currentUserId: null,
            applyDataScope: false,
            cancellationToken: cancellationToken);
        if (rawLines.Count == 0)
            return;

        var mapped = await MapPurchaseOrderItemListLinesAsync(rawLines, cancellationToken);
        var mappedById = mapped.ToDictionary(
            x => x.PurchaseOrderItemId.Trim(),
            x => x,
            StringComparer.OrdinalIgnoreCase);

        foreach (var id in rawLines
                     .OrderByDescending(x => x.OrderCreateTime)
                     .ThenBy(x => x.PurchaseOrderItemCode)
                     .Select(x => x.PurchaseOrderItemId.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!mappedById.TryGetValue(id, out var line))
                continue;
            result.PurchaseOrderItems.Add(line);
            AddRow(
                result.PurchaseOrders,
                line.PurchaseOrderItemId,
                line.PurchaseOrderItemCode,
                line.ItemStatus,
                line.OrderCreateTime,
                line.PurchaseOrderId);
        }
    }

    private async Task<List<PurchaseOrderItemListLineDto>> MapPurchaseOrderItemListLinesAsync(
        List<PurchaseOrderItemListLineRaw> lines,
        CancellationToken cancellationToken)
    {
        if (lines.Count == 0)
            return new List<PurchaseOrderItemListLineDto>();

        var createUserIds = lines
            .Select(x => x.CreateByUserId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var createUserLoginByUserId = createUserIds.Count == 0
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : await _db.Users.AsNoTracking()
                .Where(u => createUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(x => x.Id, x => (string?)x.UserName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var poItemIds = lines
            .Select(x => x.PurchaseOrderItemId.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        const short paymentAuditFailed = -1;
        const short paymentCancelled = -2;
        var activePaymentPoItemIds = poItemIds.Count == 0
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.FinancePaymentItems.AsNoTracking()
                .Where(pi => pi.PurchaseOrderItemId != null && poItemIds.Contains(pi.PurchaseOrderItemId))
                .Join(
                    _db.FinancePayments.AsNoTracking(),
                    pi => pi.FinancePaymentId,
                    p => p.Id,
                    (pi, p) => new { pi.PurchaseOrderItemId, p.Status })
                .Where(x => x.Status != paymentAuditFailed && x.Status != paymentCancelled)
                .Select(x => x.PurchaseOrderItemId!)
                .Distinct()
                .ToListAsync(cancellationToken))
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var vendorIds = lines
            .Select(x => x.VendorId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendorEnglishMap = vendorIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Vendors.AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id) && v.EnglishOfficialName != null && v.EnglishOfficialName != "")
                .Select(v => new { v.Id, v.EnglishOfficialName })
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.EnglishOfficialName!.Trim(),
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

        var list = new List<PurchaseOrderItemListLineDto>(lines.Count);
        foreach (var r in lines)
        {
            var createKey = (r.CreateByUserId ?? string.Empty).Trim();
            string? createUserName = null;
            if (!string.IsNullOrEmpty(createKey) && createUserLoginByUserId.TryGetValue(createKey, out var login))
                createUserName = login;

            var vendorKey = r.VendorId.Trim();
            string? vendorEnglishName = null;
            if (!string.IsNullOrEmpty(vendorKey) && vendorEnglishMap.TryGetValue(vendorKey, out var ven))
                vendorEnglishName = ven;

            list.Add(new PurchaseOrderItemListLineDto
            {
                PurchaseOrderItemId = r.PurchaseOrderItemId,
                PurchaseOrderId = r.PurchaseOrderId,
                PurchaseOrderItemCode = r.PurchaseOrderItemCode,
                PurchaseOrderCode = r.PurchaseOrderCode,
                FreightForwarderOrderNo = r.FreightForwarderOrderNo,
                PurchaseOrderType = r.PurchaseOrderType,
                VendorId = r.VendorId,
                VendorName = r.VendorName,
                VendorEnglishName = vendorEnglishName,
                ItemStatus = r.ItemStatus,
                PurchaseProgressStatus = r.PurchaseProgressStatus,
                StockInProgressStatus = r.StockInProgressStatus,
                PaymentRequestProgressStatus = activePaymentPoItemIds.Contains(r.PurchaseOrderItemId.Trim())
                    ? (short)1
                    : (short)0,
                PaymentProgressStatus = r.PaymentProgressStatus,
                InvoiceProgressStatus = r.InvoiceProgressStatus,
                OrderCreateTime = r.OrderCreateTime,
                PurchaseUserName = r.PurchaseUserName,
                CreateUserName = createUserName,
                Pn = r.Pn,
                Brand = r.Brand,
                Qty = r.Qty,
                Cost = r.Cost,
                LineTotal = Math.Round(r.Qty * r.Cost, 2, MidpointRounding.AwayFromZero),
                Currency = r.Currency
            });
        }

        return list;
    }

    private static DateTime? SorOccurredAt(StockOutRequest sor) =>
        sor.RequestDate != default ? sor.RequestDate : sor.CreateTime;

    private static void AddSalesOrderItemRow(
        List<CustomsDeclarationBusinessRecordRowDto> target,
        List<string> orderedItemIds,
        CustomsDeclarationItem cdi,
        IReadOnlyDictionary<string, SellOrderItem> sellLineById,
        IReadOnlyDictionary<string, SellOrderItem> sellLineByCode,
        IReadOnlyDictionary<string, StockOutRequest> sorById,
        IReadOnlyDictionary<string, PackingItem> packingItemById,
        IReadOnlyDictionary<string, CustomsPendlist> pendlistById)
    {
        var code = cdi.SellOrderItemCode?.Trim();
        var lineId = cdi.SellOrderItemId?.Trim();

        if (string.IsNullOrEmpty(lineId))
        {
            var piKey = cdi.PackingItemId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(piKey) && packingItemById.TryGetValue(piKey, out var pi)
                && !string.IsNullOrWhiteSpace(pi.SellOrderItemId))
            {
                lineId = pi.SellOrderItemId.Trim();
            }
        }

        if (string.IsNullOrEmpty(lineId))
        {
            var plKey = cdi.CustomsPendlistId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(plKey) && pendlistById.TryGetValue(plKey, out var pl)
                && !string.IsNullOrWhiteSpace(pl.SellOrderItemId))
            {
                lineId = pl.SellOrderItemId.Trim();
            }
        }

        if (string.IsNullOrEmpty(lineId))
        {
            var sorKey = cdi.StockOutRequestId?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(sorKey) && sorById.TryGetValue(sorKey, out var sor)
                && !string.IsNullOrWhiteSpace(sor.SalesOrderItemId))
            {
                lineId = sor.SalesOrderItemId.Trim();
            }
        }

        SellOrderItem? line = null;
        if (!string.IsNullOrEmpty(lineId) && sellLineById.TryGetValue(lineId, out var byId))
            line = byId;
        else if (!string.IsNullOrEmpty(code) && sellLineByCode.TryGetValue(code, out var byCode))
            line = byCode;

        if (string.IsNullOrEmpty(code) && line != null)
            code = line.SellOrderItemCode?.Trim();

        var rowId = line?.Id?.Trim() ?? lineId ?? code;
        if (string.IsNullOrEmpty(rowId))
            return;

        var rowCode = !string.IsNullOrEmpty(code) ? code : line?.SellOrderItemCode?.Trim() ?? rowId;
        var parentId = line?.SellOrderId?.Trim() ?? ResolveSalesOrderId(cdi, sorById, sellLineById, sellLineByCode);
        var status = line?.Status;
        var occurredAt = (DateTime?)(line?.CreateTime ?? cdi.CreateTime);

        var added = AddRow(target, rowId, rowCode, status, occurredAt, parentId);
        if (added)
            orderedItemIds.Add(rowId);
    }

    private static string? ResolveSalesOrderId(
        CustomsDeclarationItem cdi,
        IReadOnlyDictionary<string, StockOutRequest> sorById,
        IReadOnlyDictionary<string, SellOrderItem> sellLineById,
        IReadOnlyDictionary<string, SellOrderItem> sellLineByCode)
    {
        var lineId = cdi.SellOrderItemId?.Trim();
        if (!string.IsNullOrEmpty(lineId)
            && sellLineById.TryGetValue(lineId, out var byId)
            && !string.IsNullOrWhiteSpace(byId.SellOrderId))
        {
            return byId.SellOrderId.Trim();
        }

        var code = cdi.SellOrderItemCode?.Trim();
        if (!string.IsNullOrEmpty(code)
            && sellLineByCode.TryGetValue(code, out var byCode)
            && !string.IsNullOrWhiteSpace(byCode.SellOrderId))
        {
            return byCode.SellOrderId.Trim();
        }

        var sorKey = cdi.StockOutRequestId?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(sorKey)
            && sorById.TryGetValue(sorKey, out var sor)
            && !string.IsNullOrWhiteSpace(sor.SalesOrderId))
        {
            return sor.SalesOrderId.Trim();
        }

        return null;
    }

    private static bool AddRow(
        List<CustomsDeclarationBusinessRecordRowDto> target,
        string id,
        string? code,
        short? status,
        DateTime? occurredAt,
        string? parentId = null)
    {
        var key = id?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
            return false;
        if (target.Any(x => string.Equals(x.Id, key, StringComparison.OrdinalIgnoreCase)))
            return false;

        target.Add(new CustomsDeclarationBusinessRecordRowDto
        {
            Id = key,
            Code = string.IsNullOrWhiteSpace(code) ? key : code.Trim(),
            Status = status,
            OccurredAt = occurredAt,
            ParentId = string.IsNullOrWhiteSpace(parentId) ? null : parentId.Trim()
        });
        return true;
    }
}

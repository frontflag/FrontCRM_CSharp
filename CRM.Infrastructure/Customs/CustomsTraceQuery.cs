using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Customs;

public sealed class CustomsTraceQuery : ICustomsTraceQuery
{
    private readonly ApplicationDbContext _db;

    public CustomsTraceQuery(ApplicationDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, CustomsTraceLinkDto>> GetByStockInNotifyIdsAsync(
        IEnumerable<string> notifyIds,
        CancellationToken cancellationToken = default)
    {
        var idList = notifyIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase);

        var notifies = await _db.StockInNotifies.AsNoTracking()
            .Where(n => idList.Contains(n.Id) && !n.IsDeleted)
            .Select(n => new { n.Id, n.CustomsDeclarationItemId, n.VendorId, n.VendorName })
            .ToListAsync(cancellationToken);

        var cdiIds = notifies
            .Where(n => !string.IsNullOrWhiteSpace(n.CustomsDeclarationItemId))
            .Select(n => n.CustomsDeclarationItemId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cdiById = cdiIds.Count == 0
            ? new Dictionary<string, (string DeclId, string? VendorId)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsDeclarationItems.AsNoTracking()
                .Where(i => cdiIds.Contains(i.Id) && !i.IsDeleted)
                .Select(i => new { i.Id, i.DeclarationId, i.VendorId })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                i => i.Id.Trim(),
                i => (DeclId: i.DeclarationId.Trim(), VendorId: string.IsNullOrWhiteSpace(i.VendorId) ? null : i.VendorId.Trim()),
                StringComparer.OrdinalIgnoreCase);

        var decIds = cdiById.Values
            .Select(x => x.DeclId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var decById = decIds.Count == 0
            ? new Dictionary<string, (string Code, string? BrokerId, short Clearance)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsDeclarations.AsNoTracking()
                .Where(d => decIds.Contains(d.Id) && !d.IsDeleted)
                .Select(d => new { d.Id, d.DeclarationCode, d.CustomsBrokerId, d.CustomsClearanceStatus })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                d => d.Id.Trim(),
                d => (
                    Code: d.DeclarationCode.Trim(),
                    BrokerId: string.IsNullOrWhiteSpace(d.CustomsBrokerId) ? null : d.CustomsBrokerId.Trim(),
                    Clearance: d.CustomsClearanceStatus),
                StringComparer.OrdinalIgnoreCase);

        var brokerIds = decById.Values
            .Select(d => d.BrokerId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var brokerNameById = brokerIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsBrokers.AsNoTracking()
                    .Where(b => brokerIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Cname })
                    .ToListAsync(cancellationToken))
                .ToDictionary(
                    b => b.Id.Trim(),
                    b => (b.Cname ?? string.Empty).Trim(),
                    StringComparer.OrdinalIgnoreCase);

        var vendorIds = notifies
            .Select(n => n.VendorId)
            .Concat(cdiById.Values.Select(v => v.VendorId))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendorNameById = await LoadVendorDisplayNameMapAsync(vendorIds, cancellationToken);

        var result = new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notifies)
        {
            var notifyKey = n.Id.Trim();
            if (string.IsNullOrWhiteSpace(n.CustomsDeclarationItemId))
                continue;

            var cdiKey = n.CustomsDeclarationItemId.Trim();
            if (!cdiById.TryGetValue(cdiKey, out var cdi))
                continue;
            if (!decById.TryGetValue(cdi.DeclId, out var dec))
                continue;

            var vendorId = !string.IsNullOrWhiteSpace(n.VendorId)
                ? n.VendorId.Trim()
                : cdi.VendorId;
            vendorNameById.TryGetValue(vendorId ?? string.Empty, out var vendorName);
            if (string.IsNullOrWhiteSpace(vendorName) && !string.IsNullOrWhiteSpace(n.VendorName))
                vendorName = n.VendorName.Trim();

            string? brokerName = null;
            if (!string.IsNullOrEmpty(dec.BrokerId))
                brokerNameById.TryGetValue(dec.BrokerId, out brokerName);

            result[notifyKey] = new CustomsTraceLinkDto
            {
                CustomsDeclarationId = cdi.DeclId,
                CustomsDeclarationCode = dec.Code,
                VendorId = vendorId,
                VendorName = vendorName,
                CustomsBrokerId = dec.BrokerId,
                CustomsBrokerName = string.IsNullOrWhiteSpace(brokerName) ? null : brokerName,
                CustomsClearanceStatus = dec.Clearance
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task EnrichCustomsStockInNotifiesAsync(
        IReadOnlyList<StockInNotify> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        var customsRows = rows
            .Where(r => StockInTypeCode.NormalizeForNotify(r.StockInType) == StockInTypeCode.Customs
                        || !string.IsNullOrWhiteSpace(r.CustomsDeclarationItemId))
            .ToList();
        if (customsRows.Count == 0)
        {
            await FillMissingVendorNamesAsync(rows, cancellationToken);
            return;
        }

        var traceMap = await GetByStockInNotifyIdsAsync(
            customsRows.Select(r => r.Id),
            cancellationToken);

        foreach (var row in customsRows)
        {
            if (!traceMap.TryGetValue(row.Id.Trim(), out var trace))
                continue;

            row.CustomsDeclarationId = trace.CustomsDeclarationId;
            row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
            row.CustomsBrokerName = string.IsNullOrWhiteSpace(trace.CustomsBrokerName)
                ? null
                : trace.CustomsBrokerName.Trim();
            row.CustomsClearanceStatus = trace.CustomsClearanceStatus;
            if (string.IsNullOrWhiteSpace(row.VendorId) && !string.IsNullOrWhiteSpace(trace.VendorId))
                row.VendorId = trace.VendorId;
            if (string.IsNullOrWhiteSpace(row.VendorName) && !string.IsNullOrWhiteSpace(trace.VendorName))
                row.VendorName = trace.VendorName;
        }

        await FillMissingVendorNamesAsync(rows, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, CustomsTraceLinkDto>> GetByStockOutNotifyIdsAsync(
        IEnumerable<string> notifyIds,
        CancellationToken cancellationToken = default)
    {
        var idList = notifyIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase);

        var cdiRows = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => !i.IsDeleted
                        && ((i.CustomsStockOutNotifyId != null && idList.Contains(i.CustomsStockOutNotifyId))
                            || idList.Contains(i.StockOutRequestId)))
            .Select(i => new
            {
                CustomsNotifyId = i.CustomsStockOutNotifyId,
                SalesNotifyId = i.StockOutRequestId,
                i.DeclarationId,
                i.VendorId,
                i.LineNo
            })
            .ToListAsync(cancellationToken);

        var bestByNotify = new Dictionary<string, (string DeclId, string? VendorId, int LineNo)>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in cdiRows)
        {
            void Consider(string? notifyId)
            {
                if (string.IsNullOrWhiteSpace(notifyId))
                    return;
                var key = notifyId.Trim();
                if (!idList.Contains(key, StringComparer.OrdinalIgnoreCase))
                    return;
                var declId = row.DeclarationId.Trim();
                var vendorId = string.IsNullOrWhiteSpace(row.VendorId) ? null : row.VendorId.Trim();
                if (!bestByNotify.TryGetValue(key, out var existing) || row.LineNo < existing.LineNo)
                    bestByNotify[key] = (declId, vendorId, row.LineNo);
            }

            Consider(row.CustomsNotifyId);
            Consider(row.SalesNotifyId);
        }

        if (bestByNotify.Count == 0)
            return new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase);

        var decIds = bestByNotify.Values
            .Select(x => x.DeclId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var decById = (await _db.CustomsDeclarations.AsNoTracking()
                .Where(d => decIds.Contains(d.Id) && !d.IsDeleted)
                .Select(d => new { d.Id, d.DeclarationCode, d.CustomsBrokerId, d.CustomsClearanceStatus })
                .ToListAsync(cancellationToken))
            .ToDictionary(d => d.Id.Trim(), d => d, StringComparer.OrdinalIgnoreCase);

        var brokerIds = decById.Values
            .Select(d => d.CustomsBrokerId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var brokerNameById = brokerIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsBrokers.AsNoTracking()
                    .Where(b => brokerIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Cname })
                    .ToListAsync(cancellationToken))
                .ToDictionary(
                    b => b.Id.Trim(),
                    b => (b.Cname ?? string.Empty).Trim(),
                    StringComparer.OrdinalIgnoreCase);

        var vendorIds = bestByNotify.Values
            .Select(x => x.VendorId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var vendorNameById = await LoadVendorDisplayNameMapAsync(vendorIds, cancellationToken);

        var result = new Dictionary<string, CustomsTraceLinkDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var (notifyKey, tuple) in bestByNotify)
        {
            if (!decById.TryGetValue(tuple.DeclId, out var dec))
                continue;
            vendorNameById.TryGetValue(tuple.VendorId ?? string.Empty, out var vendorName);
            string? brokerName = null;
            var brokerId = dec.CustomsBrokerId?.Trim();
            if (!string.IsNullOrEmpty(brokerId))
                brokerNameById.TryGetValue(brokerId, out brokerName);
            result[notifyKey] = new CustomsTraceLinkDto
            {
                CustomsDeclarationId = tuple.DeclId,
                CustomsDeclarationCode = dec.DeclarationCode.Trim(),
                VendorId = tuple.VendorId,
                VendorName = vendorName,
                CustomsBrokerId = brokerId,
                CustomsBrokerName = brokerName,
                CustomsClearanceStatus = dec.CustomsClearanceStatus
            };
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CustomsOriginalPurchaseLinkDto?> ResolveOriginalPurchaseByArrivalNotifyAsync(
        string? customsDeclarationItemId,
        short stockInType,
        CancellationToken cancellationToken = default)
    {
        if (StockInTypeCode.NormalizeForNotify(stockInType) != StockInTypeCode.Customs)
            return null;

        var cdiId = customsDeclarationItemId?.Trim();
        if (string.IsNullOrEmpty(cdiId))
            return null;

        var sourceStockItemId = await _db.CustomsDeclarationItems.AsNoTracking()
            .Where(i => i.Id == cdiId && !i.IsDeleted)
            .Select(i => i.SourceStockItemId)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(sourceStockItemId))
            return null;

        var layerId = sourceStockItemId.Trim();
        var layer = await _db.StockItems.AsNoTracking()
            .Where(si => si.Id == layerId && !si.IsDeleted)
            .Select(si => new { si.PurchaseOrderItemId, si.StockInItemId })
            .FirstOrDefaultAsync(cancellationToken);
        if (layer == null)
            return null;

        var poItemId = layer.PurchaseOrderItemId?.Trim();
        if (string.IsNullOrEmpty(poItemId) && !string.IsNullOrWhiteSpace(layer.StockInItemId))
        {
            var extendId = layer.StockInItemId.Trim();
            poItemId = await _db.StockInItemExtends.AsNoTracking()
                .Where(e => e.Id == extendId && !e.IsDeleted && e.PurchaseOrderItemId != null)
                .Select(e => e.PurchaseOrderItemId!)
                .FirstOrDefaultAsync(cancellationToken);
            poItemId = poItemId?.Trim();
        }

        if (string.IsNullOrEmpty(poItemId))
            return null;

        var poItem = await _db.PurchaseOrderItems.AsNoTracking()
            .Where(p => p.Id == poItemId)
            .Select(p => new { p.Id, p.PurchaseOrderItemCode, p.PurchaseOrderId, p.Qty })
            .FirstOrDefaultAsync(cancellationToken);
        if (poItem == null)
            return null;

        var poId = poItem.PurchaseOrderId?.Trim();
        if (string.IsNullOrEmpty(poId))
            return null;

        var po = await _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.Id == poId)
            .Select(p => new { p.CreateTime, p.PurchaseUserName })
            .FirstOrDefaultAsync(cancellationToken);

        return new CustomsOriginalPurchaseLinkDto
        {
            PurchaseOrderItemId = poItem.Id.Trim(),
            PurchaseOrderItemCode = (poItem.PurchaseOrderItemCode ?? string.Empty).Trim(),
            PurchaseOrderId = poId,
            PurchaseUserName = po?.PurchaseUserName?.Trim(),
            PurchaseOrderCreateTime = po?.CreateTime,
            Qty = poItem.Qty
        };
    }

    /// <inheritdoc />
    public async Task EnrichStockOutRequestListItemsAsync(
        IReadOnlyList<StockOutRequestListItemDto> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        var targetRows = rows
            .Where(r => StockOutTypeCode.NormalizeForNotify(r.StockOutType) == StockOutTypeCode.Customs)
            .ToList();
        if (targetRows.Count == 0)
            return;

        var traceMap = await GetByStockOutNotifyIdsAsync(targetRows.Select(r => r.Id), cancellationToken);
        foreach (var row in targetRows)
        {
            if (!traceMap.TryGetValue(row.Id.Trim(), out var trace))
                continue;
            row.CustomsDeclarationId = trace.CustomsDeclarationId;
            row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
            row.CustomsBrokerName = trace.CustomsBrokerName;
        }
    }

    /// <inheritdoc />
    public async Task EnrichStockOutListItemsAsync(
        IReadOnlyList<StockOutListItemDto> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        var customsRows = rows
            .Where(r => StockOutTypeCode.NormalizeForNotify(r.StockOutType) == StockOutTypeCode.Customs)
            .ToList();
        if (customsRows.Count == 0)
            return;

        var sourceIds = customsRows
            .Select(r => r.SourceId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (sourceIds.Count == 0)
            return;

        var traceMap = await GetByStockOutNotifyIdsAsync(sourceIds, cancellationToken);
        foreach (var row in customsRows)
        {
            var sourceId = row.SourceId?.Trim();
            if (string.IsNullOrEmpty(sourceId))
                continue;
            if (!traceMap.TryGetValue(sourceId, out var trace))
                continue;
            row.CustomsDeclarationId = trace.CustomsDeclarationId;
            row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
            row.CustomsBrokerName = string.IsNullOrWhiteSpace(trace.CustomsBrokerName)
                ? null
                : trace.CustomsBrokerName.Trim();
            row.CustomsClearanceStatus = trace.CustomsClearanceStatus;
        }
    }

    /// <inheritdoc />
    public async Task EnrichStockOutItemListItemsAsync(
        IReadOnlyList<StockOutItemListRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        var customsRows = rows
            .Where(r => StockOutTypeCode.NormalizeForNotify(r.StockOutType) == StockOutTypeCode.Customs)
            .ToList();
        if (customsRows.Count == 0)
            return;

        var itemIds = customsRows
            .Select(r => r.StockOutItemId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var extendByItemId = itemIds.Count == 0
            ? new Dictionary<string, (string? CdiId, string? StockInItemId)>(StringComparer.OrdinalIgnoreCase)
            : (await _db.StockOutItemExtends.AsNoTracking()
                .Where(e => itemIds.Contains(e.Id) && !e.IsDeleted)
                .Select(e => new { e.Id, e.CustomsDeclarationItemId, e.StockInItemId })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                e => e.Id.Trim(),
                e => (
                    CdiId: string.IsNullOrWhiteSpace(e.CustomsDeclarationItemId) ? null : e.CustomsDeclarationItemId.Trim(),
                    StockInItemId: string.IsNullOrWhiteSpace(e.StockInItemId) ? null : e.StockInItemId.Trim()),
                StringComparer.OrdinalIgnoreCase);

        var cdiIds = extendByItemId.Values
            .Select(x => x.CdiId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (cdiIds.Count > 0)
        {
            var cdiToDecl = (await _db.CustomsDeclarationItems.AsNoTracking()
                    .Where(i => cdiIds.Contains(i.Id) && !i.IsDeleted)
                    .Select(i => new { i.Id, i.DeclarationId })
                    .ToListAsync(cancellationToken))
                .Where(i => !string.IsNullOrWhiteSpace(i.DeclarationId))
                .ToDictionary(
                    i => i.Id.Trim(),
                    i => i.DeclarationId.Trim(),
                    StringComparer.OrdinalIgnoreCase);
            var summaries = await LoadSummariesByDeclarationIdsAsync(cdiToDecl.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cancellationToken);
            foreach (var row in customsRows)
            {
                var itemId = row.StockOutItemId?.Trim();
                if (string.IsNullOrEmpty(itemId) || !extendByItemId.TryGetValue(itemId, out var ext) || ext.CdiId == null)
                    continue;
                if (!cdiToDecl.TryGetValue(ext.CdiId, out var declId))
                    continue;
                if (!summaries.TryGetValue(declId, out var summary))
                    continue;
                ApplyStockOutItemCustoms(row, summary);
            }
        }

        await ApplyPackingDeclarationsToStockOutItemRowsAsync(
            customsRows.Where(r => string.IsNullOrWhiteSpace(r.CustomsDeclarationId)).ToList(),
            r => r.PackingId,
            cancellationToken);

        var missing = customsRows
            .Where(r => string.IsNullOrWhiteSpace(r.CustomsDeclarationId))
            .ToList();
        if (missing.Count == 0)
            return;

        var stockOutIds = missing
            .Select(r => r.StockOutId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var sourceByOutId = stockOutIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.StockOuts.AsNoTracking()
                    .Where(s => stockOutIds.Contains(s.Id) && !s.IsDeleted)
                    .Select(s => new { s.Id, s.SourceId })
                    .ToListAsync(cancellationToken))
                .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
                .ToDictionary(
                    s => s.Id.Trim(),
                    s => s.SourceId!.Trim(),
                    StringComparer.OrdinalIgnoreCase);

        if (sourceByOutId.Count > 0)
        {
            var traceMap = await GetByStockOutNotifyIdsAsync(sourceByOutId.Values, cancellationToken);
            foreach (var row in missing)
            {
                var outId = row.StockOutId?.Trim();
                if (string.IsNullOrEmpty(outId) || !sourceByOutId.TryGetValue(outId, out var sourceId))
                    continue;
                if (!traceMap.TryGetValue(sourceId, out var trace))
                    continue;
                ApplyStockOutItemCustoms(row, trace);
            }

            // 按箱出库时 SourceId 是装箱单 Id，不是出库通知 Id。
            var stillMissing = missing.Where(r => string.IsNullOrWhiteSpace(r.CustomsDeclarationId)).ToList();
            if (stillMissing.Count > 0)
            {
                await ApplyPackingDeclarationsToStockOutItemRowsAsync(
                    stillMissing,
                    r =>
                    {
                        var outId = r.StockOutId?.Trim();
                        return string.IsNullOrEmpty(outId) ? null : sourceByOutId.GetValueOrDefault(outId);
                    },
                    cancellationToken);
            }
        }

        missing = customsRows
            .Where(r => string.IsNullOrWhiteSpace(r.CustomsDeclarationId))
            .ToList();
        if (missing.Count == 0)
            return;

        var stockInItemIds = missing
            .Select(r =>
            {
                var itemId = r.StockOutItemId?.Trim();
                if (string.IsNullOrEmpty(itemId) || !extendByItemId.TryGetValue(itemId, out var ext))
                    return null;
                return ext.StockInItemId;
            })
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stockInItemIds.Count == 0)
            return;

        var stockInIdByItemId = (await _db.StockInItems.AsNoTracking()
                .Where(i => stockInItemIds.Contains(i.Id) && !i.IsDeleted)
                .Select(i => new { i.Id, i.StockInId })
                .ToListAsync(cancellationToken))
            .Where(i => !string.IsNullOrWhiteSpace(i.StockInId))
            .ToDictionary(
                i => i.Id.Trim(),
                i => i.StockInId.Trim(),
                StringComparer.OrdinalIgnoreCase);
        if (stockInIdByItemId.Count == 0)
            return;

        var notifyByStockInId = (await _db.StockIns.AsNoTracking()
                .Where(s => stockInIdByItemId.Values.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.SourceId })
                .ToListAsync(cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .ToDictionary(
                s => s.Id.Trim(),
                s => s.SourceId!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        if (notifyByStockInId.Count == 0)
            return;

        var inboundTrace = await GetByStockInNotifyIdsAsync(notifyByStockInId.Values, cancellationToken);
        foreach (var row in missing)
        {
            var itemId = row.StockOutItemId?.Trim();
            if (string.IsNullOrEmpty(itemId) || !extendByItemId.TryGetValue(itemId, out var ext) || ext.StockInItemId == null)
                continue;
            if (!stockInIdByItemId.TryGetValue(ext.StockInItemId, out var stockInId))
                continue;
            if (!notifyByStockInId.TryGetValue(stockInId, out var notifyId))
                continue;
            if (!inboundTrace.TryGetValue(notifyId, out var trace))
                continue;
            ApplyStockOutItemCustoms(row, trace);
        }
    }

    private async Task ApplyPackingDeclarationsToStockOutItemRowsAsync(
        IReadOnlyList<StockOutItemListRowDto> rows,
        Func<StockOutItemListRowDto, string?> packingIdSelector,
        CancellationToken cancellationToken)
    {
        var packingIds = rows
            .Select(r => packingIdSelector(r)?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (packingIds.Count == 0)
            return;

        var packingDecls = await _db.Packings.AsNoTracking()
            .Where(p => packingIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.CustomsDeclarationId })
            .ToListAsync(cancellationToken);
        var packingToDecl = packingDecls
            .Where(p => !string.IsNullOrWhiteSpace(p.CustomsDeclarationId))
            .ToDictionary(
                p => p.Id.Trim(),
                p => p.CustomsDeclarationId!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        if (packingToDecl.Count == 0)
            return;

        var summaries = await LoadSummariesByDeclarationIdsAsync(packingToDecl.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cancellationToken);
        foreach (var row in rows)
        {
            var packingId = packingIdSelector(row)?.Trim();
            if (string.IsNullOrEmpty(packingId) || !packingToDecl.TryGetValue(packingId, out var declId))
                continue;
            if (!summaries.TryGetValue(declId, out var summary))
                continue;
            ApplyStockOutItemCustoms(row, summary);
        }
    }

    private static void ApplyStockOutItemCustoms(StockOutItemListRowDto row, StockOutCustomsSummaryDto summary)
    {
        row.CustomsDeclarationId = summary.DeclarationId;
        row.CustomsDeclarationCode = summary.DeclarationCode;
        row.CustomsBrokerName = string.IsNullOrWhiteSpace(summary.CustomsBrokerName)
            ? null
            : summary.CustomsBrokerName.Trim();
        row.CustomsClearanceStatus = summary.CustomsClearanceStatus;
    }

    private static void ApplyStockOutItemCustoms(StockOutItemListRowDto row, CustomsTraceLinkDto trace)
    {
        row.CustomsDeclarationId = trace.CustomsDeclarationId;
        row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
        row.CustomsBrokerName = string.IsNullOrWhiteSpace(trace.CustomsBrokerName)
            ? null
            : trace.CustomsBrokerName.Trim();
        row.CustomsClearanceStatus = trace.CustomsClearanceStatus;
    }

    /// <inheritdoc />
    public async Task EnrichStockItemListItemsAsync(
        IReadOnlyList<InventoryStockItemListRowDto> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        var customsRows = rows
            .Where(r => StockInTypeCode.NormalizeForNotify(r.StockInType) == StockInTypeCode.Customs)
            .ToList();
        if (customsRows.Count == 0)
            return;

        var stockInIds = customsRows
            .Select(r => r.StockInId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stockInIds.Count == 0)
            return;

        var notifyByStockInId = (await _db.StockIns.AsNoTracking()
                .Where(s => stockInIds.Contains(s.Id) && !s.IsDeleted)
                .Select(s => new { s.Id, s.SourceId })
                .ToListAsync(cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s.SourceId))
            .ToDictionary(
                s => s.Id.Trim(),
                s => s.SourceId!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        if (notifyByStockInId.Count == 0)
            return;

        var traceMap = await GetByStockInNotifyIdsAsync(notifyByStockInId.Values, cancellationToken);
        foreach (var row in customsRows)
        {
            var stockInId = row.StockInId?.Trim();
            if (string.IsNullOrEmpty(stockInId) || !notifyByStockInId.TryGetValue(stockInId, out var notifyId))
                continue;
            if (!traceMap.TryGetValue(notifyId, out var trace))
                continue;
            row.CustomsDeclarationId = trace.CustomsDeclarationId;
            row.CustomsDeclarationCode = trace.CustomsDeclarationCode;
            row.CustomsBrokerName = string.IsNullOrWhiteSpace(trace.CustomsBrokerName)
                ? null
                : trace.CustomsBrokerName.Trim();
            row.CustomsClearanceStatus = trace.CustomsClearanceStatus;
        }
    }

    /// <inheritdoc />
    public async Task<StockOutCustomsSummaryDto?> ResolveStockOutNotifyCustomsSummaryAsync(
        string notifyId,
        short stockOutType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notifyId))
            return null;
        if (StockOutTypeCode.NormalizeForNotify(stockOutType) != StockOutTypeCode.Customs)
            return null;

        var traceMap = await GetByStockOutNotifyIdsAsync(new[] { notifyId.Trim() }, cancellationToken);
        if (!traceMap.TryGetValue(notifyId.Trim(), out var trace))
            return null;

        return await LoadStockOutCustomsSummaryAsync(trace.CustomsDeclarationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StockOutCustomsSummaryDto?> ResolveStockOutCustomsSummaryAsync(
        StockOut stockOut,
        CancellationToken cancellationToken = default)
    {
        if (stockOut == null)
            return null;
        if (StockOutTypeCode.NormalizeForNotify(stockOut.StockOutType) != StockOutTypeCode.Customs)
            return null;

        var sourceId = stockOut.SourceId?.Trim();
        if (string.IsNullOrEmpty(sourceId))
            return null;

        return await ResolveStockOutNotifyCustomsSummaryAsync(sourceId, stockOut.StockOutType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<StockOutCustomsSummaryDto?> ResolveCustomsSummaryByDeclarationIdAsync(
        string? declarationId,
        CancellationToken cancellationToken = default) =>
        LoadStockOutCustomsSummaryAsync(declarationId ?? string.Empty, cancellationToken);

    private async Task<StockOutCustomsSummaryDto?> LoadStockOutCustomsSummaryAsync(
        string declarationId,
        CancellationToken cancellationToken)
    {
        var map = await LoadSummariesByDeclarationIdsAsync(new[] { declarationId }, cancellationToken);
        return map.TryGetValue(declarationId.Trim(), out var summary) ? summary : null;
    }

    private async Task<IReadOnlyDictionary<string, StockOutCustomsSummaryDto>> LoadSummariesByDeclarationIdsAsync(
        IReadOnlyList<string> declarationIds,
        CancellationToken cancellationToken)
    {
        var idList = declarationIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new Dictionary<string, StockOutCustomsSummaryDto>(StringComparer.OrdinalIgnoreCase);

        var decs = await _db.CustomsDeclarations.AsNoTracking()
            .Where(d => idList.Contains(d.Id) && !d.IsDeleted)
            .Select(d => new { d.Id, d.DeclarationCode, d.CustomsBrokerId, d.CustomsClearanceStatus })
            .ToListAsync(cancellationToken);
        var brokerIds = decs
            .Select(d => d.CustomsBrokerId?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var brokerNameById = brokerIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsBrokers.AsNoTracking()
                    .Where(b => brokerIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Cname })
                    .ToListAsync(cancellationToken))
                .ToDictionary(
                    b => b.Id.Trim(),
                    b => (b.Cname ?? string.Empty).Trim(),
                    StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, StockOutCustomsSummaryDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var dec in decs)
        {
            var brokerId = dec.CustomsBrokerId?.Trim();
            string? brokerName = null;
            if (!string.IsNullOrEmpty(brokerId))
                brokerNameById.TryGetValue(brokerId, out brokerName);
            result[dec.Id.Trim()] = new StockOutCustomsSummaryDto
            {
                DeclarationId = dec.Id.Trim(),
                DeclarationCode = dec.DeclarationCode.Trim(),
                CustomsBrokerId = string.IsNullOrEmpty(brokerId) ? null : brokerId,
                CustomsBrokerName = string.IsNullOrWhiteSpace(brokerName) ? null : brokerName,
                CustomsClearanceStatus = dec.CustomsClearanceStatus
            };
        }

        return result;
    }

    private async Task FillMissingVendorNamesAsync(
        IReadOnlyList<StockInNotify> rows,
        CancellationToken cancellationToken)
    {
        var vendorIds = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.VendorId) && string.IsNullOrWhiteSpace(r.VendorName))
            .Select(r => r.VendorId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (vendorIds.Count == 0)
            return;

        var vendorNameById = await LoadVendorDisplayNameMapAsync(vendorIds, cancellationToken);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.VendorId) || !string.IsNullOrWhiteSpace(row.VendorName))
                continue;
            if (vendorNameById.TryGetValue(row.VendorId.Trim(), out var name))
                row.VendorName = name;
        }
    }

    private async Task<Dictionary<string, string>> LoadVendorDisplayNameMapAsync(
        IReadOnlyList<string> vendorIds,
        CancellationToken cancellationToken)
    {
        if (vendorIds.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return (await _db.Vendors.AsNoTracking()
                .Where(v => vendorIds.Contains(v.Id))
                .Select(v => new { v.Id, v.OfficialName, v.NickName, v.Code })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                v => v.Id.Trim(),
                v => !string.IsNullOrWhiteSpace(v.OfficialName) ? v.OfficialName.Trim()
                    : !string.IsNullOrWhiteSpace(v.NickName) ? v.NickName.Trim()
                    : (v.Code ?? string.Empty).Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}

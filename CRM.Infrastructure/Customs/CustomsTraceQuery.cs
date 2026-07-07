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
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (await _db.CustomsDeclarations.AsNoTracking()
                .Where(d => decIds.Contains(d.Id) && !d.IsDeleted)
                .Select(d => new { d.Id, d.DeclarationCode })
                .ToListAsync(cancellationToken))
            .ToDictionary(d => d.Id.Trim(), d => d.DeclarationCode.Trim(), StringComparer.OrdinalIgnoreCase);

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
            if (!decById.TryGetValue(cdi.DeclId, out var decCode))
                continue;

            var vendorId = !string.IsNullOrWhiteSpace(n.VendorId)
                ? n.VendorId.Trim()
                : cdi.VendorId;
            vendorNameById.TryGetValue(vendorId ?? string.Empty, out var vendorName);
            if (string.IsNullOrWhiteSpace(vendorName) && !string.IsNullOrWhiteSpace(n.VendorName))
                vendorName = n.VendorName.Trim();

            result[notifyKey] = new CustomsTraceLinkDto
            {
                CustomsDeclarationId = cdi.DeclId,
                CustomsDeclarationCode = decCode,
                VendorId = vendorId,
                VendorName = vendorName
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
        var declId = declarationId.Trim();
        if (string.IsNullOrEmpty(declId))
            return null;

        var dec = await _db.CustomsDeclarations.AsNoTracking()
            .Where(d => d.Id == declId && !d.IsDeleted)
            .Select(d => new { d.Id, d.DeclarationCode, d.CustomsBrokerId, d.CustomsClearanceStatus })
            .FirstOrDefaultAsync(cancellationToken);
        if (dec == null)
            return null;

        string? brokerName = null;
        var brokerId = dec.CustomsBrokerId?.Trim();
        if (!string.IsNullOrEmpty(brokerId))
        {
            brokerName = await _db.CustomsBrokers.AsNoTracking()
                .Where(b => b.Id == brokerId)
                .Select(b => b.Cname)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(brokerName))
                brokerName = brokerName.Trim();
        }

        return new StockOutCustomsSummaryDto
        {
            DeclarationId = dec.Id.Trim(),
            DeclarationCode = dec.DeclarationCode.Trim(),
            CustomsBrokerId = brokerId,
            CustomsBrokerName = brokerName,
            CustomsClearanceStatus = dec.CustomsClearanceStatus
        };
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

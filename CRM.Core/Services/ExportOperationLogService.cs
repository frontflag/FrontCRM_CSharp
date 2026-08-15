using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Utilities;

namespace CRM.Core.Services;

public sealed class ExportOperationLogRequest
{
    public required string BizType { get; init; }
    public required string RecordId { get; init; }
    public string? RecordCode { get; init; }
    public required string ActionType { get; init; }
    public required string ExportKind { get; init; }
    public required string OperationDesc { get; init; }
    public int ExportedCount { get; init; }
    public int MaxRows { get; init; } = ExportOperationAudit.DefaultMaxExportRows;
    public bool Truncated { get; init; }
    public required IReadOnlyDictionary<string, object?> Filters { get; init; }
    public bool FiltersMasked { get; init; }
    public string? OperatorUserId { get; init; }
    public string? OperatorUserName { get; init; }
    public string? PageTitle { get; init; }
    public string? PageUrl { get; init; }
}

public interface IExportOperationLogService
{
    Task AppendAsync(ExportOperationLogRequest request, CancellationToken cancellationToken = default);
}

public sealed class ExportOperationLogService : IExportOperationLogService
{
    private readonly ILogOperationAppendService _append;
    private readonly IInventoryCenterService _inventory;

    public ExportOperationLogService(ILogOperationAppendService append, IInventoryCenterService inventory)
    {
        _append = append;
        _inventory = inventory;
    }

    public async Task AppendAsync(ExportOperationLogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var display = await BuildDisplayContextAsync(request.Filters, cancellationToken).ConfigureAwait(false);
        var catalog = ExportKindCatalog.Get(request.ExportKind);
        var pageTitle = string.IsNullOrWhiteSpace(request.PageTitle) ? catalog?.PageTitle : request.PageTitle.Trim();
        var pageUrl = ExportKindCatalog.SanitizePageUrl(request.PageUrl) ?? catalog?.PageUrl;
        var sysRemark = ExportKindCatalog.BuildSysRemark(request.Truncated, request.FiltersMasked, request.MaxRows);
        var extra = ExportOperationAudit.BuildExtraInfoJson(
            request.ExportKind,
            request.ExportedCount,
            request.Filters,
            request.FiltersMasked,
            request.MaxRows,
            request.Truncated,
            display: display,
            pageTitle: pageTitle,
            pageUrl: pageUrl,
            sysRemark: string.IsNullOrWhiteSpace(sysRemark) ? null : sysRemark);

        await _append.AppendAsync(
            request.BizType,
            request.RecordId,
            request.RecordCode,
            request.ActionType,
            request.OperatorUserId,
            request.OperatorUserName,
            request.OperationDesc,
            null,
            extra,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ExportFilterDisplayContext?> BuildDisplayContextAsync(
        IReadOnlyDictionary<string, object?> filters,
        CancellationToken cancellationToken)
    {
        if (!NeedsWarehouseNames(filters)) return null;

        cancellationToken.ThrowIfCancellationRequested();
        var warehouses = await _inventory.GetWarehousesAsync().ConfigureAwait(false);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in warehouses)
        {
            if (string.IsNullOrWhiteSpace(w.Id)) continue;
            var name = string.IsNullOrWhiteSpace(w.WarehouseName) ? w.Id : w.WarehouseName.Trim();
            map[w.Id] = name;
        }

        return map.Count == 0
            ? null
            : new ExportFilterDisplayContext { WarehouseNamesById = map };
    }

    private static bool NeedsWarehouseNames(IReadOnlyDictionary<string, object?> filters)
    {
        if (!filters.TryGetValue("warehouseId", out var value) && !filters.TryGetValue("WarehouseId", out value))
            return false;
        if (value is null) return false;
        if (value is string s) return !string.IsNullOrWhiteSpace(s);
        var text = Convert.ToString(value);
        return !string.IsNullOrWhiteSpace(text);
    }
}

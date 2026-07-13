using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Vendor;
using CRM.Infrastructure.Ai;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

public sealed class VendorIntelReportService : IVendorIntelReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IRepository<VendorInfo> _vendorRepo;

    public VendorIntelReportService(
        ApplicationDbContext db,
        IAiOrchestrator aiOrchestrator,
        IRepository<VendorInfo> vendorRepo)
    {
        _db = db;
        _aiOrchestrator = aiOrchestrator;
        _vendorRepo = vendorRepo;
    }

    public async Task<VendorIntelInvestigateResultDto> InvestigateAsync(
        VendorIntelInvestigateRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var companyName = (request.CompanyName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(companyName))
            throw new ArgumentException("企业名称不能为空", nameof(request));

        var creditCode = NormalizeOptional(request.CreditCode);
        var region = NormalizeOptional(request.Region);
        var vendorId = NormalizeOptional(request.VendorId);

        if (!string.IsNullOrEmpty(vendorId))
        {
            var vendor = await _vendorRepo.GetByIdAsync(vendorId)
                           ?? throw new InvalidOperationException("供应商不存在");
            if (string.IsNullOrEmpty(creditCode))
                creditCode = NormalizeOptional(vendor.CreditCode);
            if (string.IsNullOrEmpty(companyName))
                companyName = (vendor.OfficialName ?? string.Empty).Trim();
        }

        var fingerprint = IntelReportFingerprint.Build(companyName, creditCode);

        if (!request.ForceRefresh)
        {
            var peer = await IntelReportPeerCache.TryLoadFromCustomerTableAsync(_db, fingerprint, cancellationToken);
            if (peer != null)
            {
                var cached = await SaveReportAsync(
                    vendorId,
                    companyName,
                    creditCode,
                    fingerprint,
                    peer.ReportJson,
                    "cache",
                    peer.InvocationLogId,
                    userId,
                    peer.SchemaVersion,
                    cancellationToken);
                cached.FromCache = true;
                return new VendorIntelInvestigateResultDto
                {
                    Report = cached,
                    FromCache = true,
                    InvocationId = peer.InvocationLogId ?? cached.Id
                };
            }
        }

        var input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["company_name"] = companyName,
            ["credit_code"] = creditCode,
            ["region"] = region,
            ["vendor_id"] = vendorId
        };

        var invokeResult = await _aiOrchestrator.InvokeAsync(
            new AiInvokeRequestDto
            {
                ScenarioCode = AiScenarioCodes.VendorIntelLookup,
                Input = input,
                BizType = "VENDOR",
                BizId = vendorId,
                TriggerType = AiInvocationTriggerType.Manual,
                ForceRefresh = request.ForceRefresh
            },
            userId,
            cancellationToken);

        var reportJson = ResolveReportJson(invokeResult);

        var saved = await SaveReportAsync(
            vendorId,
            companyName,
            creditCode,
            fingerprint,
            reportJson,
            invokeResult.FromCache ? "cache" : "live",
            invokeResult.InvocationId,
            userId,
            "1.1",
            cancellationToken);

        saved.FromCache = invokeResult.FromCache;
        return new VendorIntelInvestigateResultDto
        {
            Report = saved,
            FromCache = invokeResult.FromCache,
            InvocationId = invokeResult.InvocationId
        };
    }

    public async Task<VendorIntelReportDetailDto?> GetLatestByVendorIdAsync(
        string vendorId,
        CancellationToken cancellationToken = default)
    {
        var id = vendorId.Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var row = await _db.VendorIntelReports.AsNoTracking()
            .Where(r => r.VendorId == id && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (row != null)
            return await MapDetailAsync(row, cancellationToken);

        var vendor = await _vendorRepo.GetByIdAsync(id);
        if (vendor == null) return null;

        var name = (vendor.OfficialName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name)) return null;

        return await GetLatestByQueryAsync(name, vendor.CreditCode, cancellationToken);
    }

    public async Task<IReadOnlyList<VendorIntelReportSummaryDto>> ListByVendorIdAsync(
        string vendorId,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var id = vendorId.Trim();
        if (string.IsNullOrEmpty(id)) return Array.Empty<VendorIntelReportSummaryDto>();

        var rows = await _db.VendorIntelReports.AsNoTracking()
            .Where(r => r.VendorId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        if (rows.Count > 0)
            return await MapSummariesAsync(rows, cancellationToken);

        var vendor = await _vendorRepo.GetByIdAsync(id);
        if (vendor == null) return Array.Empty<VendorIntelReportSummaryDto>();

        var fingerprint = IntelReportFingerprint.Build(vendor.OfficialName ?? string.Empty, vendor.CreditCode);
        rows = await _db.VendorIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return await MapSummariesAsync(rows, cancellationToken);
    }

    public async Task<VendorIntelReportDetailDto?> GetByIdAsync(
        string reportId,
        CancellationToken cancellationToken = default)
    {
        var id = reportId.Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var row = await _db.VendorIntelReports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return row == null ? null : await MapDetailAsync(row, cancellationToken);
    }

    public async Task<VendorIntelReportDetailDto?> GetLatestByQueryAsync(
        string companyName,
        string? creditCode,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = IntelReportFingerprint.Build(companyName, creditCode);
        var row = await _db.VendorIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (row != null)
            return await MapDetailAsync(row, cancellationToken);

        var peer = await IntelReportPeerCache.TryLoadFromCustomerTableAsync(_db, fingerprint, cancellationToken);
        if (peer == null) return null;

        return MapPeerReadOnly(companyName.Trim(), creditCode, peer);
    }

    private async Task<VendorIntelReportDetailDto> SaveReportAsync(
        string? vendorId,
        string companyName,
        string? creditCode,
        string fingerprint,
        string reportJson,
        string source,
        string? invocationLogId,
        string? userId,
        string schemaVersion,
        CancellationToken cancellationToken)
    {
        await _db.VendorIntelReports
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.IsLatest, false),
                cancellationToken);

        var entity = new VendorIntelReport
        {
            Id = Guid.NewGuid().ToString(),
            VendorId = vendorId,
            CompanyName = companyName,
            CreditCode = creditCode,
            QueryFingerprint = fingerprint,
            ScenarioCode = AiScenarioCodes.VendorIntelLookup,
            ReportJson = AiJsonHelper.CoerceJsonObjectForJsonb(reportJson) ?? "{}",
            SchemaVersion = schemaVersion,
            Source = source,
            InvocationLogId = invocationLogId,
            IsLatest = true,
            CreatedBy = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.VendorIntelReports.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapDetailAsync(entity, cancellationToken);
    }

    private async Task<VendorIntelReportDetailDto> MapDetailAsync(
        VendorIntelReport row,
        CancellationToken cancellationToken)
    {
        var summary = (await MapSummariesAsync(new[] { row }, cancellationToken)).First();
        object? report = null;
        try
        {
            report = JsonSerializer.Deserialize<object>(row.ReportJson);
        }
        catch
        {
            report = row.ReportJson;
        }

        return new VendorIntelReportDetailDto
        {
            Id = summary.Id,
            VendorId = summary.VendorId,
            CompanyName = summary.CompanyName,
            CreditCode = summary.CreditCode,
            Source = summary.Source,
            IsLatest = summary.IsLatest,
            CreatedBy = summary.CreatedBy,
            CreatedByUserName = summary.CreatedByUserName,
            CreatedAt = summary.CreatedAt,
            Report = report,
            SchemaVersion = row.SchemaVersion,
            InvocationLogId = row.InvocationLogId,
            FromCache = string.Equals(row.Source, "cache", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static VendorIntelReportDetailDto MapPeerReadOnly(
        string companyName,
        string? creditCode,
        PeerIntelSnapshot peer)
    {
        object? report = null;
        try
        {
            report = JsonSerializer.Deserialize<object>(peer.ReportJson);
        }
        catch
        {
            report = peer.ReportJson;
        }

        return new VendorIntelReportDetailDto
        {
            CompanyName = companyName,
            CreditCode = creditCode,
            Source = "cache",
            IsLatest = true,
            Report = report,
            SchemaVersion = peer.SchemaVersion,
            InvocationLogId = peer.InvocationLogId,
            FromCache = true
        };
    }

    private async Task<IReadOnlyList<VendorIntelReportSummaryDto>> MapSummariesAsync(
        IReadOnlyList<VendorIntelReport> rows,
        CancellationToken cancellationToken)
    {
        var userIds = rows
            .Select(r => r.CreatedBy)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var userNameMap = userIds.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.UserName ?? u.RealName ?? u.Id })
                .ToDictionaryAsync(x => x.Id, x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        return rows.Select(r => new VendorIntelReportSummaryDto
        {
            Id = r.Id,
            VendorId = r.VendorId,
            CompanyName = r.CompanyName,
            CreditCode = r.CreditCode,
            Source = r.Source,
            IsLatest = r.IsLatest,
            CreatedBy = r.CreatedBy,
            CreatedByUserName = r.CreatedBy != null && userNameMap.TryGetValue(r.CreatedBy, out var n) ? n : null,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    private static string ResolveReportJson(AiInvokeResultDto invokeResult)
    {
        if (invokeResult.Data is JsonElement element)
        {
            var fromElement = AiJsonHelper.CoerceJsonObjectForJsonb(element.GetRawText());
            if (fromElement != null)
                return fromElement;
        }

        var fromContent = AiJsonHelper.ExtractJsonObjectText(invokeResult.Content);
        if (fromContent != null)
            return fromContent;

        if (invokeResult.Data != null)
        {
            var serialized = JsonSerializer.Serialize(invokeResult.Data);
            var fromData = AiJsonHelper.CoerceJsonObjectForJsonb(serialized);
            if (fromData != null)
                return fromData;
        }

        return "{}";
    }

    private static string? NormalizeOptional(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}

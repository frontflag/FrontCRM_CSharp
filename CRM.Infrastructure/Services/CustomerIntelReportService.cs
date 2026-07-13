using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Customer;
using CRM.Infrastructure.Ai;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Services;

public sealed class CustomerIntelReportService : ICustomerIntelReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IRepository<CustomerInfo> _customerRepo;

    public CustomerIntelReportService(
        ApplicationDbContext db,
        IAiOrchestrator aiOrchestrator,
        IRepository<CustomerInfo> customerRepo)
    {
        _db = db;
        _aiOrchestrator = aiOrchestrator;
        _customerRepo = customerRepo;
    }

    public async Task<CustomerIntelInvestigateResultDto> InvestigateAsync(
        CustomerIntelInvestigateRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var companyName = (request.CompanyName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(companyName))
            throw new ArgumentException("企业名称不能为空", nameof(request));

        var creditCode = NormalizeOptional(request.CreditCode);
        var region = NormalizeOptional(request.Region);
        var customerId = NormalizeOptional(request.CustomerId);

        if (!string.IsNullOrEmpty(customerId))
        {
            var customer = await _customerRepo.GetByIdAsync(customerId)
                           ?? throw new InvalidOperationException("客户不存在");
            if (string.IsNullOrEmpty(creditCode))
                creditCode = NormalizeOptional(customer.CreditCode);
            if (string.IsNullOrEmpty(region))
                region = NormalizeOptional(customer.City ?? customer.Region);
            if (string.IsNullOrEmpty(companyName))
                companyName = (customer.OfficialName ?? string.Empty).Trim();
        }

        var fingerprint = BuildQueryFingerprint(companyName, creditCode);
        var input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["company_name"] = companyName,
            ["credit_code"] = creditCode,
            ["region"] = region,
            ["customer_id"] = customerId
        };

        var invokeResult = await _aiOrchestrator.InvokeAsync(
            new AiInvokeRequestDto
            {
                ScenarioCode = AiScenarioCodes.CustomerIntelLookup,
                Input = input,
                BizType = "CUSTOMER",
                BizId = customerId,
                TriggerType = AiInvocationTriggerType.Manual,
                ForceRefresh = request.ForceRefresh
            },
            userId,
            cancellationToken);

        var reportJson = ResolveReportJson(invokeResult);

        var saved = await SaveReportAsync(
            customerId,
            companyName,
            creditCode,
            fingerprint,
            reportJson,
            invokeResult.FromCache ? "cache" : "live",
            invokeResult.InvocationId,
            userId,
            cancellationToken);

        saved.FromCache = invokeResult.FromCache;
        return new CustomerIntelInvestigateResultDto
        {
            Report = saved,
            FromCache = invokeResult.FromCache,
            InvocationId = invokeResult.InvocationId
        };
    }

    public async Task<CustomerIntelReportDetailDto?> GetLatestByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var id = customerId.Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var row = await _db.CustomerIntelReports.AsNoTracking()
            .Where(r => r.CustomerId == id && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (row != null)
            return await MapDetailAsync(row, cancellationToken);

        var customer = await _customerRepo.GetByIdAsync(id);
        if (customer == null) return null;

        var name = (customer.OfficialName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name)) return null;

        return await GetLatestByQueryAsync(name, customer.CreditCode, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerIntelReportSummaryDto>> ListByCustomerIdAsync(
        string customerId,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var id = customerId.Trim();
        if (string.IsNullOrEmpty(id)) return Array.Empty<CustomerIntelReportSummaryDto>();

        var rows = await _db.CustomerIntelReports.AsNoTracking()
            .Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        if (rows.Count > 0)
            return await MapSummariesAsync(rows, cancellationToken);

        var customer = await _customerRepo.GetByIdAsync(id);
        if (customer == null) return Array.Empty<CustomerIntelReportSummaryDto>();

        var fingerprint = BuildQueryFingerprint(customer.OfficialName ?? string.Empty, customer.CreditCode);
        rows = await _db.CustomerIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint)
            .OrderByDescending(r => r.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return await MapSummariesAsync(rows, cancellationToken);
    }

    public async Task<CustomerIntelReportDetailDto?> GetByIdAsync(
        string reportId,
        CancellationToken cancellationToken = default)
    {
        var id = reportId.Trim();
        if (string.IsNullOrEmpty(id)) return null;

        var row = await _db.CustomerIntelReports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return row == null ? null : await MapDetailAsync(row, cancellationToken);
    }

    public async Task<CustomerIntelReportDetailDto?> GetLatestByQueryAsync(
        string companyName,
        string? creditCode,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = BuildQueryFingerprint(companyName, creditCode);
        var row = await _db.CustomerIntelReports.AsNoTracking()
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return row == null ? null : await MapDetailAsync(row, cancellationToken);
    }

    internal static string BuildQueryFingerprint(string companyName, string? creditCode)
    {
        var input = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["company_name"] = NormalizeKey(companyName),
            ["credit_code"] = NormalizeKey(creditCode)
        };
        var json = AiJsonHelper.CanonicalFingerprintJson(input, new[] { "company_name", "credit_code" });
        return AiJsonHelper.ComputeSha256Hex(json);
    }

    private async Task<CustomerIntelReportDetailDto> SaveReportAsync(
        string? customerId,
        string companyName,
        string? creditCode,
        string fingerprint,
        string reportJson,
        string source,
        string invocationLogId,
        string? userId,
        CancellationToken cancellationToken)
    {
        await _db.CustomerIntelReports
            .Where(r => r.QueryFingerprint == fingerprint && r.IsLatest)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.IsLatest, false),
                cancellationToken);

        var entity = new CustomerIntelReport
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            CompanyName = companyName,
            CreditCode = creditCode,
            QueryFingerprint = fingerprint,
            ScenarioCode = AiScenarioCodes.CustomerIntelLookup,
            ReportJson = AiJsonHelper.CoerceJsonObjectForJsonb(reportJson) ?? "{}",
            SchemaVersion = "1.0",
            Source = source,
            InvocationLogId = invocationLogId,
            IsLatest = true,
            CreatedBy = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.CustomerIntelReports.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await MapDetailAsync(entity, cancellationToken);
    }

    private async Task<CustomerIntelReportDetailDto> MapDetailAsync(
        CustomerIntelReport row,
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

        return new CustomerIntelReportDetailDto
        {
            Id = summary.Id,
            CustomerId = summary.CustomerId,
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

    private async Task<IReadOnlyList<CustomerIntelReportSummaryDto>> MapSummariesAsync(
        IReadOnlyList<CustomerIntelReport> rows,
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

        return rows.Select(r => new CustomerIntelReportSummaryDto
        {
            Id = r.Id,
            CustomerId = r.CustomerId,
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

    private static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}

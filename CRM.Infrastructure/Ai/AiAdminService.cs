using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Ai;

public sealed class AiAdminService : IAiAdminService
{
    private readonly IRepository<AiProvider> _providerRepo;
    private readonly IRepository<AiPromptTemplate> _templateRepo;
    private readonly IRepository<AiScenario> _scenarioRepo;
    private readonly ApplicationDbContext _db;
    private readonly IRbacService _rbacService;
    private readonly IUnitOfWork _unitOfWork;

    public AiAdminService(
        IRepository<AiProvider> providerRepo,
        IRepository<AiPromptTemplate> templateRepo,
        IRepository<AiScenario> scenarioRepo,
        ApplicationDbContext db,
        IRbacService rbacService,
        IUnitOfWork unitOfWork)
    {
        _providerRepo = providerRepo;
        _templateRepo = templateRepo;
        _scenarioRepo = scenarioRepo;
        _db = db;
        _rbacService = rbacService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AiProviderAdminDto>> ListProvidersAsync(CancellationToken cancellationToken = default)
    {
        var rows = (await _providerRepo.FindAsync(p => !p.IsDeleted))
            .OrderBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return rows.Select(MapProvider).ToList();
    }

    public async Task UpdateProviderAsync(AiProviderAdminDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var row = await _providerRepo.GetByIdAsync(dto.Id.Trim())
                  ?? throw new InvalidOperationException("AI 厂商不存在。");
        row.Name = dto.Name.Trim();
        row.BaseUrl = dto.BaseUrl.Trim();
        row.ApiKeyEnv = string.IsNullOrWhiteSpace(dto.ApiKeyEnv) ? null : dto.ApiKeyEnv.Trim();
        row.DefaultModel = dto.DefaultModel.Trim();
        row.TimeoutSeconds = Math.Clamp(dto.TimeoutSeconds, 5, 600);
        row.IsEnabled = dto.IsEnabled;
        row.ModifyTime = DateTime.UtcNow;
        await _providerRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AiPromptTemplateAdminDto>> ListTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var rows = (await _templateRepo.FindAsync(t => !t.IsDeleted))
            .OrderBy(t => t.Code, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(t => t.Version)
            .ToList();
        return rows.Select(MapTemplate).ToList();
    }

    public async Task UpdateTemplateAsync(AiPromptTemplateAdminDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var row = await _templateRepo.GetByIdAsync(dto.Id.Trim())
                  ?? throw new InvalidOperationException("AI 模板不存在。");
        row.SystemPrompt = dto.SystemPrompt ?? string.Empty;
        row.UserPromptTemplate = dto.UserPromptTemplate ?? string.Empty;
        row.OutputFormat = string.IsNullOrWhiteSpace(dto.OutputFormat) ? AiOutputFormatCode.Json : dto.OutputFormat.Trim();
        row.JsonSchemaHint = dto.JsonSchemaHint;
        row.IsActive = dto.IsActive;
        row.ModifyTime = DateTime.UtcNow;
        await _templateRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AiScenarioAdminDto>> ListScenariosAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var rows = (await _scenarioRepo.FindAsync(s => !s.IsDeleted))
            .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return rows.Select(MapScenario).ToList();
    }

    public async Task UpdateScenarioAsync(AiScenarioAdminDto dto, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var row = await _scenarioRepo.GetByIdAsync(dto.Id.Trim())
                  ?? throw new InvalidOperationException("AI 场景不存在。");
        row.Name = dto.Name.Trim();
        row.Description = dto.Description;
        row.ProviderCode = dto.ProviderCode.Trim();
        row.Model = dto.Model.Trim();
        row.PromptTemplateId = dto.PromptTemplateId.Trim();
        row.CacheTtlSeconds = Math.Max(0, dto.CacheTtlSeconds);
        row.CacheKeyFieldsJson = string.IsNullOrWhiteSpace(dto.CacheKeyFieldsJson) ? "[]" : dto.CacheKeyFieldsJson;
        row.AllowedInputFieldsJson = string.IsNullOrWhiteSpace(dto.AllowedInputFieldsJson) ? "[]" : dto.AllowedInputFieldsJson;
        row.MaxTokens = Math.Clamp(dto.MaxTokens, 256, 8192);
        row.Temperature = Math.Clamp(dto.Temperature, 0m, 2m);
        row.PermissionCode = dto.PermissionCode.Trim();
        row.RateLimitPerUserPerMin = Math.Max(1, dto.RateLimitPerUserPerMin);
        row.IsEnabled = dto.IsEnabled;
        row.ModifyTime = DateTime.UtcNow;
        await _scenarioRepo.UpdateAsync(row);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AiInvocationLogListItemDto>> ListInvocationLogsAsync(
        int take,
        string? scenarioCode,
        CancellationToken cancellationToken = default)
    {
        var n = Math.Clamp(take, 1, 500);
        var code = (scenarioCode ?? string.Empty).Trim();
        var query = _db.AiInvocationLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(code))
            query = query.Where(l => l.ScenarioCode == code);

        var rows = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(n)
            .ToListAsync(cancellationToken);

        return rows.Select(l => new AiInvocationLogListItemDto
        {
            Id = l.Id,
            ScenarioCode = l.ScenarioCode,
            ProviderCode = l.ProviderCode,
            Model = l.Model,
            UserId = l.UserId,
            Status = l.Status,
            FromCache = l.FromCache,
            LatencyMs = l.LatencyMs,
            TotalTokens = l.TotalTokens,
            ErrorMessage = l.ErrorMessage,
            CreatedAt = l.CreatedAt
        }).ToList();
    }

    public async Task<AiUsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default)
    {
        var sinceDay = DateTime.UtcNow.Date;
        var logs = await _db.AiInvocationLogs.AsNoTracking()
            .Where(l => l.CreatedAt >= sinceDay)
            .ToListAsync(cancellationToken);
        var dailyLimit = await _db.AiGlobalConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == AiGlobalConfigKeys.DailyQuotaLimit, cancellationToken);
        return new AiUsageSummaryDto
        {
            TodayInvocationCount = logs.Count(l => !l.FromCache && l.Status != AiInvocationStatusCode.Failed),
            TodayTokenTotal = logs.Where(l => l.TotalTokens.HasValue).Sum(l => l.TotalTokens!.Value),
            TodayCacheHitCount = logs.Count(l => l.FromCache),
            DailyQuotaLimit = int.TryParse(dailyLimit?.ConfigValue, out var n) ? n : 5000
        };
    }

    public async Task<IReadOnlyList<AiScenarioListItemDto>> ListInvokableScenariosForUserAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(userId))
            return Array.Empty<AiScenarioListItemDto>();

        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
        var scenarios = (await _scenarioRepo.FindAsync(s => s.IsEnabled && !s.IsDeleted)).ToList();
        return scenarios
            .Where(s => CanUseScenario(summary, s.PermissionCode))
            .Select(s => new AiScenarioListItemDto
            {
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                PermissionCode = s.PermissionCode
            })
            .OrderBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool CanUseScenario(UserPermissionSummaryDto summary, string permissionCode)
    {
        if (summary.IsSysAdmin)
            return true;
        var perm = (permissionCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(perm))
            return true;
        return summary.PermissionCodes.Any(c => string.Equals(c, perm, StringComparison.OrdinalIgnoreCase));
    }

    private static AiProviderAdminDto MapProvider(AiProvider p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        BaseUrl = p.BaseUrl,
        ApiKeyEnv = p.ApiKeyEnv,
        DefaultModel = p.DefaultModel,
        TimeoutSeconds = p.TimeoutSeconds,
        IsEnabled = p.IsEnabled
    };

    private static AiPromptTemplateAdminDto MapTemplate(AiPromptTemplate t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        Version = t.Version,
        SystemPrompt = t.SystemPrompt,
        UserPromptTemplate = t.UserPromptTemplate,
        OutputFormat = t.OutputFormat,
        JsonSchemaHint = t.JsonSchemaHint,
        IsActive = t.IsActive
    };

    private static AiScenarioAdminDto MapScenario(AiScenario s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        Name = s.Name,
        Description = s.Description,
        ProviderCode = s.ProviderCode,
        Model = s.Model,
        PromptTemplateId = s.PromptTemplateId,
        CacheTtlSeconds = s.CacheTtlSeconds,
        CacheKeyFieldsJson = s.CacheKeyFieldsJson,
        AllowedInputFieldsJson = s.AllowedInputFieldsJson,
        MaxTokens = s.MaxTokens,
        Temperature = s.Temperature,
        PermissionCode = s.PermissionCode,
        RateLimitPerUserPerMin = s.RateLimitPerUserPerMin,
        IsEnabled = s.IsEnabled
    };
}

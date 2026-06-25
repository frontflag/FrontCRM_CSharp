using System.Diagnostics;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Ai;

public sealed class AiOrchestrator : IAiOrchestrator
{
    private readonly IRepository<AiScenario> _scenarioRepo;
    private readonly IRepository<AiPromptTemplate> _templateRepo;
    private readonly IRepository<AiProvider> _providerRepo;
    private readonly ApplicationDbContext _db;
    private readonly IAiLlmProviderFactory _providerFactory;
    private readonly IRbacService _rbacService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        IRepository<AiScenario> scenarioRepo,
        IRepository<AiPromptTemplate> templateRepo,
        IRepository<AiProvider> providerRepo,
        ApplicationDbContext db,
        IAiLlmProviderFactory providerFactory,
        IRbacService rbacService,
        IUnitOfWork unitOfWork,
        ILogger<AiOrchestrator> logger)
    {
        _scenarioRepo = scenarioRepo;
        _templateRepo = templateRepo;
        _providerRepo = providerRepo;
        _db = db;
        _providerFactory = providerFactory;
        _rbacService = rbacService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AiInvokeResultDto> InvokeAsync(
        AiInvokeRequestDto request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var scenarioCode = (request.ScenarioCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(scenarioCode))
            throw new InvalidOperationException("scenarioCode 不能为空。");

        var scenario = (await _scenarioRepo.FindAsync(s => s.Code == scenarioCode && !s.IsDeleted))
            .FirstOrDefault()
                       ?? throw new InvalidOperationException($"AI 场景 {scenarioCode} 不存在。");
        if (!scenario.IsEnabled)
            throw new InvalidOperationException($"AI 场景 {scenarioCode} 已禁用。");

        await EnsurePermissionAsync(userId, scenario.PermissionCode);

        var template = await _templateRepo.GetByIdAsync(scenario.PromptTemplateId.Trim())
                       ?? throw new InvalidOperationException("AI 场景关联的提示词模板不存在。");
        if (template.IsDeleted || !template.IsActive)
            throw new InvalidOperationException("AI 提示词模板不可用。");

        var provider = (await _providerRepo.FindAsync(p => p.Code == scenario.ProviderCode && !p.IsDeleted))
            .FirstOrDefault()
                       ?? throw new InvalidOperationException($"AI 厂商 {scenario.ProviderCode} 不存在。");

        var allowedFields = AiJsonHelper.ParseStringArray(scenario.AllowedInputFieldsJson);
        var cacheKeyFields = AiJsonHelper.ParseStringArray(scenario.CacheKeyFieldsJson);
        var rawInput = request.Input ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var filteredInput = AiJsonHelper.FilterInput(rawInput, allowedFields);

        var uid = (userId ?? string.Empty).Trim();
        await EnsureRateLimitsAsync(uid, scenario, cancellationToken);

        var fingerprintJson = AiJsonHelper.CanonicalFingerprintJson(filteredInput, cacheKeyFields);
        var cacheKey = AiJsonHelper.ComputeSha256Hex(
            $"{scenario.Code}|{scenario.Model}|{template.Version}|ws={(scenario.EnableWebSearch ? 1 : 0)}|{fingerprintJson}");

        if (scenario.CacheTtlSeconds > 0)
        {
            var cached = await TryGetCacheAsync(cacheKey, cancellationToken);
            if (cached != null)
            {
                var cacheLogId = await WriteLogAsync(
                    scenario, provider, template, uid, request, fingerprintJson,
                    string.Empty, null, AiInvocationStatusCode.Cached, true, 0, null, null, cancellationToken);

                return new AiInvokeResultDto
                {
                    InvocationId = cacheLogId,
                    FromCache = true,
                    Content = cached.ResponseContent,
                    Data = AiJsonHelper.TryParseJsonObject(cached.ResponseJson ?? cached.ResponseContent),
                    Usage = null,
                    ScenarioCode = scenario.Code,
                    ProviderCode = cached.ProviderCode,
                    Model = cached.Model
                };
            }
        }

        var systemPrompt = template.SystemPrompt;
        var userPrompt = AiJsonHelper.RenderTemplate(template.UserPromptTemplate, filteredInput);
        if (!string.IsNullOrWhiteSpace(template.JsonSchemaHint))
            userPrompt = userPrompt.TrimEnd() + "\n\n【JSON 结构要求】\n" + template.JsonSchemaHint.Trim();

        if (string.Equals(scenario.Code, AiScenarioCodes.MaterialIntelLookup, StringComparison.OrdinalIgnoreCase))
        {
            systemPrompt = AppendMaterialIntelLanguageGuard(systemPrompt);
            userPrompt = userPrompt.TrimEnd()
                + "\n请将所有描述性字段（meaning、application_areas、technical_features、disclaimer 等）用简体中文输出，英文资料须翻译后再写入 JSON。"
                + " spec_params.datasheet_url 与 spec_params.image_url 须尽量填写可访问的 https 链接，找不到填 null，禁止编造。";
        }
        var messages = new List<AiChatMessageDto>
        {
            new() { Role = "system", Content = systemPrompt },
            new() { Role = "user", Content = userPrompt }
        };

        var promptCombined = systemPrompt + "\n---\n" + userPrompt;
        var promptHash = AiJsonHelper.ComputeSha256Hex(promptCombined);
        var promptPreview = await BuildPromptPreviewAsync(promptCombined, cancellationToken);

        EnsureWebSearchModelCompatible(scenario, provider);

        var sw = Stopwatch.StartNew();
        AiChatCompletionResult? llmResult = null;
        string? error = null;
        try
        {
            var llm = _providerFactory.Create(provider);
            llmResult = await llm.ChatAsync(new AiChatCompletionRequest
            {
                ProviderCode = provider.Code,
                Model = scenario.Model,
                Messages = messages,
                MaxTokens = scenario.MaxTokens,
                Temperature = scenario.Temperature,
                TimeoutSeconds = provider.TimeoutSeconds,
                EnableWebSearch = scenario.EnableWebSearch
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogWarning(ex, "AI invoke failed scenario={Scenario}", scenario.Code);
        }
        finally
        {
            sw.Stop();
        }

        var status = error == null ? AiInvocationStatusCode.Success : AiInvocationStatusCode.Failed;
        var logId = await WriteLogAsync(
            scenario, provider, template, uid, request, fingerprintJson,
            promptHash, promptPreview, status, false, (int)sw.ElapsedMilliseconds, error, llmResult?.Usage, cancellationToken);

        if (error != null)
            throw new InvalidOperationException(error);

        var content = llmResult!.Content ?? string.Empty;
        if (scenario.CacheTtlSeconds > 0 && !string.IsNullOrWhiteSpace(content))
            await SaveCacheAsync(scenario, template, provider, cacheKey, fingerprintJson, content, cancellationToken);

        var parsedData = string.Equals(template.OutputFormat, AiOutputFormatCode.Json, StringComparison.OrdinalIgnoreCase)
            ? AiJsonHelper.TryParseJsonObject(content)
            : null;

        return new AiInvokeResultDto
        {
            InvocationId = logId,
            FromCache = false,
            Content = content,
            Data = parsedData,
            Usage = llmResult.Usage,
            ScenarioCode = scenario.Code,
            ProviderCode = provider.Code,
            Model = scenario.Model
        };
    }

    private static string AppendMaterialIntelLanguageGuard(string systemPrompt)
    {
        const string guard = "【强制语言】part_number_breakdown.meaning、application_areas、technical_features、disclaimer 等描述字段必须全部使用简体中文，禁止英文句子；联网检索到的英文内容须翻译后再输出。";
        if (systemPrompt.Contains("【强制语言】", StringComparison.Ordinal))
            return systemPrompt;
        return systemPrompt.TrimEnd() + "\n\n" + guard;
    }

    private static void EnsureWebSearchModelCompatible(AiScenario scenario, AiProvider provider)
    {
        if (!scenario.EnableWebSearch)
            return;

        var isMoonshot = string.Equals(provider.Code, AiProviderCodes.Moonshot, StringComparison.OrdinalIgnoreCase)
            || (provider.BaseUrl ?? string.Empty).Contains("moonshot", StringComparison.OrdinalIgnoreCase);
        if (!isMoonshot)
            return;

        var model = scenario.Model.Trim();
        if (model.StartsWith("kimi-k2.7", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"联网搜索请使用 kimi-k2.5 或 kimi-k2.6；当前模型 {model} 与 Moonshot $web_search 不兼容。请在「AI 配置 → 场景」中更换 Model 后再查询。");
        }
    }

    private async Task EnsurePermissionAsync(string? userId, string permissionCode)
    {
        var perm = (permissionCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(perm))
            return;
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("未登录，无法调用 AI。");

        var summary = await _rbacService.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return;
        if (summary.PermissionCodes.Any(c => string.Equals(c, perm, StringComparison.OrdinalIgnoreCase)))
            return;
        if (string.Equals(perm, AiPermissionCodes.Admin, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("当前账号无权管理 AI 配置。");
        throw new InvalidOperationException("当前账号无权使用该 AI 场景。");
    }

    private async Task EnsureRateLimitsAsync(string userId, AiScenario scenario, CancellationToken cancellationToken)
    {
        var sinceMinute = DateTime.UtcNow.AddMinutes(-1);
        var sinceDay = DateTime.UtcNow.Date;

        var recentCount = await _db.AiInvocationLogs.AsNoTracking()
            .CountAsync(l =>
                l.ScenarioCode == scenario.Code
                && l.UserId == userId
                && l.CreatedAt >= sinceMinute
                && !l.FromCache, cancellationToken);
        if (recentCount >= Math.Max(1, scenario.RateLimitPerUserPerMin))
            throw new InvalidOperationException($"调用过于频繁，请稍后再试（限制 {scenario.RateLimitPerUserPerMin} 次/分钟）。");

        var dailyLimit = await GetDailyQuotaLimitAsync(cancellationToken);
        if (dailyLimit <= 0)
            return;

        var todayCount = await _db.AiInvocationLogs.AsNoTracking()
            .CountAsync(l =>
                l.CreatedAt >= sinceDay && !l.FromCache && l.Status != AiInvocationStatusCode.Failed, cancellationToken);
        if (todayCount >= dailyLimit)
            throw new InvalidOperationException($"已达全站 AI 日调用配额（{dailyLimit} 次）。");
    }

    private async Task<int> GetDailyQuotaLimitAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AiGlobalConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == AiGlobalConfigKeys.DailyQuotaLimit, cancellationToken);
        return int.TryParse(row?.ConfigValue, out var n) ? n : 5000;
    }

    private async Task<string?> BuildPromptPreviewAsync(string promptCombined, CancellationToken cancellationToken)
    {
        var enabledRow = await _db.AiGlobalConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == AiGlobalConfigKeys.PromptPreviewEnabled, cancellationToken);
        if (!string.Equals(enabledRow?.ConfigValue, "true", StringComparison.OrdinalIgnoreCase))
            return null;
        var maxRow = await _db.AiGlobalConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == AiGlobalConfigKeys.PromptPreviewMaxChars, cancellationToken);
        var max = int.TryParse(maxRow?.ConfigValue, out var n) ? Math.Clamp(n, 0, 500) : 200;
        if (max <= 0)
            return null;
        return promptCombined.Length <= max ? promptCombined : promptCombined[..max];
    }

    private async Task<AiInvocationCache?> TryGetCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var row = await _db.AiInvocationCaches
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey && c.ExpiresAt > now, cancellationToken);
        if (row == null)
            return null;
        row.HitCount += 1;
        await _unitOfWork.SaveChangesAsync();
        return row;
    }

    private async Task SaveCacheAsync(
        AiScenario scenario,
        AiPromptTemplate template,
        AiProvider provider,
        string cacheKey,
        string fingerprintJson,
        string content,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var now = DateTime.UtcNow;
        var existing = await _db.AiInvocationCaches
            .FirstOrDefaultAsync(c => c.CacheKey == cacheKey, cancellationToken);
        var responseJson = string.Equals(template.OutputFormat, AiOutputFormatCode.Json, StringComparison.OrdinalIgnoreCase)
            ? AiJsonHelper.ExtractJsonObjectText(content)
            : null;
        if (responseJson == null
            && string.Equals(template.OutputFormat, AiOutputFormatCode.Json, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning(
                "AI cache skipped response_json: LLM output is not valid JSON scenario={Scenario}",
                scenario.Code);
        }

        if (existing != null)
        {
            existing.ResponseContent = content;
            existing.ResponseJson = responseJson;
            existing.ExpiresAt = now.AddSeconds(scenario.CacheTtlSeconds);
            existing.ProviderCode = provider.Code;
            existing.Model = scenario.Model;
            existing.TemplateVersion = template.Version;
        }
        else
        {
            await _db.AiInvocationCaches.AddAsync(new AiInvocationCache
            {
                Id = Guid.NewGuid().ToString(),
                CacheKey = cacheKey,
                ScenarioCode = scenario.Code,
                RequestFingerprintJson = AiJsonHelper.CoerceJsonObjectForJsonb(fingerprintJson) ?? "{}",
                ResponseContent = content,
                ResponseJson = responseJson,
                ProviderCode = provider.Code,
                Model = scenario.Model,
                TemplateVersion = template.Version,
                HitCount = 0,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(scenario.CacheTtlSeconds)
            }, cancellationToken);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI cache write failed scenario={Scenario}", scenario.Code);
        }
    }

    private async Task<string> WriteLogAsync(
        AiScenario scenario,
        AiProvider provider,
        AiPromptTemplate template,
        string userId,
        AiInvokeRequestDto request,
        string fingerprintJson,
        string promptHash,
        string? promptPreview,
        string status,
        bool fromCache,
        int latencyMs,
        string? error,
        AiTokenUsageDto? usage,
        CancellationToken cancellationToken)
    {
        var log = new AiInvocationLog
        {
            Id = Guid.NewGuid().ToString(),
            ScenarioCode = scenario.Code,
            ProviderCode = provider.Code,
            Model = scenario.Model,
            TemplateVersion = template.Version,
            UserId = string.IsNullOrEmpty(userId) ? null : userId,
            BizType = string.IsNullOrWhiteSpace(request.BizType) ? null : request.BizType.Trim(),
            BizId = string.IsNullOrWhiteSpace(request.BizId) ? null : request.BizId.Trim(),
            RequestFingerprintJson = AiJsonHelper.CoerceJsonObjectForJsonb(fingerprintJson) ?? "{}",
            PromptHash = promptHash,
            PromptPreview = promptPreview,
            Status = status,
            FromCache = fromCache,
            LatencyMs = latencyMs,
            ErrorMessage = error == null ? null : (error.Length > 1000 ? error[..1000] : error),
            PromptTokens = usage?.PromptTokens,
            CompletionTokens = usage?.CompletionTokens,
            TotalTokens = usage?.TotalTokens,
            CreatedAt = DateTime.UtcNow
        };
        await _db.AiInvocationLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync();
        return log.Id;
    }
}

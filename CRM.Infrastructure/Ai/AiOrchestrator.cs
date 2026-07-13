using System.Diagnostics;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using CRM.Infrastructure.Ai.EntityParse;
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
    private readonly IAiEntityParseLogService _entityParseLogService;
    private readonly ILogger<AiOrchestrator> _logger;

    public AiOrchestrator(
        IRepository<AiScenario> scenarioRepo,
        IRepository<AiPromptTemplate> templateRepo,
        IRepository<AiProvider> providerRepo,
        ApplicationDbContext db,
        IAiLlmProviderFactory providerFactory,
        IRbacService rbacService,
        IUnitOfWork unitOfWork,
        IAiEntityParseLogService entityParseLogService,
        ILogger<AiOrchestrator> logger)
    {
        _scenarioRepo = scenarioRepo;
        _templateRepo = templateRepo;
        _providerRepo = providerRepo;
        _db = db;
        _providerFactory = providerFactory;
        _rbacService = rbacService;
        _unitOfWork = unitOfWork;
        _entityParseLogService = entityParseLogService;
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

        if (scenario.CacheTtlSeconds > 0 && !request.ForceRefresh)
        {
            var cached = await TryGetCacheAsync(cacheKey, cancellationToken);
            if (cached != null)
            {
                var cacheLogId = await WriteLogAsync(
                    scenario, provider, template, uid, request, fingerprintJson,
                    string.Empty, null, AiInvocationStatusCode.Cached, true, 0, null, null, cancellationToken);

                return await EnrichEntityParseResultAsync(
                    new AiInvokeResultDto
                    {
                        InvocationId = cacheLogId,
                        FromCache = true,
                        Content = cached.ResponseContent,
                        Data = AiJsonHelper.TryParseJsonObject(cached.ResponseJson ?? cached.ResponseContent),
                        Usage = null,
                        ScenarioCode = scenario.Code,
                        ProviderCode = cached.ProviderCode,
                        Model = cached.Model
                    },
                    scenario, template, uid, request, filteredInput, cached.ResponseContent,
                    cached.ResponseJson ?? cached.ResponseContent, true, 0, cancellationToken);
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
        else if (string.Equals(scenario.Code, AiScenarioCodes.CustomerIntelLookup, StringComparison.OrdinalIgnoreCase))
        {
            systemPrompt = AppendCustomerIntelLanguageGuard(systemPrompt);
            userPrompt = userPrompt.TrimEnd()
                + "\n请使用简体中文输出所有描述性内容。禁止编造司法风险数量、行政处罚或联系方式；查不到填 null 或空数组并标注 confidence: low。";
        }
        else if (string.Equals(scenario.Code, AiScenarioCodes.VendorIntelLookup, StringComparison.OrdinalIgnoreCase))
        {
            systemPrompt = AppendVendorIntelLanguageGuard(systemPrompt);
            userPrompt = userPrompt.TrimEnd()
                + "\n请使用简体中文输出所有描述性内容。从采购与供应链视角评估供应商资质、交付与合规；禁止编造司法风险数量、行政处罚或联系方式；查不到填 null 或空数组并标注 confidence: low。";
        }
        var messages = new List<AiChatMessageDto>
        {
            new() { Role = "system", Content = systemPrompt }
        };

        if (AiEntityParseScenarioCodes.IsBusinessCardScenario(scenario.Code))
        {
            filteredInput.TryGetValue("image_base64", out var imageBase64);
            filteredInput.TryGetValue("image_mime", out var imageMime);
            if (string.IsNullOrWhiteSpace(imageBase64))
                throw new InvalidOperationException("名片图片不能为空。");

            var images = new List<AiChatImagePartDto>
            {
                new()
                {
                    Base64 = imageBase64!.Trim(),
                    MimeType = string.IsNullOrWhiteSpace(imageMime) ? "image/jpeg" : imageMime!.Trim()
                }
            };

            if (filteredInput.TryGetValue("image_base64_2", out var imageBase64Back)
                && !string.IsNullOrWhiteSpace(imageBase64Back))
            {
                filteredInput.TryGetValue("image_mime_2", out var imageMimeBack);
                images.Add(new AiChatImagePartDto
                {
                    Base64 = imageBase64Back.Trim(),
                    MimeType = string.IsNullOrWhiteSpace(imageMimeBack) ? "image/jpeg" : imageMimeBack!.Trim()
                });
            }

            var userContent = string.IsNullOrWhiteSpace(userPrompt) ? "请解析附件名片图片。" : userPrompt;
            if (images.Count > 1)
                userContent += "\n第一张为名片正面，第二张为名片反面，请合并两面全部信息后再输出 JSON。";

            messages.Add(new AiChatMessageDto
            {
                Role = "user",
                Content = userContent,
                Images = images
            });
        }
        else
        {
            messages.Add(new() { Role = "user", Content = userPrompt });
        }

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

        return await EnrichEntityParseResultAsync(
            new AiInvokeResultDto
            {
                InvocationId = logId,
                FromCache = false,
                Content = content,
                Data = parsedData,
                Usage = llmResult.Usage,
                ScenarioCode = scenario.Code,
                ProviderCode = provider.Code,
                Model = scenario.Model
            },
            scenario, template, uid, request, filteredInput, content, content, false,
            (int)sw.ElapsedMilliseconds, cancellationToken);
    }

    private async Task<AiInvokeResultDto> EnrichEntityParseResultAsync(
        AiInvokeResultDto result,
        AiScenario scenario,
        AiPromptTemplate template,
        string userId,
        AiInvokeRequestDto request,
        IReadOnlyDictionary<string, string?> filteredInput,
        string rawLlmContent,
        string parseResultRaw,
        bool fromCache,
        int latencyMs,
        CancellationToken cancellationToken)
    {
        if (!EntityParseNormalizer.IsEntityParseScenario(scenario.Code))
            return result;

        filteredInput.TryGetValue("raw_text", out var rawText);
        if (string.IsNullOrWhiteSpace(rawText) && AiEntityParseScenarioCodes.IsBusinessCardScenario(scenario.Code))
            rawText = filteredInput.ContainsKey("image_base64_2") ? "[business_card_image_dual]" : "[business_card_image]";
        try
        {
            var created = await _entityParseLogService.TryCreateParsedLogAsync(new EntityParseLogCreateRequest
            {
                InvocationId = result.InvocationId,
                ScenarioCode = scenario.Code,
                EntityType = request.BizType,
                UserId = userId,
                ParentBizId = request.BizId,
                RawText = rawText ?? string.Empty,
                ParseResultRaw = parseResultRaw,
                RawLlmObject = result.Data,
                TemplateVersion = template.Version,
                ProviderCode = result.ProviderCode,
                Model = result.Model,
                FromCache = fromCache,
                LatencyMs = latencyMs
            }, cancellationToken);

            if (created != null)
            {
                result.EntityParseLogId = created.LogId;
                if (created.NormalizedData != null)
                    result.Data = created.NormalizedData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI entity parse log write failed scenario={Scenario}", scenario.Code);
        }

        return result;
    }

    private static string AppendMaterialIntelLanguageGuard(string systemPrompt)
    {
        const string guard = "【强制语言】part_number_breakdown.meaning、application_areas、technical_features、disclaimer 等描述字段必须全部使用简体中文，禁止英文句子；联网检索到的英文内容须翻译后再输出。";
        if (systemPrompt.Contains("【强制语言】", StringComparison.Ordinal))
            return systemPrompt;
        return systemPrompt.TrimEnd() + "\n\n" + guard;
    }

    private static string AppendCustomerIntelLanguageGuard(string systemPrompt)
    {
        const string guard = "【强制语言】客户情报报告所有描述性字段必须使用简体中文；JSON 键名保持英文 snake_case；sections[].id 必须使用约定英文 id。";
        if (systemPrompt.Contains("【客户情报强制语言】", StringComparison.Ordinal))
            return systemPrompt;
        return systemPrompt.TrimEnd() + "\n\n" + guard;
    }

    private static string AppendVendorIntelLanguageGuard(string systemPrompt)
    {
        const string guard = "【强制语言】供应商情报报告所有描述性字段必须使用简体中文；JSON 键名保持英文 snake_case；sections[].id 必须使用约定英文 id。";
        if (systemPrompt.Contains("【供应商情报强制语言】", StringComparison.Ordinal))
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
            TriggerType = AiInvocationTriggerType.NormalizeOrDefault(request.TriggerType),
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

    public async Task<bool> IsInvokeCachedAsync(
        AiInvokeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = await ResolveCacheKeyForRequestAsync(request, cancellationToken);
        if (string.IsNullOrEmpty(cacheKey))
            return false;
        return await IsCacheHitReadOnlyAsync(cacheKey, cancellationToken);
    }

    private async Task<string?> ResolveCacheKeyForRequestAsync(
        AiInvokeRequestDto request,
        CancellationToken cancellationToken)
    {
        var scenarioCode = (request.ScenarioCode ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(scenarioCode))
            return null;

        var scenario = (await _scenarioRepo.FindAsync(s => s.Code == scenarioCode && !s.IsDeleted))
            .FirstOrDefault();
        if (scenario == null || !scenario.IsEnabled || scenario.CacheTtlSeconds <= 0)
            return null;

        var template = await _templateRepo.GetByIdAsync(scenario.PromptTemplateId.Trim());
        if (template == null || template.IsDeleted || !template.IsActive)
            return null;

        var allowedFields = AiJsonHelper.ParseStringArray(scenario.AllowedInputFieldsJson);
        var cacheKeyFields = AiJsonHelper.ParseStringArray(scenario.CacheKeyFieldsJson);
        var rawInput = request.Input ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var filteredInput = AiJsonHelper.FilterInput(rawInput, allowedFields);
        var fingerprintJson = AiJsonHelper.CanonicalFingerprintJson(filteredInput, cacheKeyFields);
        return AiJsonHelper.ComputeSha256Hex(
            $"{scenario.Code}|{scenario.Model}|{template.Version}|ws={(scenario.EnableWebSearch ? 1 : 0)}|{fingerprintJson}");
    }

    private async Task<bool> IsCacheHitReadOnlyAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _db.AiInvocationCaches.AsNoTracking()
            .AnyAsync(c => c.CacheKey == cacheKey && c.ExpiresAt > now, cancellationToken);
    }
}

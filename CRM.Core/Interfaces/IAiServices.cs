namespace CRM.Core.Interfaces;

using System.Text.Json;
using CRM.Core.Models.Ai;

public sealed class AiChatImagePartDto
{
    public string Base64 { get; set; } = string.Empty;
    public string MimeType { get; set; } = "image/jpeg";
}

public sealed class AiChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    /// <summary>名片等多模态场景：纯 base64（不含 data: 前缀）。与 Images 二选一，Images 优先。</summary>
    public string? ImageBase64 { get; set; }
    /// <summary>如 image/jpeg、image/png。</summary>
    public string? ImageMimeType { get; set; }
    /// <summary>双面名片等多图场景，按顺序传入（如正面、反面）。</summary>
    public List<AiChatImagePartDto>? Images { get; set; }
}

public sealed class AiChatCompletionRequest
{
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public IReadOnlyList<AiChatMessageDto> Messages { get; set; } = Array.Empty<AiChatMessageDto>();
    public int MaxTokens { get; set; } = 2048;
    public decimal Temperature { get; set; } = 0.3m;
    public int TimeoutSeconds { get; set; } = 120;
    /// <summary>为 true 且厂商为 Moonshot 时，调用内置 $web_search 联网搜索（多轮 tool_calls）。</summary>
    public bool EnableWebSearch { get; set; }
}

public sealed class AiTokenUsageDto
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public sealed class AiChatCompletionResult
{
    public string Content { get; set; } = string.Empty;
    public AiTokenUsageDto? Usage { get; set; }
}

public interface IAiLlmProvider
{
    string ProviderCode { get; }
    Task<AiChatCompletionResult> ChatAsync(AiChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public interface IAiLlmProviderFactory
{
    IAiLlmProvider Create(AiProvider config);
}

public sealed class AiInvokeRequestDto
{
    public string ScenarioCode { get; set; } = string.Empty;
    public Dictionary<string, string?> Input { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? BizType { get; set; }
    public string? BizId { get; set; }
    /// <summary>触发方式：manual=人工主动，auto=系统补刷；缺省为 manual。</summary>
    public string? TriggerType { get; set; }
    /// <summary>为 true 时跳过 PG 调用缓存，强制请求 LLM。</summary>
    public bool ForceRefresh { get; set; }
}

public sealed class AiInvokeResultDto
{
    public string InvocationId { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public string Content { get; set; } = string.Empty;
    public object? Data { get; set; }
    public AiTokenUsageDto? Usage { get; set; }
    public string ScenarioCode { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    /// <summary>entity.parse.* 成功解析后写入 ai_entity_parse_log 的记录 id。</summary>
    public string? EntityParseLogId { get; set; }
}

public sealed class EntityParseLogCreateRequest
{
    public string InvocationId { get; set; } = string.Empty;
    public string ScenarioCode { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? UserId { get; set; }
    public string? ParentBizId { get; set; }
    public string? RawText { get; set; }
    public string? ParseResultRaw { get; set; }
    public object? RawLlmObject { get; set; }
    public int TemplateVersion { get; set; } = 1;
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public int LatencyMs { get; set; }
}

public sealed class EntityParseLogCreateResult
{
    public string LogId { get; set; } = string.Empty;
    public object? NormalizedData { get; set; }
}

public sealed class AiEntityParseLogConfirmDto
{
    public JsonElement ConfirmedFields { get; set; }
}

public sealed class AiEntityParseLogSavedDto
{
    public string SavedBizId { get; set; } = string.Empty;
}

public sealed class AiEntityParseLogQueryDto
{
    public int Take { get; set; } = 50;
    public string? ScenarioCode { get; set; }
    public string? EntityType { get; set; }
    public string? Outcome { get; set; }
    public string? UserId { get; set; }
}

public class AiEntityParseLogListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string InvocationId { get; set; } = string.Empty;
    public string ScenarioCode { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ParentBizType { get; set; }
    public string? ParentBizId { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? SavedBizId { get; set; }
    public int RawTextLength { get; set; }
    public bool FromCache { get; set; }
    public int LatencyMs { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? SavedAt { get; set; }
}

public sealed class AiEntityParseLogDetailDto : AiEntityParseLogListItemDto
{
    public string RawText { get; set; } = string.Empty;
    public object? ParseResultJson { get; set; }
    public object? ConfirmedFieldsJson { get; set; }
    public string? ParseResultRaw { get; set; }
}

public interface IAiEntityParseLogService
{
    Task<EntityParseLogCreateResult?> TryCreateParsedLogAsync(
        EntityParseLogCreateRequest request,
        CancellationToken cancellationToken = default);

    Task ConfirmAsync(
        string logId,
        string userId,
        JsonElement confirmedFields,
        CancellationToken cancellationToken = default);

    Task MarkSavedAsync(
        string logId,
        string userId,
        string savedBizId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AiEntityParseLogListItemDto>> ListForAdminAsync(
        AiEntityParseLogQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AiEntityParseLogDetailDto?> GetDetailForAdminAsync(
        string logId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        AiEntityParseLogQueryDto query,
        CancellationToken cancellationToken = default);

    Task<int> PurgeOlderThanAsync(
        int keepDays,
        CancellationToken cancellationToken = default);
}

public interface IAiOrchestrator
{
    Task<AiInvokeResultDto> InvokeAsync(AiInvokeRequestDto request, string? userId, CancellationToken cancellationToken = default);

    /// <summary>判断指定场景与输入是否已有未过期的 AI 响应缓存（不写入日志、不递增命中计数）。</summary>
    Task<bool> IsInvokeCachedAsync(AiInvokeRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class AiScenarioListItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
}

public sealed class AiProviderAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKeyEnv { get; set; }
    public string DefaultModel { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class AiPromptTemplateAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "json";
    public string? JsonSchemaHint { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AiScenarioAdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptTemplateId { get; set; } = string.Empty;
    public int CacheTtlSeconds { get; set; }
    public string CacheKeyFieldsJson { get; set; } = "[]";
    public string AllowedInputFieldsJson { get; set; } = "[]";
    public int MaxTokens { get; set; }
    public decimal Temperature { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public int RateLimitPerUserPerMin { get; set; }
    public bool IsEnabled { get; set; }
    public bool EnableWebSearch { get; set; }
}

public sealed class AiInvocationLogListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string ScenarioCode { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? UserId { get; set; }
    /// <summary>执行 AI 功能的员工登录账号。</summary>
    public string? ExecutorUserName { get; set; }
    public string? TriggerType { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public int LatencyMs { get; set; }
    public int? TotalTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AiUsageSummaryDto
{
    public int TodayInvocationCount { get; set; }
    public int TodayTokenTotal { get; set; }
    public int TodayCacheHitCount { get; set; }
    public int DailyQuotaLimit { get; set; }
}

public interface IAiAdminService
{
    Task<IReadOnlyList<AiProviderAdminDto>> ListProvidersAsync(CancellationToken cancellationToken = default);
    Task UpdateProviderAsync(AiProviderAdminDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiPromptTemplateAdminDto>> ListTemplatesAsync(CancellationToken cancellationToken = default);
    Task UpdateTemplateAsync(AiPromptTemplateAdminDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiScenarioAdminDto>> ListScenariosAsync(CancellationToken cancellationToken = default);
    Task UpdateScenarioAsync(AiScenarioAdminDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiInvocationLogListItemDto>> ListInvocationLogsAsync(
        int take,
        string? scenarioCode,
        string? triggerType = null,
        CancellationToken cancellationToken = default);
    Task<AiUsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiScenarioListItemDto>> ListInvokableScenariosForUserAsync(string? userId, CancellationToken cancellationToken = default);
}

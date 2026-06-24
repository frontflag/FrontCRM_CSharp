namespace CRM.Core.Interfaces;

using CRM.Core.Models.Ai;

public sealed class AiChatMessageDto
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public sealed class AiChatCompletionRequest
{
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public IReadOnlyList<AiChatMessageDto> Messages { get; set; } = Array.Empty<AiChatMessageDto>();
    public int MaxTokens { get; set; } = 2048;
    public decimal Temperature { get; set; } = 0.3m;
    public int TimeoutSeconds { get; set; } = 120;
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
}

public interface IAiOrchestrator
{
    Task<AiInvokeResultDto> InvokeAsync(AiInvokeRequestDto request, string? userId, CancellationToken cancellationToken = default);
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
}

public sealed class AiInvocationLogListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string ScenarioCode { get; set; } = string.Empty;
    public string ProviderCode { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? UserId { get; set; }
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
    Task<IReadOnlyList<AiInvocationLogListItemDto>> ListInvocationLogsAsync(int take, string? scenarioCode, CancellationToken cancellationToken = default);
    Task<AiUsageSummaryDto> GetUsageSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiScenarioListItemDto>> ListInvokableScenariosForUserAsync(string? userId, CancellationToken cancellationToken = default);
}

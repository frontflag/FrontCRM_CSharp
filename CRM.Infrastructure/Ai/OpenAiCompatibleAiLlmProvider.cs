using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Ai;

public sealed class OpenAiCompatibleAiLlmProvider : IAiLlmProvider
{
    private readonly AiProvider _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAiSecretResolver _secretResolver;
    private readonly ILogger<OpenAiCompatibleAiLlmProvider> _logger;

    public OpenAiCompatibleAiLlmProvider(
        AiProvider config,
        IHttpClientFactory httpClientFactory,
        IAiSecretResolver secretResolver,
        ILogger<OpenAiCompatibleAiLlmProvider> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _secretResolver = secretResolver;
        _logger = logger;
    }

    public string ProviderCode => _config.Code;

    public async Task<AiChatCompletionResult> ChatAsync(AiChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _secretResolver.ResolveApiKey(_config.ApiKeyEnv);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"AI 厂商 {_config.Code} 未配置 API Key 环境变量 {_config.ApiKeyEnv ?? "(null)"}。");

        var baseUrl = (_config.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException($"AI 厂商 {_config.Code} 未配置 base_url。");

        var client = _httpClientFactory.CreateClient($"ai-{_config.Code}");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 600));

        var model = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;
        using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpReq.Content = JsonContent.Create(new
        {
            model,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
            max_tokens = request.MaxTokens,
            temperature = ResolveRequestTemperature(model, request.Temperature),
            stream = false
        });

        using var resp = await client.SendAsync(httpReq, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("AI provider {Provider} HTTP {Status}: {Body}", _config.Code, (int)resp.StatusCode, Truncate(body, 500));
            throw new InvalidOperationException($"AI 调用失败 ({(int)resp.StatusCode}): {Truncate(body, 300)}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body);
        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        return new AiChatCompletionResult
        {
            Content = content,
            Usage = parsed?.Usage == null
                ? null
                : new AiTokenUsageDto
                {
                    PromptTokens = parsed.Usage.PromptTokens,
                    CompletionTokens = parsed.Usage.CompletionTokens,
                    TotalTokens = parsed.Usage.TotalTokens
                }
        };
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

    /// <summary>kimi-k2.x 系列（含 kimi-k2.5）仅允许 temperature=1。</summary>
    private static double ResolveRequestTemperature(string? model, decimal requested)
    {
        var name = (model ?? string.Empty).Trim();
        if (name.StartsWith("kimi-k2", StringComparison.OrdinalIgnoreCase))
            return 1.0;
        return (double)requested;
    }

    private sealed class OpenAiChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAiChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAiUsage? Usage { get; set; }
    }

    private sealed class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OpenAiUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}

public interface IAiSecretResolver
{
    string? ResolveApiKey(string? apiKeyEnv);
}

public sealed class ConfigurationAiSecretResolver : IAiSecretResolver
{
    public string? ResolveApiKey(string? apiKeyEnv)
    {
        if (string.IsNullOrWhiteSpace(apiKeyEnv))
            return null;
        return Environment.GetEnvironmentVariable(apiKeyEnv.Trim());
    }
}

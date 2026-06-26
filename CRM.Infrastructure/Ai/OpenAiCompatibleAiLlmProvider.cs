using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Ai;

public sealed class OpenAiCompatibleAiLlmProvider : IAiLlmProvider
{
    private const int WebSearchMaxRounds = 4;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
        if (request.EnableWebSearch && SupportsMoonshotWebSearch())
            return await ChatWithMoonshotWebSearchAsync(request, cancellationToken);

        return await ChatOnceAsync(request, cancellationToken);
    }

    private bool SupportsMoonshotWebSearch() =>
        string.Equals(_config.Code, AiProviderCodes.Moonshot, StringComparison.OrdinalIgnoreCase)
        || (_config.BaseUrl ?? string.Empty).Contains("moonshot", StringComparison.OrdinalIgnoreCase);

    private async Task<AiChatCompletionResult> ChatOnceAsync(
        AiChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var (parsed, _) = await PostChatCompletionsAsync(
            BuildStandardPayload(request),
            request,
            cancellationToken);

        var choice = parsed?.Choices?.FirstOrDefault();
        var content = choice?.Message?.Content ?? string.Empty;
        return new AiChatCompletionResult
        {
            Content = content,
            Usage = MapUsage(parsed?.Usage)
        };
    }

    private async Task<AiChatCompletionResult> ChatWithMoonshotWebSearchAsync(
        AiChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var model = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;
        WarnIfWebSearchModelMismatch(model);

        var messages = BuildMessageArray(request.Messages);
        AiTokenUsageDto? totalUsage = null;
        OpenAiChatResponse? lastResponse = null;

        for (var round = 0; round < WebSearchMaxRounds; round++)
        {
            var payload = BuildStandardPayload(request);
            // 每轮深拷贝，避免 messages 已挂在上一轮 payload 上导致 "The node already has a parent"
            payload["messages"] = (JsonArray)messages.DeepClone();
            payload["tools"] = BuildMoonshotWebSearchTools();
            ApplyWebSearchThinking(payload, model);

            var (parsed, rawBody) = await PostChatCompletionsAsync(payload, request, cancellationToken);
            lastResponse = parsed;
            totalUsage = MergeUsage(totalUsage, MapUsage(parsed?.Usage));

            using var doc = JsonDocument.Parse(rawBody);
            var choiceEl = doc.RootElement.GetProperty("choices")[0];
            var finishReason = choiceEl.TryGetProperty("finish_reason", out var frEl)
                ? frEl.GetString()
                : null;
            if (!choiceEl.TryGetProperty("message", out var messageEl))
                throw new InvalidOperationException("AI 联网搜索返回空消息。");

            if (IsToolCallFinish(finishReason) && messageEl.TryGetProperty("tool_calls", out var toolCallsEl)
                && toolCallsEl.ValueKind == JsonValueKind.Array && toolCallsEl.GetArrayLength() > 0)
            {
                messages.Add(JsonNode.Parse(messageEl.GetRawText())!.AsObject());
                foreach (var toolCallEl in toolCallsEl.EnumerateArray())
                {
                    var toolCallId = toolCallEl.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                    if (string.IsNullOrWhiteSpace(toolCallId))
                        continue;

                    var toolName = "$web_search";
                    var toolArgs = "{}";
                    if (toolCallEl.TryGetProperty("function", out var fnEl))
                    {
                        if (fnEl.TryGetProperty("name", out var nameEl) && !string.IsNullOrWhiteSpace(nameEl.GetString()))
                            toolName = nameEl.GetString()!;
                        if (fnEl.TryGetProperty("arguments", out var argsEl))
                            toolArgs = argsEl.ValueKind == JsonValueKind.String
                                ? argsEl.GetString() ?? "{}"
                                : argsEl.GetRawText();
                    }

                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = toolCallId,
                        ["name"] = toolName,
                        ["content"] = toolArgs
                    });
                }

                _logger.LogInformation(
                    "Moonshot web search round {Round} tool_calls={Count} model={Model}",
                    round + 1,
                    toolCallsEl.GetArrayLength(),
                    model);
                continue;
            }

            var content = messageEl.TryGetProperty("content", out var contentEl)
                ? contentEl.GetString() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException($"AI 联网搜索未返回有效内容: {Truncate(rawBody, 300)}");

            return new AiChatCompletionResult
            {
                Content = content,
                Usage = totalUsage
            };
        }

        var fallback = lastResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return new AiChatCompletionResult
            {
                Content = fallback,
                Usage = totalUsage
            };
        }

        throw new InvalidOperationException("AI 联网搜索轮次过多，未获得最终结果。");
    }

    /// <summary>
    /// kimi-k2.5/k2.6 联网需 disabled；kimi-k2.7* 仅允许 enabled；其余模型不传 thinking。
    /// </summary>
    private static void ApplyWebSearchThinking(JsonObject payload, string? model)
    {
        var name = (model ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            return;

        if (RequiresWebSearchThinkingDisabled(name))
        {
            payload["thinking"] = new JsonObject { ["type"] = "disabled" };
            return;
        }

        if (RequiresWebSearchThinkingEnabled(name))
            payload["thinking"] = new JsonObject { ["type"] = "enabled" };
    }

    private static bool RequiresWebSearchThinkingDisabled(string model) =>
        model.Equals("kimi-k2.5", StringComparison.OrdinalIgnoreCase)
        || model.Equals("kimi-k2.6", StringComparison.OrdinalIgnoreCase);

    private static bool RequiresWebSearchThinkingEnabled(string model) =>
        model.StartsWith("kimi-k2.7", StringComparison.OrdinalIgnoreCase);

    private void WarnIfWebSearchModelMismatch(string? model)
    {
        var name = (model ?? string.Empty).Trim();
        if (name.StartsWith("kimi-k2.7", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Moonshot web search with model {Model} may be unstable; prefer kimi-k2.5 or kimi-k2.6 per official docs.",
                name);
        }
    }

    private async Task<(OpenAiChatResponse? Parsed, string RawBody)> PostChatCompletionsAsync(
        JsonObject payload,
        AiChatCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey();
        var baseUrl = ResolveBaseUrl();
        var client = _httpClientFactory.CreateClient($"ai-{_config.Code}");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 600));

        var model = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;
        payload["model"] ??= model;

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpReq.Content = new StringContent(payload.ToJsonString(JsonOpts), Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(httpReq, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("AI provider {Provider} HTTP {Status}: {Body}", _config.Code, (int)resp.StatusCode, Truncate(body, 500));
            throw new InvalidOperationException($"AI 调用失败 ({(int)resp.StatusCode}): {Truncate(body, 300)}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAiChatResponse>(body);
        return (parsed, body);
    }

    private JsonObject BuildStandardPayload(AiChatCompletionRequest request)
    {
        var model = string.IsNullOrWhiteSpace(request.Model) ? _config.DefaultModel : request.Model;
        return new JsonObject
        {
            ["model"] = model,
            ["messages"] = BuildMessageArray(request.Messages),
            ["max_tokens"] = request.MaxTokens,
            ["temperature"] = ResolveRequestTemperature(model, request.Temperature, request.EnableWebSearch),
            ["stream"] = false
        };
    }

    private static JsonArray BuildMessageArray(IReadOnlyList<AiChatMessageDto> messages)
    {
        var arr = new JsonArray();
        foreach (var message in messages)
        {
            var imageParts = message.Images?
                .Where(i => !string.IsNullOrWhiteSpace(i.Base64))
                .ToList();
            if (imageParts is { Count: > 0 })
            {
                var contentArr = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = message.Content ?? string.Empty
                    }
                };
                foreach (var image in imageParts)
                {
                    var mime = string.IsNullOrWhiteSpace(image.MimeType) ? "image/jpeg" : image.MimeType.Trim();
                    var dataUrl = $"data:{mime};base64,{image.Base64.Trim()}";
                    contentArr.Add(new JsonObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JsonObject { ["url"] = dataUrl }
                    });
                }

                arr.Add(new JsonObject
                {
                    ["role"] = message.Role,
                    ["content"] = contentArr
                });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(message.ImageBase64))
            {
                var mime = string.IsNullOrWhiteSpace(message.ImageMimeType) ? "image/jpeg" : message.ImageMimeType.Trim();
                var dataUrl = $"data:{mime};base64,{message.ImageBase64.Trim()}";
                arr.Add(new JsonObject
                {
                    ["role"] = message.Role,
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = message.Content ?? string.Empty
                        },
                        new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject
                            {
                                ["url"] = dataUrl
                            }
                        }
                    }
                });
                continue;
            }

            arr.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content
            });
        }

        return arr;
    }

    private static JsonArray BuildMoonshotWebSearchTools() =>
        new(new JsonObject
        {
            ["type"] = "builtin_function",
            ["function"] = new JsonObject { ["name"] = "$web_search" }
        });

    private static bool IsToolCallFinish(string? finishReason) =>
        string.Equals(finishReason, "tool_calls", StringComparison.OrdinalIgnoreCase);

    private string ResolveApiKey()
    {
        var apiKey = _secretResolver.ResolveApiKey(_config.ApiKeyEnv);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"AI 厂商 {_config.Code} 未配置 API Key 环境变量 {_config.ApiKeyEnv ?? "(null)"}。");
        return apiKey;
    }

    private string ResolveBaseUrl()
    {
        var baseUrl = (_config.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException($"AI 厂商 {_config.Code} 未配置 base_url。");
        return baseUrl;
    }

    private static AiTokenUsageDto? MapUsage(OpenAiUsage? usage) =>
        usage == null
            ? null
            : new AiTokenUsageDto
            {
                PromptTokens = usage.PromptTokens,
                CompletionTokens = usage.CompletionTokens,
                TotalTokens = usage.TotalTokens
            };

    private static AiTokenUsageDto? MergeUsage(AiTokenUsageDto? left, AiTokenUsageDto? right)
    {
        if (left == null)
            return right;
        if (right == null)
            return left;
        return new AiTokenUsageDto
        {
            PromptTokens = left.PromptTokens + right.PromptTokens,
            CompletionTokens = left.CompletionTokens + right.CompletionTokens,
            TotalTokens = left.TotalTokens + right.TotalTokens
        };
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max];

    /// <summary>
    /// kimi-k2.x 普通调用仅允许 temperature=1；
    /// kimi-k2.5/k2.6 开启 $web_search 时 API 仅允许 0.6。
    /// </summary>
    private static double ResolveRequestTemperature(string? model, decimal requested, bool enableWebSearch)
    {
        var name = (model ?? string.Empty).Trim();
        if (enableWebSearch && RequiresWebSearchThinkingDisabled(name))
            return 0.6;
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
        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        public List<OpenAiToolCall>? ToolCalls { get; set; }
    }

    private sealed class OpenAiToolCall
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("function")]
        public OpenAiToolFunction? Function { get; set; }
    }

    private sealed class OpenAiToolFunction
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
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

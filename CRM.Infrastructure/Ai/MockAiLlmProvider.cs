using CRM.Core.Constants;
using CRM.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Ai;

public sealed class MockAiLlmProvider : IAiLlmProvider
{
    private readonly ILogger<MockAiLlmProvider> _logger;

    public MockAiLlmProvider(ILogger<MockAiLlmProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderCode => AiProviderCodes.Mock;

    public Task<AiChatCompletionResult> ChatAsync(AiChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var userMsg = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var pn = ExtractBetween(userMsg, "PN=", "，") ?? ExtractBetween(userMsg, "PN=", ",") ?? "UNKNOWN";
        var brand = ExtractBetween(userMsg, "品牌=", "。") ?? ExtractBetween(userMsg, "品牌=", ".") ?? "UNKNOWN";

        var json = $$"""
                     {
                       "package": "Mock-SOIC-8",
                       "voltage": "2.7V-5.5V",
                       "temperature_range": "-40°C to +125°C",
                       "description": "Mock response for PN={{pn}} brand={{brand}}. Replace provider with moonshot for live lookup.",
                       "confidence": "low",
                       "disclaimer": "This is mock data for development only."
                     }
                     """;

        return Task.FromResult(new AiChatCompletionResult
        {
            Content = json,
            Usage = new AiTokenUsageDto { PromptTokens = 50, CompletionTokens = 120, TotalTokens = 170 }
        });
    }

    private static string? ExtractBetween(string text, string start, string end)
    {
        var i = text.IndexOf(start, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return null;
        i += start.Length;
        var j = text.IndexOf(end, i, StringComparison.Ordinal);
        if (j < 0)
            return text[i..].Trim();
        return text[i..j].Trim();
    }
}

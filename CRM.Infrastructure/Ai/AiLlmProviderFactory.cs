using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Infrastructure.Ai;

public sealed class AiLlmProviderFactory : IAiLlmProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public AiLlmProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAiLlmProvider Create(AiProvider config)
    {
        var code = (config.Code ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(code))
            throw new InvalidOperationException("AI 厂商 code 不能为空。");
        if (!config.IsEnabled)
            throw new InvalidOperationException($"AI 厂商 {code} 已禁用。");

        if (string.Equals(code, AiProviderCodes.Mock, StringComparison.OrdinalIgnoreCase))
            return _serviceProvider.GetRequiredService<MockAiLlmProvider>();

        return new OpenAiCompatibleAiLlmProvider(
            config,
            _serviceProvider.GetRequiredService<IHttpClientFactory>(),
            _serviceProvider.GetRequiredService<IAiSecretResolver>(),
            _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiCompatibleAiLlmProvider>>());
    }
}

namespace CRM.Core.Utilities;

/// <summary>
/// 将厂商 HTTP 错误转成业务用户可见文案，避免把组织号、密钥片段、原始 JSON 暴露到界面。
/// 完整响应体只应写服务端日志。
/// </summary>
public static class AiProviderUserError
{
    public const string Quota =
        "AI 服务额度不足或账号已暂停，请联系管理员检查套餐与余额后重试。";

    public const string Auth =
        "AI 服务鉴权失败，请联系管理员检查密钥配置。";

    public const string Unavailable =
        "AI 服务暂时不可用，请稍后重试。";

    public const string Generic =
        "AI 调用失败，请稍后重试。";

    public static string FromHttp(int statusCode, string? responseBody)
    {
        var body = responseBody ?? string.Empty;
        if (statusCode == 429 || LooksLikeQuota(body))
            return Quota;
        if (statusCode is 401 or 403 || LooksLikeAuth(body))
            return Auth;
        if (statusCode >= 500)
            return Unavailable;
        return Generic;
    }

    private static bool LooksLikeQuota(string body) =>
        ContainsIgnoreCase(body, "insufficient balance")
        || ContainsIgnoreCase(body, "exceeded_current_quota")
        || ContainsIgnoreCase(body, "exceeded your current quota")
        || ContainsIgnoreCase(body, "quota_exceeded")
        || (ContainsIgnoreCase(body, "suspended") && ContainsIgnoreCase(body, "recharge"));

    private static bool LooksLikeAuth(string body) =>
        ContainsIgnoreCase(body, "invalid authentication")
        || ContainsIgnoreCase(body, "incorrect api key")
        || ContainsIgnoreCase(body, "invalid_api_key");

    private static bool ContainsIgnoreCase(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

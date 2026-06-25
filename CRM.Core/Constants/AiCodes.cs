namespace CRM.Core.Constants;

public static class AiProviderCodes
{
    public const string Mock = "mock";
    public const string Moonshot = "moonshot";
}

public static class AiScenarioCodes
{
    public const string MaterialSpecLookup = "material.spec.lookup";
    public const string MaterialIntelLookup = "material.intel.lookup";
}

public static class AiPermissionCodes
{
    public const string Admin = "biz.ai.admin";
    public const string MaterialSpecLookup = "biz.ai.material_spec.lookup";
    public const string MaterialIntelLookup = "biz.ai.material_intel.lookup";
}

public static class AiOutputFormatCode
{
    public const string Text = "text";
    public const string Json = "json";
}

public static class AiInvocationStatusCode
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Cached = "cached";
}

public static class AiGlobalConfigKeys
{
    public const string DailyQuotaLimit = "daily_quota_limit";
    public const string PromptPreviewEnabled = "prompt_preview_enabled";
    public const string PromptPreviewMaxChars = "prompt_preview_max_chars";
}

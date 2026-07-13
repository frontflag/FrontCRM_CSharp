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
    public const string CustomerIntelLookup = "customer.intel.lookup";
}

public static class AiPermissionCodes
{
    public const string Admin = "biz.ai.admin";
    public const string MaterialSpecLookup = "biz.ai.material_spec.lookup";
    public const string MaterialIntelLookup = "biz.ai.material_intel.lookup";
    public const string CustomerIntelLookup = "biz.ai.customer_intel.lookup";
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

/// <summary>AI 调用触发方式：人工主动 vs 界面联动补刷。</summary>
public static class AiInvocationTriggerType
{
    public const string Manual = "manual";
    public const string Auto = "auto";

    public static string NormalizeOrDefault(string? raw) =>
        string.Equals(raw?.Trim(), Auto, StringComparison.OrdinalIgnoreCase) ? Auto : Manual;
}

public static class AiGlobalConfigKeys
{
    public const string DailyQuotaLimit = "daily_quota_limit";
    public const string PromptPreviewEnabled = "prompt_preview_enabled";
    public const string PromptPreviewMaxChars = "prompt_preview_max_chars";
    public const string EntityParseLogRetentionDays = "entity_parse_log_retention_days";
}

public static class AiEntityParseOutcomeCode
{
    public const string Parsed = "parsed";
    public const string Confirmed = "confirmed";
    public const string Failed = "failed";
    public const string Saved = "saved";
}

public static class AiEntityParseScenarioCodes
{
    public const string Prefix = "entity.parse.";

    public const string Customer = "entity.parse.customer";
    public const string Rfq = "entity.parse.rfq";
    public const string Vendor = "entity.parse.vendor";
    public const string CustomerContact = "entity.parse.customer_contact";
    public const string VendorContact = "entity.parse.vendor_contact";
    public const string CustomerAddress = "entity.parse.customer_address";
    public const string VendorAddress = "entity.parse.vendor_address";
    public const string CustomerBusinessCard = "entity.parse.customer_business_card";
    public const string VendorBusinessCard = "entity.parse.vendor_business_card";

    public static bool IsBusinessCardScenario(string scenarioCode) =>
        string.Equals(scenarioCode, CustomerBusinessCard, StringComparison.OrdinalIgnoreCase)
        || string.Equals(scenarioCode, VendorBusinessCard, StringComparison.OrdinalIgnoreCase);
}

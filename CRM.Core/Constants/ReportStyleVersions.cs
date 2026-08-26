namespace CRM.Core.Constants;

/// <summary>报表样式版本（sysparam <see cref="SysParamCodes.ReportStyleVersion"/>）。</summary>
public static class ReportStyleVersions
{
    public const string V1 = "V1";
    public const string V2 = "V2";
    public const string Default = V1;

    public static bool IsAllowed(string? value)
    {
        var v = value?.Trim();
        return string.Equals(v, V1, StringComparison.Ordinal)
            || string.Equals(v, V2, StringComparison.Ordinal);
    }

    /// <summary>未配置或非法值回落为 V1。</summary>
    public static string NormalizeOrDefault(string? value)
    {
        var v = value?.Trim();
        return IsAllowed(v) ? v! : Default;
    }

    /// <summary>非法值抛 <see cref="ArgumentException"/>（API 映射 400）。</summary>
    public static string RequireAllowed(string? value)
    {
        var v = value?.Trim();
        if (!IsAllowed(v))
            throw new ArgumentException("报表样式版本仅允许 V1 或 V2");
        return v!;
    }
}

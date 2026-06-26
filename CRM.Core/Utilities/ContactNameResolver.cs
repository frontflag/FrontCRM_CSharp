namespace CRM.Core.Utilities;

/// <summary>
/// 客户/供应商联系人中文名、英文名解析与校验（至少填写一项）。
/// </summary>
public static class ContactNameResolver
{
    public const string AtLeastOneRequiredMessage = "中文名与英文名至少填写一项";

    public static (string? CName, string? EName) ResolveForCreate(
        string? cName,
        string? eName,
        string? legacyName = null,
        string? legacyContactName = null)
    {
        cName = NullIfWhiteSpace(cName);
        eName = NullIfWhiteSpace(eName);
        if (cName == null && eName == null)
            cName = NullIfWhiteSpace(legacyName) ?? NullIfWhiteSpace(legacyContactName);
        if (cName == null && eName == null)
            throw new ArgumentException(AtLeastOneRequiredMessage);
        return (cName, eName);
    }

    /// <summary>
    /// 更新联系人姓名：仅当 request 中对应字段非 null 时覆盖（null 表示未提交、不修改）。
    /// legacyName / legacyContactName 仅在未提交 cName/eName 时作为 CName 回退。
    /// </summary>
    public static (string? CName, string? EName) ResolveForUpdate(
        string? currentCName,
        string? currentEName,
        string? requestCName,
        string? requestEName,
        string? legacyName = null,
        string? legacyContactName = null)
    {
        var cName = requestCName != null ? NullIfWhiteSpace(requestCName) : NullIfWhiteSpace(currentCName);
        var eName = requestEName != null ? NullIfWhiteSpace(requestEName) : NullIfWhiteSpace(currentEName);

        if (requestCName == null && requestEName == null)
        {
            var legacy = NullIfWhiteSpace(legacyName) ?? NullIfWhiteSpace(legacyContactName);
            if (legacy != null)
                cName = legacy;
        }

        if (cName == null && eName == null)
            throw new ArgumentException(AtLeastOneRequiredMessage);
        return (cName, eName);
    }

    public static bool HasAnyName(string? cName, string? eName) =>
        !string.IsNullOrWhiteSpace(cName) || !string.IsNullOrWhiteSpace(eName);

    private static string? NullIfWhiteSpace(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

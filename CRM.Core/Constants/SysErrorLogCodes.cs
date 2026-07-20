namespace CRM.Core.Constants;

using CRM.Core.Models.System;

public static class SysErrorLogPermissionCodes
{
    public const string Read = "sys.errorlog.read";
    public const string Resolve = "sys.errorlog.resolve";
}

public static class SysErrorLogHttpKeys
{
    /// <summary>HttpContext.Items：本请求已落库的错误编号，如 E-123。</summary>
    public const string ErrorIdItem = "SysErrorLog.ErrorId";
}

public static class SysErrorLogIdFormat
{
    public static string Format(long id) => $"E-{id}";

    public static bool TryParse(string? raw, out long id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var s = raw.Trim();
        if (s.StartsWith("E-", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        return long.TryParse(s, out id) && id > 0;
    }
}

/// <summary>标记处理时的固定备注（与前端忽略操作一致）。</summary>
public static class SysErrorLogResolveRemarks
{
    public const string Ignore = "忽略";
}

/// <summary>运维列表筛选：未处理 / 已处理 / 忽略（互斥）。</summary>
public static class SysErrorLogFilterStatus
{
    public const string Open = "open";
    public const string Resolved = "resolved";
    public const string Ignored = "ignored";

    public static string Resolve(SysErrorLog log)
    {
        if (!log.IsResolved) return Open;
        return string.Equals(log.ResolveRemark, SysErrorLogResolveRemarks.Ignore, StringComparison.Ordinal)
            ? Ignored
            : Resolved;
    }
}

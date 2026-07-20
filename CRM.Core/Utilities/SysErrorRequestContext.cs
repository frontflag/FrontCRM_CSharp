namespace CRM.Core.Utilities;

/// <summary>
/// 当前请求审计上下文（跨层传递，避免 Infrastructure 依赖 ASP.NET）。
/// </summary>
public static class SysErrorRequestContext
{
    private sealed class Holder
    {
        public string? ErrorId;
        public string? UserId;
        public string? UserName;
        public string? RequestPath;
    }

    private static readonly AsyncLocal<Holder?> Current = new();

    private static Holder Ensure() => Current.Value ??= new Holder();

    public static string? ErrorId
    {
        get => Current.Value?.ErrorId;
        set => Ensure().ErrorId = value;
    }

    public static string? UserId
    {
        get => Current.Value?.UserId;
        set => Ensure().UserId = value;
    }

    public static string? UserName
    {
        get => Current.Value?.UserName;
        set => Ensure().UserName = value;
    }

    public static string? RequestPath
    {
        get => Current.Value?.RequestPath;
        set => Ensure().RequestPath = value;
    }

    public static void Clear() => Current.Value = null;
}

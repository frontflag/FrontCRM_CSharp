namespace CRM.Core.Constants;

public static class DashboardNoticeKinds
{
    public const string Announcement = "announcement";
    public const string Bbs = "bbs";
}

public static class DashboardNoticeRules
{
    /// <summary>此时间（UTC）之前发布的论坛「系统更新」不进入桌面系统通告。</summary>
    public static readonly DateTime BbsSystemUpdateNotBeforeUtc =
        new(2026, 10, 3, 11, 30, 0, DateTimeKind.Utc);
}

namespace CRM.Core.Constants;

public static class BbsPermissionCodes
{
    /// <summary>版主：置顶、删他人帖等。SYS_ADMIN / SYS_MANAGER 亦视为版主。</summary>
    public const string Moderate = "bbs.moderate";
}

/// <summary>
/// 论坛主题类型。1–6 为当前可用类型（空库起步，不再沿用 EBS 求助/吆喝枚举）。
/// </summary>
public static class BbsSubjectTypes
{
    public const int CompanyNotice = 1;
    public const int IndustryNews = 2;
    public const int Share = 3;
    public const int OpsGuide = 4;
    public const int Suggestion = 5;
    public const int SystemUpdate = 6;

    public static readonly int[] All =
    [
        CompanyNotice,
        IndustryNews,
        Share,
        OpsGuide,
        Suggestion,
        SystemUpdate
    ];

    public static bool IsAllowed(int type) => type is >= CompanyNotice and <= SystemUpdate;

    public static string ToLabel(int type) => type switch
    {
        CompanyNotice => "公司通告",
        IndustryNews => "行业快讯",
        Share => "交流分享",
        OpsGuide => "操作说明",
        Suggestion => "优化建议",
        SystemUpdate => "系统更新",
        _ => type.ToString()
    };
}

public static class BbsSubjectStatuses
{
    public const int Open = 1;
    public const int Close = 2;

    public static bool IsValid(int status) => status is Open or Close;

    public static string ToLabel(int status) => status switch
    {
        Open => "打开",
        Close => "关闭",
        _ => status.ToString()
    };
}

public static class BbsLimits
{
    public const int TitleMaxLength = 200;
    public const int ContentMaxBytes = 50 * 1024;
    public const int HotReplyThreshold = 50;
}

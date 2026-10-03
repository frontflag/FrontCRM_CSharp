namespace CRM.Core.Constants;

/// <summary>随版本发布、各环境启动时幂等写入的系统帖。</summary>
public static class BbsSystemPosts
{
    /// <summary>操作说明板块：《论坛如何使用》。</summary>
    public const string OpsGuideId = "bbs-sys-ops-guide";

    public const string AuthorName = "系统";
}

public static class BbsPermissionCodes
{
    /// <summary>
    /// 全局版主（过渡）：置顶/关帖/删帖全板块。
    /// 板块版主以 bbs_board_moderator 为准；SYS_ADMIN / SYS_MANAGER 亦为全局版主并可设置板块版主。
    /// </summary>
    public const string Moderate = "bbs.moderate";
}

/// <summary>
/// 论坛主题类型。1–6 为内置类型；≥100 为超管新建的自定义板块。
/// </summary>
public static class BbsSubjectTypes
{
    public const int CompanyNotice = 1;
    public const int IndustryNews = 2;
    public const int Share = 3;
    public const int OpsGuide = 4;
    public const int Suggestion = 5;
    public const int SystemUpdate = 6;

    /// <summary>自定义板块类型号下限（含）。</summary>
    public const int CustomMin = 100;

    public static readonly int[] All =
    [
        CompanyNotice,
        IndustryNews,
        Share,
        OpsGuide,
        Suggestion,
        SystemUpdate
    ];

    /// <summary>侧栏顺序固定、不可拖放调整的板块（系统更新 / 操作说明）。</summary>
    public static readonly int[] FixedBoardTypes = [SystemUpdate, OpsGuide];

    public static bool IsBuiltin(int type) => type is >= CompanyNotice and <= SystemUpdate;

    public static bool IsCustom(int type) => type >= CustomMin;

    public static bool IsAllowed(int type) => IsBuiltin(type) || IsCustom(type);

    /// <summary>系统更新、操作说明：仅 SYS_ADMIN 可发帖；不设板块版主。</summary>
    public static bool IsAdminOnlyPostType(int type) => type is OpsGuide or SystemUpdate;

    public static bool IsFixedBoardType(int type) => type is SystemUpdate or OpsGuide;

    public static bool SupportsBoardModerator(int type) => IsAllowed(type) && !IsAdminOnlyPostType(type);

    /// <summary>可拖放调整顺序的板块。</summary>
    public static bool IsMovableBoardType(int type) => SupportsBoardModerator(type);

    /// <summary>可配置板块的默认显示顺序（系统更新/操作说明固定置顶区下方，不在此列）。</summary>
    public static int DefaultSortOrder(int type) => type switch
    {
        CompanyNotice => 10,
        IndustryNews => 20,
        Share => 30,
        Suggestion => 40,
        _ when IsCustom(type) => 100 + (type - CustomMin) * 10,
        _ => 100
    };

    public static string ToLabel(int type) => type switch
    {
        CompanyNotice => "公司通告",
        IndustryNews => "行业快讯",
        Share => "交流分享",
        OpsGuide => "操作说明",
        Suggestion => "优化建议",
        SystemUpdate => "系统更新",
        _ when IsCustom(type) => $"自定义板块{type}",
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

/// <summary>主题形态：普通帖 / 投票帖。</summary>
public static class BbsSubjectKinds
{
    public const int Normal = 0;
    public const int Poll = 1;

    public static bool IsValid(int kind) => kind is Normal or Poll;
}

/// <summary>投票模式：单选 / 多选。</summary>
public static class BbsVoteModes
{
    public const int Single = 1;
    public const int Multi = 2;

    public static bool IsValid(int mode) => mode is Single or Multi;
}

public static class BbsPollLimits
{
    public const int MinOptions = 2;
    public const int MaxOptions = 20;
    public const int OptionTextMaxLength = 100;
}

/// <summary>赞/踩目标类型。</summary>
public static class BbsReactionTargetTypes
{
    public const int Subject = 1;
    public const int Reply = 2;

    public static bool IsValid(int type) => type is Subject or Reply;
}

/// <summary>赞/踩取值；0 表示取消。</summary>
public static class BbsReactionValues
{
    public const int Like = 1;
    public const int Dislike = -1;
    public const int None = 0;

    public static bool IsVote(int value) => value is Like or Dislike;
    public static bool IsValidRequest(int value) => value is Like or Dislike or None;
}

/// <summary>论坛主题媒体，复用 upload_document，bizType = BBS_SUBJECT。</summary>
public static class BbsDocumentBizTypes
{
    public const string Subject = "BBS_SUBJECT";

    public static bool IsSubject(string? bizType) =>
        !string.IsNullOrWhiteSpace(bizType)
        && bizType.Equals(Subject, StringComparison.OrdinalIgnoreCase);
}

public static class BbsMediaLimits
{
    public const int MaxImagesPerSubject = 50;
    public const int MaxVideosPerSubject = 1;
    public const long MaxImageBytes = 5L * 1024 * 1024;
    public const long MaxVideoBytes = 50L * 1024 * 1024;

    public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm"
    };

    public static bool IsImage(string? ext) =>
        !string.IsNullOrWhiteSpace(ext) && ImageExtensions.Contains(ext.StartsWith('.') ? ext : "." + ext);

    public static bool IsVideo(string? ext) =>
        !string.IsNullOrWhiteSpace(ext) && VideoExtensions.Contains(ext.StartsWith('.') ? ext : "." + ext);
}

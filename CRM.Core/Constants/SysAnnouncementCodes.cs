namespace CRM.Core.Constants;

/// <summary>系统公告附件 bizType（documents/upload）。</summary>
public static class SysAnnouncementDocumentBizType
{
    public const string SysAnnouncement = "SYS_ANNOUNCEMENT";
}

/// <summary>JWT claim：模拟登录操作者 Id；存在即表示当前会话为模拟登录。</summary>
public static class ImpersonationClaimTypes
{
    public const string Impersonator = "impersonator";
}

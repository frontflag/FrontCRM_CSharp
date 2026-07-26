namespace CRM.Core.Constants;

/// <summary>SuperAdmin 敏感操作日志（仅 /debug/super 可见；系统操作日志强制排除）。</summary>
public static class SuperAdminOperationLogCodes
{
    public const string BizType = "super_admin";

    public const string ChangePassword = "更改SuperAdmin密码";
    public const string CreateSuperAdmin = "创建SuperAdmin账号";
}

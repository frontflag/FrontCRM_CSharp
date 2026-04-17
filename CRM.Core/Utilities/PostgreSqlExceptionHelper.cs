namespace CRM.Core.Utilities;

/// <summary>用于在未对齐迁移时做有限降级（仅识别常见 SQLSTATE）。</summary>
public static class PostgreSqlExceptionHelper
{
    /// <summary>PostgreSQL 42703：列/关系不存在（常见于未执行迁移而模型已增列）。</summary>
    public static bool IsUndefinedObject(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        if (msg.Contains("42703", StringComparison.Ordinal))
            return true;
        return ex.InnerException != null && IsUndefinedObject(ex.InnerException);
    }
}

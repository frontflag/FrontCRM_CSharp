using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.API.Utilities;

/// <summary>将 EF / Npgsql 异常展开为可读的 API 提示（便于排查列缺失、约束失败等）。</summary>
internal static class ApiExceptionMessages
{
    public static string FormatWithDatabaseInner(Exception ex)
    {
        if (ex is DbUpdateException)
        {
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
            {
                if (inner is PostgresException pg)
                {
                    return string.IsNullOrWhiteSpace(pg.Detail)
                        ? pg.Message
                        : $"{pg.Message} {pg.Detail}";
                }
            }
        }

        return ex.Message;
    }
}

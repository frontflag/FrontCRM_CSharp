using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Services;

public static class DashboardNoticeSchemaEnsure
{
    public static async Task EnsureAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE public.sys_announcement
                  ADD COLUMN IF NOT EXISTS delivery character varying(16) NOT NULL DEFAULT 'popup';

                CREATE TABLE IF NOT EXISTS public.bbs_dashboard_notice_dismiss (
                  id character varying(36) NOT NULL,
                  subject_id character varying(36) NOT NULL,
                  user_id character varying(36) NOT NULL,
                  dismissed_at timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  CONSTRAINT "PK_bbs_dashboard_notice_dismiss" PRIMARY KEY (id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_dashboard_notice_dismiss_subject_user
                  ON public.bbs_dashboard_notice_dismiss (subject_id, user_id);
                """,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "桌面系统通告表结构对齐失败");
        }
    }
}

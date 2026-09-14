using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.WorkCalendar;

/// <summary>
/// 幂等补齐工作日程依赖的可空列与任务表。不做历史回填、不写权限。
/// </summary>
public static class WorkCalendarSchemaEnsure
{
    public static async Task EnsureAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE public.rfq ADD COLUMN IF NOT EXISTS assigned_at timestamp with time zone NULL;""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE public.sellorder ADD COLUMN IF NOT EXISTS approved_at timestamp with time zone NULL;""",
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_rfq_sales_assigned
                  ON public.rfq (sales_user_id, assigned_at)
                  WHERE assigned_at IS NOT NULL;
                """,
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_sellorder_sales_approved
                  ON public.sellorder (sales_user_id, approved_at)
                  WHERE approved_at IS NOT NULL;
                """,
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS public.work_task (
                  id character varying(36) NOT NULL,
                  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  create_by_user_id character varying(36) NOT NULL,
                  modify_time timestamp with time zone NULL,
                  status smallint NOT NULL DEFAULT 10,
                  object_type character varying(32) NOT NULL,
                  object_id character varying(36) NOT NULL,
                  title character varying(200) NOT NULL,
                  content character varying(2000) NULL,
                  start_date date NOT NULL,
                  priority smallint NOT NULL DEFAULT 2,
                  assignee_user_id character varying(36) NOT NULL,
                  completed_at timestamp with time zone NULL,
                  completed_by_user_id character varying(36) NULL,
                  contact_history_id character varying(36) NULL,
                  is_deleted boolean NOT NULL DEFAULT false,
                  CONSTRAINT "PK_work_task" PRIMARY KEY (id)
                );
                """,
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_work_task_assignee_start
                  ON public.work_task (assignee_user_id, start_date)
                  WHERE is_deleted = false;
                """,
                cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_work_task_object
                  ON public.work_task (object_type, object_id)
                  WHERE is_deleted = false;
                """,
                cancellationToken);
            logger.LogInformation("工作日程兼容结构已对齐：rfq.assigned_at、sellorder.approved_at、work_task");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "工作日程兼容 DDL 未执行成功。审批/订单列表不受影响；月历蓝绿点可能暂时为空。请执行 scripts/ensure_work_task_postgresql.sql");
        }
    }
}

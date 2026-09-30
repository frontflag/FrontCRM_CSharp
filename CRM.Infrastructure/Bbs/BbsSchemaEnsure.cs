using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Bbs;

/// <summary>幂等创建 bbs_subject / bbs_reply。</summary>
public static class BbsSchemaEnsure
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
                CREATE TABLE IF NOT EXISTS public.bbs_subject (
                  id character varying(36) NOT NULL,
                  title character varying(200) NOT NULL,
                  content text NOT NULL,
                  type integer NOT NULL,
                  status integer NOT NULL DEFAULT 1,
                  is_top boolean NOT NULL DEFAULT false,
                  is_hot boolean NOT NULL DEFAULT false,
                  anonymous boolean NOT NULL DEFAULT false,
                  view_count integer NOT NULL DEFAULT 0,
                  reply_count integer NOT NULL DEFAULT 0,
                  last_reply_time timestamp with time zone NULL,
                  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  create_by character varying(36) NULL,
                  modify_time timestamp with time zone NULL,
                  modify_by character varying(36) NULL,
                  is_deleted boolean NOT NULL DEFAULT false,
                  CONSTRAINT "PK_bbs_subject" PRIMARY KEY (id)
                );
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_bbs_subject_list
                  ON public.bbs_subject (is_deleted, is_top, last_reply_time DESC NULLS LAST, create_time DESC);
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_bbs_subject_type_status
                  ON public.bbs_subject (type, status)
                  WHERE is_deleted = false;
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS public.bbs_reply (
                  id character varying(36) NOT NULL,
                  subject_id character varying(36) NOT NULL,
                  content text NOT NULL,
                  anonymous boolean NOT NULL DEFAULT false,
                  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  create_by character varying(36) NULL,
                  modify_time timestamp with time zone NULL,
                  modify_by character varying(36) NULL,
                  is_deleted boolean NOT NULL DEFAULT false,
                  CONSTRAINT "PK_bbs_reply" PRIMARY KEY (id)
                );
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_bbs_reply_subject
                  ON public.bbs_reply (subject_id, create_time)
                  WHERE is_deleted = false;
                """,
                cancellationToken);

            logger.LogInformation("BBS 表结构已对齐：bbs_subject、bbs_reply");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BBS SchemaEnsure 失败（可手动执行 scripts/ensure_bbs_postgresql.sql）");
        }
    }
}

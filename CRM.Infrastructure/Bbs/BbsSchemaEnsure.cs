using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Infrastructure.Bbs;

/// <summary>幂等创建 bbs_subject / bbs_reply / bbs_reaction / bbs_board_moderator，并补齐赞踩列。</summary>
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
                  like_count integer NOT NULL DEFAULT 0,
                  dislike_count integer NOT NULL DEFAULT 0,
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
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS like_count integer NOT NULL DEFAULT 0;
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS dislike_count integer NOT NULL DEFAULT 0;
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
                  like_count integer NOT NULL DEFAULT 0,
                  dislike_count integer NOT NULL DEFAULT 0,
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
                ALTER TABLE public.bbs_reply
                  ADD COLUMN IF NOT EXISTS like_count integer NOT NULL DEFAULT 0;
                ALTER TABLE public.bbs_reply
                  ADD COLUMN IF NOT EXISTS dislike_count integer NOT NULL DEFAULT 0;
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS ix_bbs_reply_subject
                  ON public.bbs_reply (subject_id, create_time)
                  WHERE is_deleted = false;
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS public.bbs_reaction (
                  id character varying(36) NOT NULL,
                  target_type integer NOT NULL,
                  target_id character varying(36) NOT NULL,
                  user_id character varying(36) NOT NULL,
                  value integer NOT NULL,
                  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  modify_time timestamp with time zone NULL,
                  CONSTRAINT "PK_bbs_reaction" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_reaction_target_user
                  ON public.bbs_reaction (target_type, target_id, user_id);
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS public.bbs_board_moderator (
                  subject_type integer NOT NULL,
                  user_id character varying(36) NULL,
                  display_name character varying(50) NULL,
                  update_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  update_by character varying(36) NULL,
                  CONSTRAINT "PK_bbs_board_moderator" PRIMARY KEY (subject_type)
                );
                ALTER TABLE public.bbs_board_moderator
                  ALTER COLUMN user_id DROP NOT NULL;
                ALTER TABLE public.bbs_board_moderator
                  ADD COLUMN IF NOT EXISTS display_name character varying(50) NULL;
                ALTER TABLE public.bbs_board_moderator
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE public.bbs_board_moderator
                  ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;
                CREATE INDEX IF NOT EXISTS ix_bbs_board_moderator_user
                  ON public.bbs_board_moderator (user_id);
                -- 仅当尚未使用 10+ 间距时，将旧默认 1/2/3/4 拉开，避免与手动设为 1 的板块并列
                DO $bbs_sort$
                BEGIN
                  IF NOT EXISTS (
                    SELECT 1 FROM public.bbs_board_moderator WHERE sort_order >= 10
                  ) THEN
                    UPDATE public.bbs_board_moderator SET sort_order = 10 WHERE subject_type = 1 AND sort_order = 1;
                    UPDATE public.bbs_board_moderator SET sort_order = 20 WHERE subject_type = 2 AND sort_order = 2;
                    UPDATE public.bbs_board_moderator SET sort_order = 30 WHERE subject_type = 3 AND sort_order = 3;
                    UPDATE public.bbs_board_moderator SET sort_order = 40 WHERE subject_type = 5 AND sort_order = 4;
                  END IF;
                END
                $bbs_sort$;
                """,
                cancellationToken);

            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS kind integer NOT NULL DEFAULT 0;
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS vote_mode integer NOT NULL DEFAULT 0;
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS vote_max_choices integer NULL;
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS vote_deadline timestamp with time zone NULL;
                ALTER TABLE public.bbs_subject
                  ADD COLUMN IF NOT EXISTS vote_count integer NOT NULL DEFAULT 0;

                CREATE TABLE IF NOT EXISTS public.bbs_poll_option (
                  id character varying(36) NOT NULL,
                  subject_id character varying(36) NOT NULL,
                  sort_order integer NOT NULL DEFAULT 0,
                  text character varying(100) NOT NULL,
                  is_deleted boolean NOT NULL DEFAULT false,
                  CONSTRAINT "PK_bbs_poll_option" PRIMARY KEY (id)
                );
                CREATE INDEX IF NOT EXISTS ix_bbs_poll_option_subject
                  ON public.bbs_poll_option (subject_id, sort_order);

                CREATE TABLE IF NOT EXISTS public.bbs_poll_vote (
                  id character varying(36) NOT NULL,
                  subject_id character varying(36) NOT NULL,
                  user_id character varying(36) NOT NULL,
                  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
                  CONSTRAINT "PK_bbs_poll_vote" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_poll_vote_subject_user
                  ON public.bbs_poll_vote (subject_id, user_id);

                CREATE TABLE IF NOT EXISTS public.bbs_poll_vote_item (
                  id character varying(36) NOT NULL,
                  vote_id character varying(36) NOT NULL,
                  option_id character varying(36) NOT NULL,
                  CONSTRAINT "PK_bbs_poll_vote_item" PRIMARY KEY (id)
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_poll_vote_item
                  ON public.bbs_poll_vote_item (vote_id, option_id);
                CREATE INDEX IF NOT EXISTS ix_bbs_poll_vote_item_option
                  ON public.bbs_poll_vote_item (option_id);
                """,
                cancellationToken);

            logger.LogInformation("BBS 表结构已对齐：含投票帖 poll 表");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BBS SchemaEnsure 失败（可手动执行 scripts/ensure_bbs_postgresql.sql）");
        }
    }
}

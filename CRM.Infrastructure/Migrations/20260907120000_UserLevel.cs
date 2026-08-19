using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>员工当前等级在 user 表；变更履历独立表 user_level_history（含当时账号快照）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260907120000_UserLevel")]
public partial class UserLevel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public."user"
              ADD COLUMN IF NOT EXISTS "Level" smallint NOT NULL DEFAULT 1;
            ALTER TABLE IF EXISTS public."user"
              ADD COLUMN IF NOT EXISTS "LevelChangedAt" timestamp with time zone NULL;
            ALTER TABLE IF EXISTS public."user"
              ADD COLUMN IF NOT EXISTS "LevelRemark" character varying(200) NULL;

            UPDATE public."user" SET "Level" = 1 WHERE "Level" IS NULL OR "Level" < 1 OR "Level" > 20;

            COMMENT ON COLUMN public."user"."Level" IS '当前用户等级 1～20，默认 1';
            COMMENT ON COLUMN public."user"."LevelChangedAt" IS '当前等级开始时间；从未改过级为空';
            COMMENT ON COLUMN public."user"."LevelRemark" IS '当前等级备注';

            CREATE TABLE IF NOT EXISTS public.user_level_history (
              "UserLevelHistoryId" character varying(36) NOT NULL,
              "UserId" character varying(36) NOT NULL,
              "UserName" character varying(50) NOT NULL,
              "OldLevel" smallint NOT NULL,
              "NewLevel" smallint NOT NULL,
              "Remark" character varying(200) NULL,
              "ChangeTime" timestamp with time zone NOT NULL,
              "OperatorUserId" character varying(36) NULL,
              "OperatorUserName" character varying(50) NULL,
              "CreateTime" timestamp with time zone NOT NULL DEFAULT NOW(),
              CONSTRAINT "PK_user_level_history" PRIMARY KEY ("UserLevelHistoryId")
            );

            CREATE INDEX IF NOT EXISTS "IX_user_level_history_UserId_ChangeTime"
              ON public.user_level_history ("UserId", "ChangeTime");

            COMMENT ON TABLE public.user_level_history IS '用户等级变更履历（只追加）；UserName 为变更当时登录账号快照';
            COMMENT ON COLUMN public.user_level_history."UserName" IS '变更当时被改等级员工的登录账号快照';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.user_level_history;
            ALTER TABLE IF EXISTS public."user" DROP COLUMN IF EXISTS "LevelRemark";
            ALTER TABLE IF EXISTS public."user" DROP COLUMN IF EXISTS "LevelChangedAt";
            ALTER TABLE IF EXISTS public."user" DROP COLUMN IF EXISTS "Level";
            """);
    }
}

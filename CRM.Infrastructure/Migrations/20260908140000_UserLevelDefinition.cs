using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>用户等级主数据：1～20 固定行，可维护说明。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908140000_UserLevelDefinition")]
public partial class UserLevelDefinition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.user_level_def (
              "UserLevelDefId" character varying(36) NOT NULL,
              "UserLevel" smallint NOT NULL,
              "Description" character varying(200) NULL,
              "CreateTime" timestamp with time zone NOT NULL DEFAULT NOW(),
              "ModifyTime" timestamp with time zone NULL,
              CONSTRAINT "PK_user_level_def" PRIMARY KEY ("UserLevelDefId")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_level_def_UserLevel"
              ON public.user_level_def ("UserLevel");

            COMMENT ON TABLE public.user_level_def IS '用户等级主数据（1～20 固定行，仅维护说明）';
            COMMENT ON COLUMN public.user_level_def."UserLevel" IS '等级 1～20';
            COMMENT ON COLUMN public.user_level_def."Description" IS '等级说明，可空，最长 200';

            INSERT INTO public.user_level_def ("UserLevelDefId", "UserLevel", "Description", "CreateTime")
            SELECT
              'ul000000-0000-4000-8000-00000000' || lpad(n::text, 4, '0'),
              n::smallint,
              NULL,
              NOW()
            FROM generate_series(1, 20) AS n
            ON CONFLICT ("UserLevel") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS public.user_level_def;""");
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260806130000_AiEntityParseLogSavedFields")]
public partial class AiEntityParseLogSavedFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.ai_entity_parse_log
              ADD COLUMN IF NOT EXISTS saved_biz_id character varying(64) NULL,
              ADD COLUMN IF NOT EXISTS saved_at timestamp with time zone NULL;

            CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_saved_created"
              ON public.ai_entity_parse_log (saved_at DESC NULLS LAST)
              WHERE saved_at IS NOT NULL;

            INSERT INTO public.ai_global_config (config_key, config_value, description, modify_time)
            VALUES (
              'entity_parse_log_retention_days',
              '180',
              'AI entity.parse 质量日志保留天数（purge 脚本/API 参考）',
              NOW()
            )
            ON CONFLICT (config_key) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_ai_entity_parse_log_saved_created";
            ALTER TABLE IF EXISTS public.ai_entity_parse_log
              DROP COLUMN IF EXISTS saved_biz_id,
              DROP COLUMN IF EXISTS saved_at;
            DELETE FROM public.ai_global_config WHERE config_key = 'entity_parse_log_retention_days';
            """);
    }
}

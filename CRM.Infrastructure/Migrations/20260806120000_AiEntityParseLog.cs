using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260806120000_AiEntityParseLog")]
public partial class AiEntityParseLog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.ai_entity_parse_log (
                id character varying(36) NOT NULL,
                invocation_id character varying(36) NOT NULL,
                scenario_code character varying(100) NOT NULL,
                entity_type character varying(64) NOT NULL,
                user_id character varying(36) NULL,
                parent_biz_type character varying(64) NULL,
                parent_biz_id character varying(64) NULL,
                raw_text text NOT NULL DEFAULT '',
                parse_result_raw text NULL,
                parse_result_json jsonb NOT NULL DEFAULT '{}'::jsonb,
                confirmed_fields_json jsonb NULL,
                outcome character varying(20) NOT NULL DEFAULT 'parsed',
                template_version integer NOT NULL DEFAULT 1,
                provider_code character varying(64) NOT NULL DEFAULT '',
                model character varying(100) NOT NULL DEFAULT '',
                from_cache boolean NOT NULL DEFAULT false,
                latency_ms integer NOT NULL DEFAULT 0,
                created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                confirmed_at timestamp with time zone NULL,
                CONSTRAINT "PK_ai_entity_parse_log" PRIMARY KEY (id)
            );

            CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_invocation"
                ON public.ai_entity_parse_log (invocation_id);

            CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_scenario_created"
                ON public.ai_entity_parse_log (scenario_code, created_at DESC);

            CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_user_created"
                ON public.ai_entity_parse_log (user_id, created_at DESC);

            CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_outcome_created"
                ON public.ai_entity_parse_log (outcome, created_at DESC);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.ai_entity_parse_log;");
    }
}

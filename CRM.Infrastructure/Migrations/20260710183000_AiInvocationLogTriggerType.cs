using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260710183000_AiInvocationLogTriggerType")]
public partial class AiInvocationLogTriggerType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.ai_invocation_log
                ADD COLUMN IF NOT EXISTS trigger_type character varying(20) NULL;

            CREATE INDEX IF NOT EXISTS "IX_ai_invocation_log_trigger_created"
                ON public.ai_invocation_log (trigger_type, created_at DESC);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public."IX_ai_invocation_log_trigger_created";
            ALTER TABLE public.ai_invocation_log DROP COLUMN IF EXISTS trigger_type;
            """);
    }
}

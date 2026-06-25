using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804120000_AiScenarioEnableWebSearch")]
public partial class AiScenarioEnableWebSearch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.ai_scenario
                ADD COLUMN IF NOT EXISTS enable_web_search boolean NOT NULL DEFAULT false;

            UPDATE public.ai_scenario
            SET enable_web_search = true
            WHERE code = 'material.intel.lookup';

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.ai_scenario
                DROP COLUMN IF EXISTS enable_web_search;
            """);
    }
}

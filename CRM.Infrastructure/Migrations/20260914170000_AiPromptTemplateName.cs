using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260914170000_AiPromptTemplateName")]
public partial class AddAiPromptTemplateName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE public.ai_prompt_template
  ADD COLUMN IF NOT EXISTS name varchar(200) NOT NULL DEFAULT '';

UPDATE public.ai_prompt_template t
SET name = s.name
FROM public.ai_scenario s
WHERE btrim(COALESCE(t.name, '')) = ''
  AND btrim(COALESCE(s.name, '')) <> ''
  AND (
    s.prompt_template_id = t.id
    OR s.code = t.code
  );
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"ALTER TABLE public.ai_prompt_template DROP COLUMN IF EXISTS name;");
    }
}

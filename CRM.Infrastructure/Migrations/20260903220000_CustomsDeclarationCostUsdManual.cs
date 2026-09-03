using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关单头/行：采购美金价系统/手工来源标记。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260903220000_CustomsDeclarationCostUsdManual")]
    public partial class CustomsDeclarationCostUsdManual : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration
                    ADD COLUMN IF NOT EXISTS cost_usd_manual boolean NOT NULL DEFAULT false;

                ALTER TABLE public.customs_declaration_item
                    ADD COLUMN IF NOT EXISTS cost_usd_manual boolean NOT NULL DEFAULT false;

                COMMENT ON COLUMN public.customs_declaration.cost_usd_manual IS '采购美金价模式：false=系统公式，true=允许行内手工覆盖；换报关公司不重置';
                COMMENT ON COLUMN public.customs_declaration_item.cost_usd_manual IS '本行采购美金价是否手工覆盖（仅头 cost_usd_manual=true 时生效）';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration_item
                    DROP COLUMN IF EXISTS cost_usd_manual;

                ALTER TABLE public.customs_declaration
                    DROP COLUMN IF EXISTS cost_usd_manual;
                """);
        }
    }
}

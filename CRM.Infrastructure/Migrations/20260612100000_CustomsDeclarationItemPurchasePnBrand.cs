using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关明细表增加 <c>purchase_pn</c>、<c>purchase_brand</c>（与在库明细 PN/品牌快照口径一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260612100000_CustomsDeclarationItemPurchasePnBrand")]
    public partial class CustomsDeclarationItemPurchasePnBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""purchase_pn"" character varying(200) NULL;
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""purchase_brand"" character varying(200) NULL;
COMMENT ON COLUMN public.customs_declaration_item.""purchase_pn"" IS '物料型号/PN 快照';
COMMENT ON COLUMN public.customs_declaration_item.""purchase_brand"" IS '品牌快照';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""purchase_brand"";
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""purchase_pn"";
");
        }
    }
}

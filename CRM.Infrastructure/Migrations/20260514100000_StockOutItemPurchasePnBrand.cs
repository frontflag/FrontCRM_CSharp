using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockoutitem：冗余采购 PN/品牌（与 stockitem / stock 一致，便于列表与报表）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260514100000_StockOutItemPurchasePnBrand")]
    public partial class StockOutItemPurchasePnBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutitem
  ADD COLUMN IF NOT EXISTS ""purchase_pn"" character varying(200) NULL,
  ADD COLUMN IF NOT EXISTS ""purchase_brand"" character varying(200) NULL;

COMMENT ON COLUMN public.stockoutitem.""purchase_pn"" IS '物料型号/采购 PN（冗余，与 stockitem.purchase_pn 一致）';
COMMENT ON COLUMN public.stockoutitem.""purchase_brand"" IS '品牌（冗余，与 stockitem.purchase_brand 一致）';

UPDATE public.stockoutitem i
SET ""purchase_pn"" = s.""purchase_pn"", ""purchase_brand"" = s.""purchase_brand""
FROM public.stockitem s
WHERE NULLIF(TRIM(COALESCE(i.""StockItemId"", '')), '') IS NOT NULL
  AND s.""StockItemId"" = i.""StockItemId"";

UPDATE public.stockoutitem i
SET
  ""purchase_pn"" = COALESCE(i.""purchase_pn"", sk.purchase_pn),
  ""purchase_brand"" = COALESCE(i.""purchase_brand"", sk.purchase_brand)
FROM public.stock sk
WHERE NULLIF(TRIM(COALESCE(i.""StockId"", '')), '') IS NOT NULL
  AND sk.""StockId"" = i.""StockId"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutitem DROP COLUMN IF EXISTS ""purchase_brand"";
ALTER TABLE IF EXISTS public.stockoutitem DROP COLUMN IF EXISTS ""purchase_pn"";
");
        }
    }
}

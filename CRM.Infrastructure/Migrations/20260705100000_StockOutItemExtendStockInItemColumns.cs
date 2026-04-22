using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 出库明细扩展 <c>stock_out_item_extend</c>：冗余入库明细主键与业务编号（经 <c>stock_item</c> 关联 <c>stock_in_item</c>）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260705100000_StockOutItemExtendStockInItemColumns")]
    public partial class StockOutItemExtendStockInItemColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out_item_extend ADD COLUMN IF NOT EXISTS ""StockInItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_out_item_extend ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

UPDATE public.stock_out_item_extend ext
SET
  ""StockInItemId"" = NULLIF(TRIM(si.""StockInItemId""), ''),
  stock_in_item_code = NULLIF(TRIM(BOTH FROM COALESCE(si.stock_in_item_code, ii.stock_in_item_code)), '')
FROM public.stock_item si
LEFT JOIN public.stock_in_item ii ON ii.""ItemId"" = si.""StockInItemId""
WHERE NULLIF(TRIM(ext.""StockItemId""), '') IS NOT NULL
  AND si.""StockItemId"" = ext.""StockItemId"";

COMMENT ON COLUMN public.stock_out_item_extend.""StockInItemId"" IS '对应入库明细主键（冗余自 stock_in_item.ItemId，经 stock_item.StockInItemId）';
COMMENT ON COLUMN public.stock_out_item_extend.stock_in_item_code IS '入库明细业务编号（冗余自 stock_in_item.stock_in_item_code；可与 stock_item.stock_in_item_code 对齐）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out_item_extend DROP COLUMN IF EXISTS stock_in_item_code;
ALTER TABLE IF EXISTS public.stock_out_item_extend DROP COLUMN IF EXISTS ""StockInItemId"";
");
        }
    }
}

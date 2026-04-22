using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 在库明细 <c>stock_item.stock_in_item_code</c>：冗余入库明细业务编号，回填自 <c>stock_in_item.stock_in_item_code</c>。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704120000_StockItemStockInItemCodeColumn")]
    public partial class StockItemStockInItemCodeColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_item ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

UPDATE public.stock_item it
SET stock_in_item_code = NULLIF(TRIM(ii.stock_in_item_code), '')
FROM public.stock_in_item ii
WHERE ii.""ItemId"" = it.""StockInItemId"";

COMMENT ON COLUMN public.stock_item.stock_in_item_code IS '对应入库明细业务编号（冗余自 stock_in_item.stock_in_item_code）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_item DROP COLUMN IF EXISTS stock_in_item_code;
");
        }
    }
}

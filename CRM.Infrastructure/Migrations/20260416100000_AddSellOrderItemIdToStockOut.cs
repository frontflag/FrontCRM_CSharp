using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 出库单头冗余销售明细主键，与销售明细扩展「实出数量」按 SellOrderItemId 汇总一致。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260416100000_AddSellOrderItemIdToStockOut")]
    public partial class AddSellOrderItemIdToStockOut : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;

UPDATE public.stockout so
SET ""SellOrderItemId"" = r.""SalesOrderItemId""
FROM public.stockoutrequest r
WHERE so.""SourceId"" IS NOT NULL
  AND BTRIM(so.""SourceId"") <> ''
  AND r.""UserId"" = so.""SourceId""
  AND (so.""SellOrderItemId"" IS NULL OR BTRIM(so.""SellOrderItemId"") = '');

CREATE INDEX IF NOT EXISTS ""IX_stockout_SellOrderItemId"" ON public.stockout (""SellOrderItemId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_stockout_SellOrderItemId"";
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""SellOrderItemId"";
");
        }
    }
}

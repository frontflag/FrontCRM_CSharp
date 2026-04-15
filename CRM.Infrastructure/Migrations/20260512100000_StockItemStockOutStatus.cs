using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockitem：持久化出库状态（与 QtyInbound/QtyStockOut 比较规则一致），供列表筛选与对账。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512100000_StockItemStockOutStatus")]
    public partial class StockItemStockOutStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem
  ADD COLUMN IF NOT EXISTS ""StockOutStatus"" smallint NOT NULL DEFAULT 0;

UPDATE public.stockitem
SET ""StockOutStatus"" = CASE
  WHEN ""QtyInbound"" <= 0 THEN 0::smallint
  WHEN ""QtyStockOut"" <= 0 THEN 1::smallint
  WHEN ""QtyStockOut"" < ""QtyInbound"" THEN 2::smallint
  ELSE 3::smallint
END;

COMMENT ON COLUMN public.stockitem.""StockOutStatus"" IS '出库状态：0=无有效入库 1=未出库 2=部分出库 3=完成（由入库量与已出库量推导）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem DROP COLUMN IF EXISTS ""StockOutStatus"";
");
        }
    }
}

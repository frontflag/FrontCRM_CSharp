using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockitem：层出库业务 USD 利润（入库快照采/销折 USD × 累计出库数量）；与订单行扩展表出库利润口径隔离。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260513100000_StockItemProfitOutBizUsd")]
    public partial class StockItemProfitOutBizUsd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem
  ADD COLUMN IF NOT EXISTS ""ProfitOutBizUsd"" numeric(18,2) NOT NULL DEFAULT 0;

UPDATE public.stockitem
SET ""ProfitOutBizUsd"" = CASE
  WHEN NULLIF(TRIM(COALESCE(""sell_order_item_id"", '')), '') IS NULL THEN 0::numeric
  WHEN ""QtyStockOut"" <= 0 THEN 0::numeric
  WHEN ""SalesPriceUsd"" IS NULL THEN 0::numeric
  ELSE ROUND((""SalesPriceUsd"" - ""PurchasePriceUsd"") * ""QtyStockOut""::numeric, 2)
END;

COMMENT ON COLUMN public.stockitem.""ProfitOutBizUsd"" IS '出库业务USD利润：(入库快照 SalesPriceUsd - PurchasePriceUsd) × QtyStockOut；无销售行或 SalesPriceUsd 空则为0';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem DROP COLUMN IF EXISTS ""ProfitOutBizUsd"";
");
        }
    }
}

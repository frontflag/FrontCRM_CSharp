using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockitem.ProfitOutBizUsd：由「× QtyStockOut」改为「× QtyInbound」入库快照；出库利润以 stockoutitemextend 为准。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260517120000_StockItemProfitOutBizUsdInboundQty")]
    public partial class StockItemProfitOutBizUsdInboundQty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.stockitem
SET ""ProfitOutBizUsd"" = CASE
  WHEN NULLIF(TRIM(COALESCE(""sell_order_item_id"", '')), '') IS NULL THEN 0::numeric
  WHEN ""QtyInbound"" <= 0 THEN 0::numeric
  WHEN ""SalesPriceUsd"" IS NULL THEN 0::numeric
  ELSE ROUND((""SalesPriceUsd"" - ""PurchasePriceUsd"") * ""QtyInbound""::numeric, 2)
END;

COMMENT ON COLUMN public.stockitem.""ProfitOutBizUsd"" IS '入库快照 USD 价差毛利：(SalesPriceUsd-PurchasePriceUsd)×QtyInbound；出库业务利润见 stockoutitemextend.ProfitOutBizUsd';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售明细扩展：备货库存可用量之和（同 PN+品牌、StockType=备货），用于「申请出库」放宽条件。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260510120000_SellOrderItemExtendPurchasedStockAvailableQty")]
    public partial class SellOrderItemExtendPurchasedStockAvailableQty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend
  ADD COLUMN IF NOT EXISTS ""PurchasedStock_AvailableQty"" integer NOT NULL DEFAULT 0;
COMMENT ON COLUMN public.sellorderitemextend.""PurchasedStock_AvailableQty"" IS '同PN+品牌下备货库存(StockType=2)在库可用量之和；由入库/出库过账后重算';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PurchasedStock_AvailableQty"";");
        }
    }
}

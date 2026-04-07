using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stock：冗余采购明细 PN/品牌（入库过账时由 stockinitem.MaterialId → purchaseorderitem）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260425100000_StockPurchasePnBrand")]
    public partial class StockPurchasePnBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS purchase_pn character varying(200) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS purchase_brand character varying(200) NULL;
COMMENT ON COLUMN public.stock.purchase_pn IS '采购订单明细 PN（由入库明细 MaterialId 解析 purchaseorderitem）';
COMMENT ON COLUMN public.stock.purchase_brand IS '采购订单明细品牌（由入库明细 MaterialId 解析 purchaseorderitem）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_brand;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_pn;
");
        }
    }
}

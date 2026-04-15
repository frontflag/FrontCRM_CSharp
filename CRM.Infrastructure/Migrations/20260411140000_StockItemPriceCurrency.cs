using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockitem：采购/销售单价币别与折算美元单价（与 PO/SO 明细 currency、财务汇率一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411140000_StockItemPriceCurrency")]
    public partial class StockItemPriceCurrency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem
  ADD COLUMN IF NOT EXISTS ""PurchaseCurrency"" smallint NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS ""PurchasePriceUsd"" numeric(18,6) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""SalesCurrency"" smallint NULL,
  ADD COLUMN IF NOT EXISTS ""SalesPriceUsd"" numeric(18,6) NULL;
COMMENT ON COLUMN public.stockitem.""PurchaseCurrency"" IS '采购单价币别 1=RMB 2=USD 3=EUR 4=HKD …（与 CurrencyCode 一致）';
COMMENT ON COLUMN public.stockitem.""PurchasePriceUsd"" IS '采购单价折合 USD（过账时按财务基准汇率计算）';
COMMENT ON COLUMN public.stockitem.""SalesCurrency"" IS '销售单价币别；无销售行时为 NULL';
COMMENT ON COLUMN public.stockitem.""SalesPriceUsd"" IS '销售单价折合 USD；无销售行时为 NULL';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockitem
  DROP COLUMN IF EXISTS ""SalesPriceUsd"",
  DROP COLUMN IF EXISTS ""SalesCurrency"",
  DROP COLUMN IF EXISTS ""PurchasePriceUsd"",
  DROP COLUMN IF EXISTS ""PurchaseCurrency"";
");
        }
    }
}

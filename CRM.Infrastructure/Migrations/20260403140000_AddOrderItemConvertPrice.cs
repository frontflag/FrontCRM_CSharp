using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售/采购订单明细增加美金折算单价列，并按财务汇率参数回填历史行。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260403140000_AddOrderItemConvertPrice")]
    public partial class AddOrderItemConvertPrice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sellorderitem
  ADD COLUMN IF NOT EXISTS convert_price numeric(18,6) NOT NULL DEFAULT 0;

ALTER TABLE public.purchaseorderitem
  ADD COLUMN IF NOT EXISTS convert_price numeric(18,6) NOT NULL DEFAULT 0;

UPDATE public.sellorderitem AS i
SET convert_price = CASE i.currency
  WHEN 2 THEN round(i.price, 6)
  WHEN 1 THEN CASE WHEN f.""UsdToCny"" > 0 THEN round(i.price / f.""UsdToCny"", 6) ELSE 0 END
  WHEN 3 THEN CASE WHEN f.""UsdToEur"" > 0 THEN round(i.price / f.""UsdToEur"", 6) ELSE 0 END
  WHEN 4 THEN CASE WHEN f.""UsdToHkd"" > 0 THEN round(i.price / f.""UsdToHkd"", 6) ELSE 0 END
  ELSE 0
END
FROM public.financeexchangeratesetting AS f
WHERE f.""FinanceExchangeRateSettingId"" = '00000000-0000-4000-8000-0000000000E1';

UPDATE public.purchaseorderitem AS i
SET convert_price = CASE i.currency
  WHEN 2 THEN round(i.cost, 6)
  WHEN 1 THEN CASE WHEN f.""UsdToCny"" > 0 THEN round(i.cost / f.""UsdToCny"", 6) ELSE 0 END
  WHEN 3 THEN CASE WHEN f.""UsdToEur"" > 0 THEN round(i.cost / f.""UsdToEur"", 6) ELSE 0 END
  WHEN 4 THEN CASE WHEN f.""UsdToHkd"" > 0 THEN round(i.cost / f.""UsdToHkd"", 6) ELSE 0 END
  ELSE 0
END
FROM public.financeexchangeratesetting AS f
WHERE f.""FinanceExchangeRateSettingId"" = '00000000-0000-4000-8000-0000000000E1';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sellorderitem DROP COLUMN IF EXISTS convert_price;
ALTER TABLE public.purchaseorderitem DROP COLUMN IF EXISTS convert_price;
");
        }
    }
}

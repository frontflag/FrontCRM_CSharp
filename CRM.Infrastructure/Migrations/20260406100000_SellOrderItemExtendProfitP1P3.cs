using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260406100000_SellOrderItemExtendProfitP1P3")]
    public partial class SellOrderItemExtendProfitP1P3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteItemId"" character varying(36);
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteCost"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteCurrency"" smallint NOT NULL DEFAULT 1;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteConvertCost"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""FxUsdToCnySnapshot"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""FxUsdToHkdSnapshot"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""FxUsdToEurSnapshot"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""SellConvertUsdUnitSnapshot"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""SellLineAmountUsdSnapshot"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteProfitExpected"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""QuoteProfitRateExpected"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ReQuoteProfitExpected"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ReQuoteProfitRateExpected"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""PoCostUsdTotal"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""PurchaseProfitExpected"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""PurchaseProfitRateExpected"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""PoCostUsdConfirmed"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""SalesProfitExpected"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ProfitOutBizUsd"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ProfitOutRateBiz"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ProfitOutFinUsd"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS ""ProfitOutRateFin"" numeric(18,6) NOT NULL DEFAULT 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ProfitOutRateFin"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ProfitOutFinUsd"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ProfitOutRateBiz"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ProfitOutBizUsd"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""SalesProfitExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PoCostUsdConfirmed"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PurchaseProfitRateExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PurchaseProfitExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PoCostUsdTotal"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ReQuoteProfitRateExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ReQuoteProfitExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteProfitRateExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteProfitExpected"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""SellLineAmountUsdSnapshot"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""SellConvertUsdUnitSnapshot"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""FxUsdToEurSnapshot"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""FxUsdToHkdSnapshot"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""FxUsdToCnySnapshot"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteConvertCost"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteCurrency"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteCost"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QuoteItemId"";
");
        }
    }
}

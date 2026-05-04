using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// <c>customer_pn_no</c> → <c>customer_so</c>（客户订单号）；新增 <c>customer_brand</c>。已与手工 SQL 对齐，幂等。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260717120000_SellOrderItemCustomerPnNoToCustomerSoAndBrand")]
    public partial class SellOrderItemCustomerPnNoToCustomerSoAndBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'sellorderitem' AND column_name = 'customer_pn_no')
                     AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'sellorderitem' AND column_name = 'customer_so') THEN
                    ALTER TABLE public.sellorderitem RENAME COLUMN customer_pn_no TO customer_so;
                  END IF;
                END $$;
                COMMENT ON COLUMN public.sellorderitem.customer_so IS '客户订单号码（客户侧采购单号等）';
                ALTER TABLE IF EXISTS public.sellorderitem
                  ADD COLUMN IF NOT EXISTS customer_brand character varying(200) NULL;
                COMMENT ON COLUMN public.sellorderitem.customer_brand IS '客户品牌（客户侧品牌描述）';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorderitem DROP COLUMN IF EXISTS customer_brand;
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'sellorderitem' AND column_name = 'customer_so')
                     AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'sellorderitem' AND column_name = 'customer_pn_no') THEN
                    ALTER TABLE public.sellorderitem RENAME COLUMN customer_so TO customer_pn_no;
                  END IF;
                END $$;
                """);
        }
    }
}

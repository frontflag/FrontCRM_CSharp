using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 出库明细业务单号 <c>stock_out_item.stock_out_item_code</c>（{出库单号}-{行序号}），并为历史行回填。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260903120000_StockOutItemCode")]
public partial class StockOutItemCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stock_out_item
                ADD COLUMN IF NOT EXISTS stock_out_item_code character varying(64) NULL;

            COMMENT ON COLUMN public.stock_out_item.stock_out_item_code IS '出库明细单号，{出库单号}-{行序号}';

            UPDATE public.stock_out_item soi
            SET stock_out_item_code = src.code
            FROM (
                SELECT
                    i."ItemId",
                    so."StockOutCode" || '-' || ROW_NUMBER() OVER (
                        PARTITION BY i."StockOutId"
                        ORDER BY i."CreateTime" NULLS LAST, i."ItemId"
                    )::text AS code
                FROM public.stock_out_item i
                INNER JOIN public.stock_out so ON so."StockOutId" = i."StockOutId"
                WHERE (i.stock_out_item_code IS NULL OR btrim(i.stock_out_item_code) = '')
                  AND so."StockOutCode" IS NOT NULL
                  AND btrim(so."StockOutCode") <> ''
            ) src
            WHERE soi."ItemId" = src."ItemId";

            CREATE UNIQUE INDEX IF NOT EXISTS "UX_stock_out_item_code"
                ON public.stock_out_item (stock_out_item_code)
                WHERE stock_out_item_code IS NOT NULL AND btrim(stock_out_item_code) <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public."UX_stock_out_item_code";

            ALTER TABLE IF EXISTS public.stock_out_item
                DROP COLUMN IF EXISTS stock_out_item_code;
            """);
    }
}

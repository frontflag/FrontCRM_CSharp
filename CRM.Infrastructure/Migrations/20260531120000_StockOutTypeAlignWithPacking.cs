using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>历史 stock_out.StockOutType 1/2/4 对齐为 10/30/40（与 packing、<see cref="CRM.Core.Constants.StockOutTypeCode"/> 一致）；3 移库保持不变。</summary>
public partial class StockOutTypeAlignWithPacking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.stock_out SET "StockOutType" = 10 WHERE "StockOutType" = 1;
            UPDATE public.stock_out SET "StockOutType" = 30 WHERE "StockOutType" = 2;
            UPDATE public.stock_out SET "StockOutType" = 40 WHERE "StockOutType" = 4;
            COMMENT ON COLUMN public.stock_out."StockOutType" IS '10销售出库 20报关出库 30退货出库 40报废出库；3移库虚拟出库';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.stock_out SET "StockOutType" = 1 WHERE "StockOutType" = 10;
            UPDATE public.stock_out SET "StockOutType" = 2 WHERE "StockOutType" = 30;
            UPDATE public.stock_out SET "StockOutType" = 4 WHERE "StockOutType" = 40;
            """);
    }
}

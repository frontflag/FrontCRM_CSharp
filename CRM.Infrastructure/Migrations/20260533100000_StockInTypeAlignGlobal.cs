using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>历史 stock_in.StockInType 1/2/4 对齐为 10/30/40（与 <see cref="CRM.Core.Constants.StockInTypeCode"/> 一致）；3 移库保持不变。</summary>
public partial class StockInTypeAlignGlobal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.stock_in SET "StockInType" = 10 WHERE "StockInType" = 1;
            UPDATE public.stock_in SET "StockInType" = 30 WHERE "StockInType" = 2;
            UPDATE public.stock_in SET "StockInType" = 40 WHERE "StockInType" = 4;
            COMMENT ON COLUMN public.stock_in."StockInType" IS '10采购入库 20报关入库 30退货入库 40报废入库；3移库虚拟入库';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.stock_in SET "StockInType" = 1 WHERE "StockInType" = 10;
            UPDATE public.stock_in SET "StockInType" = 2 WHERE "StockInType" = 30;
            UPDATE public.stock_in SET "StockInType" = 4 WHERE "StockInType" = 40;
            """);
    }
}

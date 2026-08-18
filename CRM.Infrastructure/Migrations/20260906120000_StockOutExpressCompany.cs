using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 出库单头表快递公司（LogisticsExpressMethod ItemCode）；出货方式为快递时可填。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260906120000_StockOutExpressCompany")]
public partial class StockOutExpressCompany : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stock_out
              ADD COLUMN IF NOT EXISTS "ExpressCompany" character varying(64) NULL;

            COMMENT ON COLUMN public.stock_out."ExpressCompany" IS '快递公司：数据字典 LogisticsExpressMethod 的 ItemCode；出货方式为快递时可填';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stock_out DROP COLUMN IF EXISTS "ExpressCompany";
            """);
    }
}

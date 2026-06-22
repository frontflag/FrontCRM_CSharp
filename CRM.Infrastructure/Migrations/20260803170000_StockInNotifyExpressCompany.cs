using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 到货通知：预计到货方式为快递时可填快递公司（LogisticsExpressMethod ItemCode）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803170000_StockInNotifyExpressCompany")]
public partial class StockInNotifyExpressCompany : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify
              ADD COLUMN IF NOT EXISTS "ExpressCompany" character varying(64) NULL;

            COMMENT ON COLUMN public.stockin_notify."ExpressCompany" IS '快递公司：数据字典 LogisticsExpressMethod 的 ItemCode；预计到货方式为快递时可填';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS "ExpressCompany";
            """);
    }
}

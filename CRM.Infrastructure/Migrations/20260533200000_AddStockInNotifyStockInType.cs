using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// stockin_notify：入库类型 StockInType（10/20/30/40，与 stock_in 共用 <see cref="CRM.Core.Constants.StockInTypeCode"/>）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260533200000_AddStockInNotifyStockInType")]
public partial class AddStockInNotifyStockInType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify
              ADD COLUMN IF NOT EXISTS "StockInType" smallint NOT NULL DEFAULT 10;
            COMMENT ON COLUMN public.stockin_notify."StockInType"
              IS '入库类型：10采购入库 20报关入库 30退货入库 40报废入库';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS "StockInType";
            """);
    }
}

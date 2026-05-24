using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// qcinfo：入库类型 StockInType（10/20/30/40，与 <see cref="CRM.Core.Constants.StockInTypeCode"/> 一致）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260533300000_AddQcInfoStockInType")]
public partial class AddQcInfoStockInType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.qcinfo
              ADD COLUMN IF NOT EXISTS "StockInType" smallint NOT NULL DEFAULT 10;
            COMMENT ON COLUMN public.qcinfo."StockInType"
              IS '入库类型：10采购入库 20报关入库 30退货入库 40报废入库';

            UPDATE public.qcinfo q
            SET "StockInType" = n."StockInType"
            FROM public.stockin_notify n
            WHERE q."StockInNotifyId" = n."UserId"
              AND n."StockInType" IS NOT NULL
              AND q."StockInType" = 10;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS "StockInType";
            """);
    }
}

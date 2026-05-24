using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// stockout_notify：出库类型 StockOutType（10/20/30/40，与 packing、stock_out 共用）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260532100000_AddStockOutNotifyStockOutType")]
public partial class AddStockOutNotifyStockOutType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE tbl text;
            BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                tbl := 'stockout_notify';
              ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutrequest') THEN
                tbl := 'stockoutrequest';
              ELSE
                RETURN;
              END IF;

              EXECUTE format(
                'ALTER TABLE public.%I ADD COLUMN IF NOT EXISTS "StockOutType" smallint NOT NULL DEFAULT 10',
                tbl);
              EXECUTE format(
                'COMMENT ON COLUMN public.%I."StockOutType" IS ''出库类型：10销售出库 20报关出库 30退货出库 40报废出库''',
                tbl);
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE tbl text;
            BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                tbl := 'stockout_notify';
              ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutrequest') THEN
                tbl := 'stockoutrequest';
              ELSE
                RETURN;
              END IF;

              EXECUTE format('ALTER TABLE public.%I DROP COLUMN IF EXISTS "StockOutType"', tbl);
            END $$;
            """);
    }
}

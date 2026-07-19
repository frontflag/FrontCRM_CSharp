using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>到货通知：实际到货日 ActualArrivalDate（左栏检索 preset）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260719120000_StockInNotifyActualArrivalDate")]
public partial class StockInNotifyActualArrivalDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify
              ADD COLUMN IF NOT EXISTS "ActualArrivalDate" timestamp with time zone NULL;

            COMMENT ON COLUMN public.stockin_notify."ActualArrivalDate" IS '实际到货日：首次 status≥20 时写入；回滚至未到货(10)时清空';

            UPDATE public.stockin_notify
            SET "ActualArrivalDate" = date_trunc('day', "ModifyTime")
            WHERE is_deleted = false
              AND "Status" >= 20
              AND "ActualArrivalDate" IS NULL
              AND "ModifyTime" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS "ActualArrivalDate";
            """);
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 出库通知 / 到货通知表重命名：stockoutrequest → stockout_notify，stockinnotify → stockin_notify。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260520100000_RenameStockNotifyTables")]
public partial class RenameStockNotifyTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockoutrequest'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockout_notify'
              ) THEN
                ALTER TABLE public.stockoutrequest RENAME TO stockout_notify;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockinnotify'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockin_notify'
              ) THEN
                ALTER TABLE public.stockinnotify RENAME TO stockin_notify;
              END IF;
            END $$;

            COMMENT ON TABLE public.stockout_notify IS '出库通知（单表，一条通知对应一条销售订单明细）';
            COMMENT ON TABLE public.stockin_notify IS '到货通知（单表，一条记录对应采购明细上的一次到货批次）';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockout_notify'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockoutrequest'
              ) THEN
                ALTER TABLE public.stockout_notify RENAME TO stockoutrequest;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockin_notify'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = 'stockinnotify'
              ) THEN
                ALTER TABLE public.stockin_notify RENAME TO stockinnotify;
              END IF;
            END $$;
            """);
    }
}

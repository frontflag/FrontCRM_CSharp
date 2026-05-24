using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// stockout_notify：UserId → ID，RequestCode → Code。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260521100000_StockOutNotifyRenameIdAndCodeColumns")]
public partial class StockOutNotifyRenameIdAndCodeColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            DECLARE
              tbl text;
            BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                tbl := 'stockout_notify';
              ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutrequest') THEN
                tbl := 'stockoutrequest';
              ELSE
                RETURN;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'UserId'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'ID'
              ) THEN
                EXECUTE format('ALTER TABLE public.%I RENAME COLUMN "UserId" TO "ID"', tbl);
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'RequestCode'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = tbl AND column_name = 'Code'
              ) THEN
                EXECUTE format('ALTER TABLE public.%I RENAME COLUMN "RequestCode" TO "Code"', tbl);
              END IF;
            END $$;

            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
              COMMENT ON COLUMN public.stockout_notify."ID" IS '出库通知主键（GUID）';
              COMMENT ON COLUMN public.stockout_notify."Code" IS '出库通知单号（如 STORxxxxx）';
            END IF;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                RETURN;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'ID'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'UserId'
              ) THEN
                ALTER TABLE public.stockout_notify RENAME COLUMN "ID" TO "UserId";
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'Code'
              ) AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'RequestCode'
              ) THEN
                ALTER TABLE public.stockout_notify RENAME COLUMN "Code" TO "RequestCode";
              END IF;
            END $$;
            """);
    }
}

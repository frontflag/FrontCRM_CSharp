using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>packing_item 增加 stockout_notify_id，并自 stockout_notify.ID 回填。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260522100000_AddPackingItemStockOutNotifyId")]
public partial class AddPackingItemStockOutNotifyId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.packing_item
                ADD COLUMN IF NOT EXISTS stockout_notify_id character varying(36) NULL;

            COMMENT ON COLUMN public.packing_item.stockout_notify_id IS '出库通知主键，对应 stockout_notify."ID"';

            CREATE INDEX IF NOT EXISTS "IX_packing_item_stockout_notify_id"
                ON public.packing_item (stockout_notify_id)
                WHERE is_deleted = false AND stockout_notify_id IS NOT NULL;

            DO $$
            DECLARE
              notify_tbl text;
              notify_pk text;
              notify_line_col text;
            BEGIN
              IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                notify_tbl := 'stockout_notify';
              ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutrequest') THEN
                notify_tbl := 'stockoutrequest';
              ELSE
                RETURN;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = notify_tbl AND column_name = 'ID'
              ) THEN
                notify_pk := 'ID';
              ELSE
                notify_pk := 'UserId';
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = notify_tbl AND column_name = 'SalesOrderItemId'
              ) THEN
                notify_line_col := 'SalesOrderItemId';
              ELSE
                notify_line_col := 'sales_order_item_id';
              END IF;

              EXECUTE format($sql$
                UPDATE public.packing_item pi
                SET stockout_notify_id = sub.notify_id
                FROM (
                  SELECT DISTINCT ON (sor.%1$I) sor.%1$I AS sell_line_id, sor.%2$I AS notify_id
                  FROM public.%3$I sor
                  WHERE NOT COALESCE(sor.is_deleted, false)
                    AND sor.%1$I IS NOT NULL
                    AND TRIM(sor.%1$I::text) <> ''
                  ORDER BY sor.%1$I,
                    CASE WHEN sor."Status" = 20 THEN 0 WHEN sor."Status" = 10 THEN 1 ELSE 2 END,
                    sor."CreateTime" DESC NULLS LAST
                ) sub
                WHERE NOT pi.is_deleted
                  AND pi.sell_order_item_id IS NOT NULL
                  AND TRIM(pi.sell_order_item_id) <> ''
                  AND pi.sell_order_item_id = sub.sell_line_id
                  AND (pi.stockout_notify_id IS NULL OR TRIM(pi.stockout_notify_id) = '')
              $sql$, notify_line_col, notify_pk, notify_tbl);
            END $$;

            DO $$
            BEGIN
              IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockout_notify') THEN
                RETURN;
              END IF;
              IF NOT EXISTS (
                SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_item_stockout_notify_stockout_notify_id'
              ) THEN
                ALTER TABLE public.packing_item
                  ADD CONSTRAINT "FK_packing_item_stockout_notify_stockout_notify_id"
                  FOREIGN KEY (stockout_notify_id) REFERENCES public.stockout_notify ("ID")
                  ON DELETE SET NULL;
              END IF;
            EXCEPTION
              WHEN undefined_column THEN
                NULL;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.packing_item
                DROP CONSTRAINT IF EXISTS "FK_packing_item_stockout_notify_stockout_notify_id";
            DROP INDEX IF EXISTS "IX_packing_item_stockout_notify_id";
            ALTER TABLE IF EXISTS public.packing_item DROP COLUMN IF EXISTS stockout_notify_id;
            """);
    }
}

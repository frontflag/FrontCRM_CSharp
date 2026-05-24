using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 拣货任务关联装箱单；拣货明细行关联装箱明细（packing_item_id 在 pickingtaskitem 上）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260523100000_PickingTaskPackingLinkage")]
public partial class PickingTaskPackingLinkage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.pickingtask
                ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

            -- 若曾误加在 pickingtask 上，则移除
            ALTER TABLE IF EXISTS public.pickingtask
                DROP COLUMN IF EXISTS packing_item_id;

            COMMENT ON COLUMN public.pickingtask.packing_id IS '装箱单主键，对应 packing."Id"';

            CREATE INDEX IF NOT EXISTS "IX_pickingtask_packing_id"
                ON public.pickingtask (packing_id)
                WHERE COALESCE(is_deleted, false) = false AND packing_id IS NOT NULL;

            ALTER TABLE IF EXISTS public.pickingtaskitem
                ADD COLUMN IF NOT EXISTS packing_item_id character varying(36) NULL;

            COMMENT ON COLUMN public.pickingtaskitem.packing_item_id IS '装箱明细主键，对应 packing_item."Id"';

            CREATE INDEX IF NOT EXISTS "IX_pickingtaskitem_packing_item_id"
                ON public.pickingtaskitem (packing_item_id)
                WHERE COALESCE(is_deleted, false) = false AND packing_item_id IS NOT NULL;

            UPDATE public.pickingtask pt
            SET packing_id = sub.packing_id
            FROM (
                SELECT DISTINCT ON (TRIM(pi.stockout_notify_id))
                    TRIM(pi.stockout_notify_id) AS stock_out_request_id,
                    pi."PackingId" AS packing_id
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.stockout_notify_id IS NOT NULL
                  AND TRIM(pi.stockout_notify_id) <> ''
                ORDER BY TRIM(pi.stockout_notify_id), pi."CreateTime" DESC NULLS LAST, pi."Id"
            ) sub
            WHERE COALESCE(pt.is_deleted, false) = false
              AND TRIM(pt."StockOutRequestId") = sub.stock_out_request_id
              AND pt.packing_id IS NULL;

            UPDATE public.pickingtaskitem pti
            SET packing_item_id = pi."Id"
            FROM public.pickingtask pt
            INNER JOIN public.packing_item pi
                ON COALESCE(pi.is_deleted, false) = false
               AND pi."PackingId" = pt.packing_id
               AND (
                    (pi.stock_item_id IS NOT NULL AND TRIM(pi.stock_item_id) <> ''
                     AND TRIM(pi.stock_item_id) = TRIM(pti.stock_item_id))
                    OR (
                        pi.stockout_notify_id IS NOT NULL
                        AND TRIM(pi.stockout_notify_id) = TRIM(pt."StockOutRequestId")
                        AND (pi.stock_item_id IS NULL OR TRIM(pi.stock_item_id) = '')
                    )
               )
            WHERE COALESCE(pti.is_deleted, false) = false
              AND COALESCE(pt.is_deleted, false) = false
              AND pti."PickingTaskId" = pt."Id"
              AND pt.packing_id IS NOT NULL
              AND pti.packing_item_id IS NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_pickingtask_packing_packing_id') THEN
                    ALTER TABLE public.pickingtask
                        ADD CONSTRAINT "FK_pickingtask_packing_packing_id"
                        FOREIGN KEY (packing_id) REFERENCES public.packing ("Id")
                        ON DELETE SET NULL;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_pickingtaskitem_packing_item_packing_item_id') THEN
                    ALTER TABLE public.pickingtaskitem
                        ADD CONSTRAINT "FK_pickingtaskitem_packing_item_packing_item_id"
                        FOREIGN KEY (packing_item_id) REFERENCES public.packing_item ("Id")
                        ON DELETE SET NULL;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.pickingtaskitem
                DROP CONSTRAINT IF EXISTS "FK_pickingtaskitem_packing_item_packing_item_id";
            ALTER TABLE IF EXISTS public.pickingtask
                DROP CONSTRAINT IF EXISTS "FK_pickingtask_packing_packing_id";
            DROP INDEX IF EXISTS "IX_pickingtaskitem_packing_item_id";
            DROP INDEX IF EXISTS "IX_pickingtask_packing_id";
            ALTER TABLE IF EXISTS public.pickingtaskitem DROP COLUMN IF EXISTS packing_item_id;
            ALTER TABLE IF EXISTS public.pickingtask DROP COLUMN IF EXISTS packing_id;
            """);
    }
}

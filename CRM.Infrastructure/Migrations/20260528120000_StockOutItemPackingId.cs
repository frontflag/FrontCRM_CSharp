using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 出库明细关联装箱单主键 <c>packing."Id"</c>（多选装箱生成出库时写入）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260528120000_StockOutItemPackingId")]
public partial class StockOutItemPackingId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stock_out_item
                ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

            COMMENT ON COLUMN public.stock_out_item.packing_id IS '装箱单主键，对应 packing."Id"';

            CREATE INDEX IF NOT EXISTS "IX_stock_out_item_packing_id"
                ON public.stock_out_item (packing_id)
                WHERE COALESCE(is_deleted, false) = false
                  AND packing_id IS NOT NULL;

            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'FK_stock_out_item_packing_packing_id'
                ) THEN
                    ALTER TABLE public.stock_out_item
                        ADD CONSTRAINT "FK_stock_out_item_packing_packing_id"
                        FOREIGN KEY (packing_id) REFERENCES public.packing ("Id")
                        ON DELETE SET NULL;
                END IF;
            END $$;

            UPDATE public.stock_out_item soi
            SET packing_id = pt.packing_id
            FROM public.stock_out so
            INNER JOIN public.pickingtask pt
                ON pt."Id" = so.picking_task_id
               AND COALESCE(pt.is_deleted, false) = false
            WHERE COALESCE(soi.is_deleted, false) = false
              AND soi."StockOutId" = so."Id"
              AND COALESCE(so.is_deleted, false) = false
              AND pt.packing_id IS NOT NULL
              AND TRIM(pt.packing_id) <> ''
              AND (soi.packing_id IS NULL OR TRIM(soi.packing_id) = '');

            UPDATE public.stock_out_item soi
            SET packing_id = pt.packing_id
            FROM public.pickingtaskitem pti
            INNER JOIN public.pickingtask pt
                ON pt."Id" = pti."PickingTaskId"
               AND COALESCE(pt.is_deleted, false) = false
            WHERE COALESCE(soi.is_deleted, false) = false
              AND COALESCE(pti.is_deleted, false) = false
              AND soi.picking_task_item_id IS NOT NULL
              AND TRIM(soi.picking_task_item_id) = pti."Id"
              AND pt.packing_id IS NOT NULL
              AND TRIM(pt.packing_id) <> ''
              AND (soi.packing_id IS NULL OR TRIM(soi.packing_id) = '');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.stock_out_item
                DROP CONSTRAINT IF EXISTS "FK_stock_out_item_packing_packing_id";

            DROP INDEX IF EXISTS "IX_stock_out_item_packing_id";

            ALTER TABLE IF EXISTS public.stock_out_item
                DROP COLUMN IF EXISTS packing_id;
            """);
    }
}

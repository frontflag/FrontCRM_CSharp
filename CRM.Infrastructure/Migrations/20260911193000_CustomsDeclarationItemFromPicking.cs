using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关明细按拣货行生成：picking_task_item_id 唯一，取消 packing_item_id 唯一。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260911193000_CustomsDeclarationItemFromPicking")]
    public partial class CustomsDeclarationItemFromPicking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration_item
                    ADD COLUMN IF NOT EXISTS picking_task_item_id character varying(36) NULL;

                COMMENT ON COLUMN public.customs_declaration_item.picking_task_item_id IS '拣货任务明细；一拣货行一报关明细';

                DROP INDEX IF EXISTS "UX_cdi_packing_item";

                CREATE INDEX IF NOT EXISTS "IX_cdi_packing_item"
                    ON public.customs_declaration_item (packing_item_id)
                    WHERE is_deleted = false AND packing_item_id IS NOT NULL;

                UPDATE public.customs_declaration_item cdi
                SET picking_task_item_id = sub.pti_id
                FROM (
                    SELECT DISTINCT ON (pti.packing_item_id)
                        pti.packing_item_id,
                        pti."Id" AS pti_id
                    FROM public.pickingtaskitem pti
                    INNER JOIN public.pickingtask pt ON pt."Id" = pti."PickingTaskId"
                    WHERE COALESCE(pti.is_deleted, false) = false
                      AND pti.packing_item_id IS NOT NULL
                      AND pt."Status" = 100
                    ORDER BY pti.packing_item_id, pti."CreateTime" NULLS LAST, pti."Id"
                ) sub
                WHERE COALESCE(cdi.is_deleted, false) = false
                  AND cdi.picking_task_item_id IS NULL
                  AND cdi.packing_item_id = sub.packing_item_id;

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_cdi_picking_task_item"
                    ON public.customs_declaration_item (picking_task_item_id)
                    WHERE is_deleted = false AND picking_task_item_id IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "UX_cdi_picking_task_item";

                ALTER TABLE public.customs_declaration_item
                    DROP COLUMN IF EXISTS picking_task_item_id;

                DROP INDEX IF EXISTS "IX_cdi_packing_item";

                CREATE UNIQUE INDEX IF NOT EXISTS "UX_cdi_packing_item"
                    ON public.customs_declaration_item (packing_item_id)
                    WHERE is_deleted = false AND packing_item_id IS NOT NULL;
                """);
        }
    }
}

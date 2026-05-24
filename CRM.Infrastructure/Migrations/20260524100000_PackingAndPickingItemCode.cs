using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// packing_item / pickingtaskitem 增加 item_code；packing 增加 last_item_line_seq 并回填历史编号。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260524100000_PackingAndPickingItemCode")]
public partial class PackingAndPickingItemCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.packing
                ADD COLUMN IF NOT EXISTS last_item_line_seq integer NOT NULL DEFAULT 0;

            COMMENT ON COLUMN public.packing.last_item_line_seq IS '装箱明细行序号水位（删除行不回收）';

            ALTER TABLE IF EXISTS public.packing_item
                ADD COLUMN IF NOT EXISTS item_code character varying(64) NULL;

            COMMENT ON COLUMN public.packing_item.item_code IS '装箱明细业务编号（装箱单号-行序）';

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_item_packing_id_item_code"
                ON public.packing_item ("PackingId", item_code)
                WHERE COALESCE(is_deleted, false) = false AND item_code IS NOT NULL AND TRIM(item_code) <> '';

            ALTER TABLE IF EXISTS public.pickingtaskitem
                ADD COLUMN IF NOT EXISTS item_code character varying(64) NULL;

            COMMENT ON COLUMN public.pickingtaskitem.item_code IS '拣货明细业务编号（通常以装箱明细编号为前缀）';

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_pickingtaskitem_picking_task_id_item_code"
                ON public.pickingtaskitem ("PickingTaskId", item_code)
                WHERE COALESCE(is_deleted, false) = false AND item_code IS NOT NULL AND TRIM(item_code) <> '';

            -- 回填 packing_item.item_code 与 packing.last_item_line_seq
            WITH ranked AS (
                SELECT
                    pi."Id" AS packing_item_id,
                    pi."PackingId" AS packing_id,
                    p."Code" AS packing_code,
                    ROW_NUMBER() OVER (
                        PARTITION BY pi."PackingId"
                        ORDER BY pi."CreateTime", pi."Id"
                    ) AS line_seq
                FROM public.packing_item pi
                INNER JOIN public.packing p ON p."Id" = pi."PackingId"
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND COALESCE(p.is_deleted, false) = false
                  AND (pi.item_code IS NULL OR TRIM(pi.item_code) = '')
            )
            UPDATE public.packing_item pi
            SET item_code = ranked.packing_code || '-' || ranked.line_seq::text
            FROM ranked
            WHERE pi."Id" = ranked.packing_item_id;

            UPDATE public.packing p
            SET last_item_line_seq = sub.max_seq
            FROM (
                SELECT
                    pi."PackingId" AS packing_id,
                    MAX(
                        CASE
                            WHEN pi.item_code ~ '-[0-9]+$' THEN
                                NULLIF(
                                    regexp_replace(pi.item_code, '^.*-([0-9]+)$', '\1'),
                                    ''
                                )::integer
                            ELSE 0
                        END
                    ) AS max_seq
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.item_code IS NOT NULL
                  AND TRIM(pi.item_code) <> ''
                GROUP BY pi."PackingId"
            ) sub
            WHERE p."Id" = sub.packing_id
              AND COALESCE(p.is_deleted, false) = false;

            -- 回填 pickingtaskitem：单条同装箱明细编号，多条加 -1/-2
            WITH pick_ranked AS (
                SELECT
                    pti."Id" AS picking_item_id,
                    pi.item_code AS packing_item_code,
                    ROW_NUMBER() OVER (
                        PARTITION BY pti."PickingTaskId", pti.packing_item_id
                        ORDER BY pti."CreateTime", pti."Id"
                    ) AS sub_seq,
                    COUNT(*) OVER (
                        PARTITION BY pti."PickingTaskId", pti.packing_item_id
                    ) AS cnt_in_group
                FROM public.pickingtaskitem pti
                LEFT JOIN public.packing_item pi
                    ON pi."Id" = pti.packing_item_id
                   AND COALESCE(pi.is_deleted, false) = false
                WHERE COALESCE(pti.is_deleted, false) = false
                  AND (pti.item_code IS NULL OR TRIM(pti.item_code) = '')
            )
            UPDATE public.pickingtaskitem pti
            SET item_code = CASE
                WHEN pick_ranked.packing_item_code IS NOT NULL AND TRIM(pick_ranked.packing_item_code) <> '' THEN
                    CASE
                        WHEN pick_ranked.cnt_in_group = 1 THEN TRIM(pick_ranked.packing_item_code)
                        ELSE TRIM(pick_ranked.packing_item_code) || '-' || pick_ranked.sub_seq::text
                    END
                ELSE NULL
            END
            FROM pick_ranked
            WHERE pti."Id" = pick_ranked.picking_item_id;

            -- 无 packing_item 关联的回退：拣货任务号-行序
            WITH task_ranked AS (
                SELECT
                    pti."Id" AS picking_item_id,
                    pt."TaskCode" AS task_code,
                    ROW_NUMBER() OVER (
                        PARTITION BY pti."PickingTaskId"
                        ORDER BY pti."CreateTime", pti."Id"
                    ) AS line_seq
                FROM public.pickingtaskitem pti
                INNER JOIN public.pickingtask pt ON pt."Id" = pti."PickingTaskId"
                WHERE COALESCE(pti.is_deleted, false) = false
                  AND COALESCE(pt.is_deleted, false) = false
                  AND (pti.item_code IS NULL OR TRIM(pti.item_code) = '')
            )
            UPDATE public.pickingtaskitem pti
            SET item_code = task_ranked.task_code || '-' || task_ranked.line_seq::text
            FROM task_ranked
            WHERE pti."Id" = task_ranked.picking_item_id
              AND task_ranked.task_code IS NOT NULL
              AND TRIM(task_ranked.task_code) <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_pickingtaskitem_picking_task_id_item_code";
            ALTER TABLE IF EXISTS public.pickingtaskitem DROP COLUMN IF EXISTS item_code;

            DROP INDEX IF EXISTS "IX_packing_item_packing_id_item_code";
            ALTER TABLE IF EXISTS public.packing_item DROP COLUMN IF EXISTS item_code;

            ALTER TABLE IF EXISTS public.packing DROP COLUMN IF EXISTS last_item_line_seq;
            """);
    }
}

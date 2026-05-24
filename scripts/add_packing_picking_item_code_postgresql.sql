-- 装箱/拣货明细 item_code（与迁移 20260524100000 一致，可单独在环境执行）
-- packing_item.item_code、packing.last_item_line_seq、pickingtaskitem.item_code

ALTER TABLE IF EXISTS public.packing
    ADD COLUMN IF NOT EXISTS last_item_line_seq integer NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.packing_item
    ADD COLUMN IF NOT EXISTS item_code character varying(64) NULL;

ALTER TABLE IF EXISTS public.pickingtaskitem
    ADD COLUMN IF NOT EXISTS item_code character varying(64) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_item_packing_id_item_code"
    ON public.packing_item ("PackingId", item_code)
    WHERE COALESCE(is_deleted, false) = false AND item_code IS NOT NULL AND TRIM(item_code) <> '';

CREATE UNIQUE INDEX IF NOT EXISTS "IX_pickingtaskitem_picking_task_id_item_code"
    ON public.pickingtaskitem ("PickingTaskId", item_code)
    WHERE COALESCE(is_deleted, false) = false AND item_code IS NOT NULL AND TRIM(item_code) <> '';

-- 回填 packing_item.item_code（{装箱单号}-{行序}）
WITH ranked AS (
    SELECT
        pi."Id" AS packing_item_id,
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

-- 回填 pickingtaskitem.item_code（单条同装箱明细编号，多条 -1/-2）
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

-- 无 packing_item 关联：拣货任务号-行序
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

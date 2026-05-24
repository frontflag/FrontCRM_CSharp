-- 历史数据回填：pickingtask.packing_id / pickingtaskitem.packing_item_id
-- 执行前请确认列已存在（见 add_picking_packing_linkage_columns_postgresql.sql）

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. pickingtask.packing_id ← packing."Id"
-- ---------------------------------------------------------------------------

-- 1.1 按 packing_item.stockout_notify_id
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
  AND (pt.packing_id IS NULL OR TRIM(pt.packing_id) = '');

-- 1.2 补充：packing_item 无 stockout_notify_id 时，按销售行匹配
-- 注意：DISTINCT ON 与 ORDER BY 左侧表达式必须一致（勿 DISTINCT ON(TRIM(...)) 而 ORDER BY 未 TRIM）
UPDATE public.pickingtask pt
SET packing_id = sub.packing_id
FROM (
    SELECT DISTINCT ON (pt2."Id")
        pt2."Id" AS picking_task_id,
        pi."PackingId" AS packing_id
    FROM public.pickingtask pt2
    INNER JOIN public.stockout_notify sor
        ON TRIM(sor."ID") = TRIM(pt2."StockOutRequestId")
    INNER JOIN public.packing_item pi
        ON COALESCE(pi.is_deleted, false) = false
       AND TRIM(pi.sell_order_item_id) = TRIM(sor."SalesOrderItemId")
       AND (pi.stockout_notify_id IS NULL OR TRIM(pi.stockout_notify_id) = '')
    WHERE COALESCE(pt2.is_deleted, false) = false
      AND (pt2.packing_id IS NULL OR TRIM(pt2.packing_id) = '')
    ORDER BY pt2."Id", pi."CreateTime" DESC NULLS LAST, pi."Id"
) sub
WHERE pt."Id" = sub.packing_task_id;

-- ---------------------------------------------------------------------------
-- 2. pickingtaskitem.packing_item_id ← packing_item."Id"
-- ---------------------------------------------------------------------------

UPDATE public.pickingtaskitem pti
SET packing_item_id = pi."Id"
FROM public.pickingtask pt
INNER JOIN public.packing_item pi
    ON COALESCE(pi.is_deleted, false) = false
   AND pi."PackingId" = pt.packing_id
   AND (
        (
            pi.stock_item_id IS NOT NULL
            AND TRIM(pi.stock_item_id) <> ''
            AND pti.stock_item_id IS NOT NULL
            AND TRIM(pti.stock_item_id) <> ''
            AND TRIM(pi.stock_item_id) = TRIM(pti.stock_item_id)
        )
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
  AND TRIM(pt.packing_id) <> ''
  AND (pti.packing_item_id IS NULL OR TRIM(pti.packing_item_id) = '');

-- 2.1 仍为空且该装箱单仅一条明细时兜底
UPDATE public.pickingtaskitem pti
SET packing_item_id = sub.packing_item_id
FROM (
    SELECT
        pti2."Id" AS picking_task_item_id,
        MIN(pi."Id") AS packing_item_id
    FROM public.pickingtaskitem pti2
    INNER JOIN public.pickingtask pt2 ON pt2."Id" = pti2."PickingTaskId"
    INNER JOIN public.packing_item pi
        ON pi."PackingId" = pt2.packing_id
       AND COALESCE(pi.is_deleted, false) = false
    WHERE COALESCE(pti2.is_deleted, false) = false
      AND COALESCE(pt2.is_deleted, false) = false
      AND pt2.packing_id IS NOT NULL
      AND (pti2.packing_item_id IS NULL OR TRIM(pti2.packing_item_id) = '')
    GROUP BY pti2."Id", pt2.packing_id
    HAVING COUNT(DISTINCT pi."Id") = 1
) sub
WHERE pti."Id" = sub.picking_task_item_id;

COMMIT;

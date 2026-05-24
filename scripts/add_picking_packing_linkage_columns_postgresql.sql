-- 拣货 ↔ 装箱关联（修正版）
-- pickingtask.packing_id      ← packing."Id"
-- pickingtaskitem.packing_item_id ← packing_item."Id"（不在 pickingtask 上）

ALTER TABLE IF EXISTS public.pickingtask
    ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

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

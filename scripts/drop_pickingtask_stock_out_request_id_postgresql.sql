-- 拣货任务仅关联装箱单：回填 packing_id 后删除 StockOutRequestId
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
  AND pt."StockOutRequestId" IS NOT NULL
  AND TRIM(pt."StockOutRequestId") = sub.stock_out_request_id
  AND (pt.packing_id IS NULL OR TRIM(pt.packing_id) = '');

ALTER TABLE IF EXISTS public.pickingtask
    DROP COLUMN IF EXISTS "StockOutRequestId";

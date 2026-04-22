-- =============================================================================
-- public.stock_out_item_extend：冗余入库明细 StockInItemId、stock_in_item_code
-- 回填：经 stock_item."StockItemId" = ext."StockItemId"，对齐 stock_in_item
-- =============================================================================

ALTER TABLE IF EXISTS public.stock_out_item_extend ADD COLUMN IF NOT EXISTS "StockInItemId" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_out_item_extend ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

UPDATE public.stock_out_item_extend ext
SET
  "StockInItemId" = NULLIF(TRIM(si."StockInItemId"), ''),
  stock_in_item_code = NULLIF(TRIM(BOTH FROM COALESCE(si.stock_in_item_code, ii.stock_in_item_code)), '')
FROM public.stock_item si
LEFT JOIN public.stock_in_item ii ON ii."ItemId" = si."StockInItemId"
WHERE NULLIF(TRIM(ext."StockItemId"), '') IS NOT NULL
  AND si."StockItemId" = ext."StockItemId";

COMMENT ON COLUMN public.stock_out_item_extend."StockInItemId" IS '对应入库明细主键（冗余自 stock_in_item.ItemId，经 stock_item.StockInItemId）';
COMMENT ON COLUMN public.stock_out_item_extend.stock_in_item_code IS '入库明细业务编号（冗余自 stock_in_item.stock_in_item_code；可与 stock_item.stock_in_item_code 对齐）';

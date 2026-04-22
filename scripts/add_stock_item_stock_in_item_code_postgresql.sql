-- =============================================================================
-- public.stock_item：冗余入库明细业务编号 stock_in_item_code（与 stock_in_item 一致）
-- =============================================================================

ALTER TABLE IF EXISTS public.stock_item ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

UPDATE public.stock_item it
SET stock_in_item_code = NULLIF(TRIM(ii.stock_in_item_code), '')
FROM public.stock_in_item ii
WHERE ii."ItemId" = it."StockInItemId";

COMMENT ON COLUMN public.stock_item.stock_in_item_code IS '对应入库明细业务编号（冗余自 stock_in_item.stock_in_item_code）';

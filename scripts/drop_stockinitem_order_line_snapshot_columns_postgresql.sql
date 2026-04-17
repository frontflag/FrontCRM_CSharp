-- 从 stockinitem 删除与 stockin 头表重复的采销行快照列（与迁移 20260628120000 一致）
ALTER TABLE IF EXISTS public.stock_in_item DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stock_in_item DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stock_in_item DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stock_in_item DROP COLUMN IF EXISTS purchase_order_item_code;


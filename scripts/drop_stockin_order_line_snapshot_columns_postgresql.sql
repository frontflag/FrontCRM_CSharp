-- 与迁移 20260629130000_RemoveStockInOrderLineSnapshotColumns 一致：去掉 stockin 头表采销行冗余列。
-- 请先确保 stockinitemextend 已存在且数据已回填（见 scripts/backfill_stockinitemextend_from_stockin_postgresql.sql）。

ALTER TABLE IF EXISTS public.stock_in DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stock_in DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stock_in DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stock_in DROP COLUMN IF EXISTS purchase_order_item_code;


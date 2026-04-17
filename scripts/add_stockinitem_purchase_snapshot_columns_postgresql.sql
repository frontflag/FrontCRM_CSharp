-- stockinitem：采购/销售明细快照列（与迁移 20260624120000_StockInItemPurchaseSnapshotColumns 一致）
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_pn character varying(200) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_brand character varying(200) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS sell_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS currency smallint NULL;


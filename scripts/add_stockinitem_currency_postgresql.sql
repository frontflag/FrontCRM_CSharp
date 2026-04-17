-- stockinitem：采购币别（与 purchaseorderitem.currency 一致；EF 迁移见 20260624120000_StockInItemPurchaseSnapshotColumns）
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS currency smallint NULL;
COMMENT ON COLUMN public.stock_in_item.currency IS '采购币别（与 purchaseorderitem.currency 一致）';


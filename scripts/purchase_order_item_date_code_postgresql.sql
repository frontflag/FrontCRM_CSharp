-- 采购订单明细：生产日期/DC 要求（与 sellorderitem.date_code 对齐）
ALTER TABLE purchaseorderitem
  ADD COLUMN IF NOT EXISTS date_code character varying(100) NULL;

COMMENT ON COLUMN purchaseorderitem.date_code IS '生产日期/DC 要求（字典 ItemCode，如 26+）';

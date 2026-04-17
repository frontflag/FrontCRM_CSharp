-- 出库单 stockout：出货方式、快递单号（与 EF 迁移 20260406210000_AddStockOutShipmentCourier 一致）

ALTER TABLE IF EXISTS public.stock_out
  ADD COLUMN IF NOT EXISTS "ShipmentMethod" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock_out
  ADD COLUMN IF NOT EXISTS "CourierTrackingNo" character varying(128) NULL;


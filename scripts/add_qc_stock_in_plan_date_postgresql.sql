-- 质检主表：计划入库日（与迁移 20260623100000_QcInfoStockInPlanDate 一致，可单独在已有库执行）
ALTER TABLE IF EXISTS public.qcinfo
  ADD COLUMN IF NOT EXISTS "StockInPlanDate" timestamp with time zone NULL;
COMMENT ON COLUMN public.qcinfo."StockInPlanDate" IS '质检保存时填写的计划入库日；质检列表生成入库单时作为入库日期';

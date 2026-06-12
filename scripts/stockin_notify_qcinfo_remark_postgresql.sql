-- 到货通知 / 质检备注列（与迁移 20260611150000_StockInNotifyQcInfoRemark 一致，可单独执行）

ALTER TABLE IF EXISTS public.stockin_notify
  ADD COLUMN IF NOT EXISTS "Remark" character varying(500) NULL;
COMMENT ON COLUMN public.stockin_notify."Remark" IS '到货通知备注（采购创建到货通知时填写）';

ALTER TABLE IF EXISTS public.qcinfo
  ADD COLUMN IF NOT EXISTS "Remark" character varying(500) NULL;
COMMENT ON COLUMN public.qcinfo."Remark" IS '质检备注（质检保存时填写）';

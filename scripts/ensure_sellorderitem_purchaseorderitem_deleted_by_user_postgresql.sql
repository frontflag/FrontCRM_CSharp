-- 销售/采购明细软删除操作人（删除日志「操作人」列）；与迁移 20260728120000 一致。
ALTER TABLE IF EXISTS public.sellorderitem
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_name character varying(100) NULL;

ALTER TABLE IF EXISTS public.purchaseorderitem
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_name character varying(100) NULL;

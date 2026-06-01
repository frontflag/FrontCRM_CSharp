-- SO/PO 主单与明细 extend 表软删除标志（与 sellorder / sellorderitem / purchaseorder / purchaseorderitem 一致）
-- 业务：DeleteAsync / Sync*ItemsOnUpdateAsync 通过 Repository.DeleteAsync(ISoftDeletable) 标删，不再物理删除 extend 行。

ALTER TABLE IF EXISTS public.sellorderextend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.sellorderitemextend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.purchaseorderextend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.purchaseorderitemextend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.sellorderextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 sellorder 整单删除同步';
COMMENT ON COLUMN public.sellorderitemextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 sellorderitem 明细删除同步';
COMMENT ON COLUMN public.purchaseorderextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 purchaseorder 整单删除同步';
COMMENT ON COLUMN public.purchaseorderitemextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 purchaseorderitem 明细删除同步';

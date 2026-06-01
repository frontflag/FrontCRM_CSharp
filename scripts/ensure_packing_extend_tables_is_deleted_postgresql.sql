-- 装箱 extend 四表软删除标志（与 SO/PO extend、packing 主表一致）
-- 业务：DeletePackingAsync 标删 extend 行，不再 RemoveRange 物理删除。

ALTER TABLE IF EXISTS public.packing_extend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.packing_extend_box
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.packing_extend_ship
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.packing_item_extend
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.packing_extend.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
COMMENT ON COLUMN public.packing_extend_box.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
COMMENT ON COLUMN public.packing_extend_ship.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
COMMENT ON COLUMN public.packing_item_extend.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing_item 明细删除同步';

-- 软删后允许同一 PackingId / PackingItemId 重新建扩展行
DROP INDEX IF EXISTS public."IX_packing_extend_box_PackingId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_box_PackingId"
  ON public.packing_extend_box ("PackingId")
  WHERE is_deleted = false;

DROP INDEX IF EXISTS public."IX_packing_extend_ship_PackingId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_ship_PackingId"
  ON public.packing_extend_ship ("PackingId")
  WHERE is_deleted = false;

DROP INDEX IF EXISTS public."IX_packing_item_extend_PackingItemId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_item_extend_PackingItemId"
  ON public.packing_item_extend ("PackingItemId")
  WHERE is_deleted = false;

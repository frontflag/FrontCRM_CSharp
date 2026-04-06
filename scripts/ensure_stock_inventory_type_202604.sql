-- =============================================================================
-- stock."Type"：库存类型（1=客单库存 2=备货库存 3=样品库存）
-- 对应迁移：20260420120000_AddStockInventoryType
-- PostgreSQL，可重复执行。推荐优先 dotnet ef database update / DbMigrator。
-- =============================================================================

ALTER TABLE IF EXISTS public.stock
  ADD COLUMN IF NOT EXISTS "Type" smallint NOT NULL DEFAULT 1;

COMMENT ON COLUMN public.stock."Type" IS '库存类型：1客单库存 2备货库存 3样品库存';

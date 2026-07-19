-- 到货通知：实际到货日（左栏「到货」preset 与列表展示）
ALTER TABLE IF EXISTS public.stockin_notify
  ADD COLUMN IF NOT EXISTS "ActualArrivalDate" timestamp with time zone NULL;

COMMENT ON COLUMN public.stockin_notify."ActualArrivalDate" IS '实际到货日：首次 status≥20 时写入；回滚至未到货(10)时清空';

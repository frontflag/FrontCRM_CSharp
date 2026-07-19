-- 历史回填：已确认到货(status≥20)且无实到日的行，用 ModifyTime 日期近似
UPDATE public.stockin_notify
SET "ActualArrivalDate" = date_trunc('day', "ModifyTime")
WHERE is_deleted = false
  AND "Status" >= 20
  AND "ActualArrivalDate" IS NULL
  AND "ModifyTime" IS NOT NULL;

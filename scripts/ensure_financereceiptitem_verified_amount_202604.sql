-- 修复 PostgreSQL 42703: 字段 VerifiedAmount 不存在
-- 与迁移 20260405120000_SellOrderItemExtendProgressP0 中 financereceiptitem 部分一致。
-- 在目标库执行一次即可（IF NOT EXISTS 可重复执行）。

ALTER TABLE IF EXISTS public.financereceiptitem
    ADD COLUMN IF NOT EXISTS "VerifiedAmount" numeric(18,2) NOT NULL DEFAULT 0;

UPDATE public.financereceiptitem
SET "VerifiedAmount" = "ReceiptConvertAmount"
WHERE "VerificationStatus" = 2
  AND ("VerifiedAmount" IS NULL OR "VerifiedAmount" = 0);

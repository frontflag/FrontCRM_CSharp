-- 修复销售订单明细列表 /lines 等接口 500：42703 sellorderitemextend 缺列（如 FxUsdToCnySnapshot）
-- 与迁移 20260406100000_SellOrderItemExtendProfitP1P3、20260408100000_SellOrderItemExtendProgressStatuses 一致。
-- 日志若提示「待执行迁移」也可改用：dotnet run --project CRM.DbMigrator（或对本库执行 dotnet ef database update）
-- 本脚本可重复执行（IF NOT EXISTS）。

-- === 利润/报价快照（P1–P3）===
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteItemId" character varying(36);
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteCost" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteCurrency" smallint NOT NULL DEFAULT 1;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteConvertCost" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToCnySnapshot" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToHkdSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "FxUsdToEurSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SellConvertUsdUnitSnapshot" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SellLineAmountUsdSnapshot" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "QuoteProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ReQuoteProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ReQuoteProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PoCostUsdTotal" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProfitRateExpected" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PoCostUsdConfirmed" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "SalesProfitExpected" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutBizUsd" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutRateBiz" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutFinUsd" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ProfitOutRateFin" numeric(18,6) NOT NULL DEFAULT 0;

-- === 执行进度状态（与明细列表进度列一致）===
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "StockInProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "StockOutProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "ReceiptProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend ADD COLUMN IF NOT EXISTS "InvoiceProgressStatus" smallint NOT NULL DEFAULT 0;

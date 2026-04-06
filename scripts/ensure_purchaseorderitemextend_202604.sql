-- 采购明细扩展表 purchaseorderitemextend：补全表结构（缺表则建表，缺列则 ADD COLUMN）
-- 来源：迁移 20260329140000_StockInNotifySingleTableAndItemExtends、20260407120000_PurchaseOrderItemExtendProgressStatus
-- 说明：近期「入库数量按入库单汇总」等为应用层逻辑，不增加新列；本脚本仅保证表/列与当前 EF 模型一致。
-- 推荐：生产环境优先 dotnet run --project CRM.DbMigrator 或 dotnet ef database update；本脚本适合手工对齐或应急。
-- PostgreSQL，可重复执行。

CREATE TABLE IF NOT EXISTS public.purchaseorderitemextend (
    "PurchaseOrderItemId" character varying(36) NOT NULL,
    "QtyStockInNotifyNot" numeric(18,4) NOT NULL DEFAULT 0,
    "QtyStockInNotifyExpectSum" numeric(18,4) NOT NULL DEFAULT 0,
    "QtyReceiveTotal" numeric(18,4) NOT NULL DEFAULT 0,
    "PurchaseInvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "PurchaseInvoiceDone" numeric(18,2) NOT NULL DEFAULT 0,
    "PurchaseInvoiceToBe" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
    "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    "CreateUserId" bigint NULL,
    "ModifyTime" timestamp with time zone NULL,
    "ModifyUserId" bigint NULL,
    "PurchaseProgressStatus" smallint NOT NULL DEFAULT 0,
    "PurchaseProgressQty" numeric(18,4) NOT NULL DEFAULT 0,
    "StockInProgressStatus" smallint NOT NULL DEFAULT 0,
    "PaymentProgressStatus" smallint NOT NULL DEFAULT 0,
    "InvoiceProgressStatus" smallint NOT NULL DEFAULT 0,
    CONSTRAINT "PK_purchaseorderitemextend" PRIMARY KEY ("PurchaseOrderItemId")
);

-- 已有表但缺列时补齐（与上面 CREATE 列集一致）
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "QtyStockInNotifyNot" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "QtyStockInNotifyExpectSum" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "QtyReceiveTotal" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseInvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseInvoiceDone" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseInvoiceToBe" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PaymentAmountNot" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PaymentAmountFinish" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "ReceiptAmount" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "ReceiptAmountNot" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "ReceiptAmountFinish" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "CreateTime" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc');
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "CreateUserId" bigint NULL;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "ModifyTime" timestamp with time zone NULL;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "ModifyUserId" bigint NULL;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PurchaseProgressQty" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "StockInProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "PaymentProgressStatus" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS "InvoiceProgressStatus" smallint NOT NULL DEFAULT 0;

-- 为尚无扩展行的采购明细补一行（与迁移中 INSERT 逻辑一致）
INSERT INTO public.purchaseorderitemextend (
  "PurchaseOrderItemId", "QtyStockInNotifyNot", "QtyStockInNotifyExpectSum", "QtyReceiveTotal",
  "PurchaseInvoiceAmount", "PurchaseInvoiceDone", "PurchaseInvoiceToBe",
  "PaymentAmount", "PaymentAmountNot", "PaymentAmountFinish",
  "ReceiptAmount", "ReceiptAmountNot", "ReceiptAmountFinish",
  "CreateTime"
)
SELECT
  i."PurchaseOrderItemId",
  i.qty,
  0,
  0,
  ROUND((i.qty * i.cost)::numeric, 2),
  0,
  ROUND((i.qty * i.cost)::numeric, 2),
  ROUND((i.qty * i.cost)::numeric, 2),
  ROUND((i.qty * i.cost)::numeric, 2),
  0,
  0,
  0,
  0,
  NOW() AT TIME ZONE 'utc'
FROM public.purchaseorderitem i
WHERE NOT EXISTS (
  SELECT 1 FROM public.purchaseorderitemextend e WHERE e."PurchaseOrderItemId" = i."PurchaseOrderItemId"
);

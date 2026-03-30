-- =============================================================================
-- FrontCRM 远程发布 — 手工「补表」脚本（PostgreSQL）
-- =============================================================================
-- 用途：目标库若未跑过 EF 迁移，可按需执行本脚本补建表/索引，减少 42P01（表不存在）。
--
-- 推荐方式（与代码一致）：
--   在可连接目标库的环境执行：
--     dotnet ef database update -p CRM.Infrastructure -s CRM.API
--
-- 本脚本特点：
--   · 多为 CREATE TABLE IF NOT EXISTS / CREATE INDEX IF NOT EXISTS，可重复执行。
--   · 不含「到货通知单表重构」中的 TRUNCATE / DROP 明细表等破坏性步骤（见文末说明）。
--   · 执行后请部署对应版本 API；EF 迁移历史表 __EFMigrationsHistory 仍建议用 dotnet ef 对齐。
--
-- 数据库：与 appsettings 一致（默认 PostgreSQL）。
--
-- 若客户端用「单事务」执行整文件且中途报错，会出现 25P02：请在同一会话执行
--   ROLLBACK;
-- 再重跑；或改用自动提交 / 分段执行。本脚本不包裹全局 BEGIN/COMMIT，便于工具默认逐条提交。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) 审批记录 approval_record（迁移 20260326203000 + 20260326212000）
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.approval_record (
    "Id" character varying(36) NOT NULL,
    "BizType" character varying(50) NOT NULL,
    "BusinessId" character varying(36) NOT NULL,
    "DocumentCode" character varying(64),
    "ItemDescription" character varying(1000),
    "ActionType" character varying(20) NOT NULL,
    "FromStatus" smallint,
    "ToStatus" smallint,
    "SubmitRemark" character varying(500),
    "AuditRemark" character varying(500),
    "SubmitterUserId" character varying(36),
    "SubmitterUserName" character varying(100),
    "ApproverUserId" character varying(36),
    "ApproverUserName" character varying(100),
    "ActionTime" timestamp with time zone NOT NULL,
    "CreateTime" timestamp with time zone NOT NULL,
    "ModifyTime" timestamp with time zone,
    CONSTRAINT "PK_approval_record" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_approval_record_ActionTime"
    ON public.approval_record ("ActionTime");
CREATE INDEX IF NOT EXISTS "IX_approval_record_BizType_BusinessId"
    ON public.approval_record ("BizType", "BusinessId");

-- -----------------------------------------------------------------------------
-- 2) 最近打开记录 log_recent（迁移 20260328230000）
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.log_recent (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "BizType" VARCHAR(64) NOT NULL,
    "RecordId" TEXT NOT NULL,
    "RecordCode" VARCHAR(128),
    "AccessedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UserId" TEXT NOT NULL DEFAULT '',
    "OpenKind" VARCHAR(16) NOT NULL DEFAULT 'detail'
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_log_recent_user_biz_record
    ON public.log_recent ("UserId", "BizType", "RecordId");
CREATE INDEX IF NOT EXISTS idx_log_recent_user_biz_time
    ON public.log_recent ("UserId", "BizType", "AccessedAt" DESC);

-- -----------------------------------------------------------------------------
-- 3) 客户操作/变更日志（迁移 20260328140000；部分接口仍可能直接读写）
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.customer_operation_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "CustomerId" TEXT NOT NULL,
    "OperationType" VARCHAR(100) NOT NULL,
    "OperationDesc" TEXT,
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Remark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_customer_id
    ON public.customer_operation_log ("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_operation_time
    ON public.customer_operation_log ("OperationTime");

CREATE TABLE IF NOT EXISTS public.customer_change_log (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "CustomerId" TEXT NOT NULL,
    "FieldName" VARCHAR(100) NOT NULL,
    "FieldLabel" VARCHAR(200),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "ChangedByUserId" TEXT,
    "ChangedByUserName" VARCHAR(100),
    "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_customer_change_log_customer_id
    ON public.customer_change_log ("CustomerId");
CREATE INDEX IF NOT EXISTS idx_customer_change_log_changed_at
    ON public.customer_change_log ("ChangedAt");

-- -----------------------------------------------------------------------------
-- 4) 统一操作/字段变更日志（迁移 20260328210000；不含数据搬迁 DO 块）
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.log_operation (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "BizType" VARCHAR(64) NOT NULL,
    "RecordId" TEXT NOT NULL,
    "RecordCode" VARCHAR(128),
    "ActionType" VARCHAR(100) NOT NULL,
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "Reason" TEXT,
    "ExtraInfo" TEXT,
    "SysRemark" TEXT,
    "OperationDesc" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_operation_biz_record
    ON public.log_operation ("BizType", "RecordId");
CREATE INDEX IF NOT EXISTS idx_log_operation_time
    ON public.log_operation ("OperationTime");

CREATE TABLE IF NOT EXISTS public.log_change_fldval (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "BizType" VARCHAR(64) NOT NULL,
    "RecordId" TEXT NOT NULL,
    "RecordCode" VARCHAR(128),
    "FieldName" VARCHAR(100) NOT NULL,
    "FieldLabel" VARCHAR(200),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ChangedByUserId" TEXT,
    "ChangedByUserName" VARCHAR(100),
    "ExtraInfo" TEXT,
    "SysRemark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_biz_record
    ON public.log_change_fldval ("BizType", "RecordId");
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_changed_at
    ON public.log_change_fldval ("ChangedAt");

-- -----------------------------------------------------------------------------
-- 5) 采购/销售明细扩展表 + 按现有明细回填（迁移 20260329140000 节选，无 TRUNCATE）
-- -----------------------------------------------------------------------------
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
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint NULL,
    "ModifyTime" timestamp with time zone NULL,
    "ModifyUserId" bigint NULL,
    CONSTRAINT "PK_purchaseorderitemextend" PRIMARY KEY ("PurchaseOrderItemId")
);

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
    SELECT 1 FROM public.purchaseorderitemextend e
    WHERE e."PurchaseOrderItemId" = i."PurchaseOrderItemId"
);

CREATE TABLE IF NOT EXISTS public.sellorderitemextend (
    "SellOrderItemId" character varying(36) NOT NULL,
    "QtyStockOutNotify" numeric(18,4) NOT NULL DEFAULT 0,
    "QtyStockOutNotifyNot" numeric(18,4) NOT NULL DEFAULT 0,
    "InvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "InvoiceAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
    "InvoiceAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmountNot" numeric(18,2) NOT NULL DEFAULT 0,
    "ReceiptAmountFinish" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmountDone" numeric(18,2) NOT NULL DEFAULT 0,
    "PaymentAmountToBe" numeric(18,2) NOT NULL DEFAULT 0,
    "PurchaseInvoiceAmount" numeric(18,2) NOT NULL DEFAULT 0,
    "PurchaseInvoiceDone" numeric(18,2) NOT NULL DEFAULT 0,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint NULL,
    "ModifyTime" timestamp with time zone NULL,
    "ModifyUserId" bigint NULL,
    CONSTRAINT "PK_sellorderitemextend" PRIMARY KEY ("SellOrderItemId")
);

INSERT INTO public.sellorderitemextend (
    "SellOrderItemId", "QtyStockOutNotify", "QtyStockOutNotifyNot",
    "InvoiceAmount", "InvoiceAmountNot", "InvoiceAmountFinish",
    "ReceiptAmount", "ReceiptAmountNot", "ReceiptAmountFinish",
    "PaymentAmount", "PaymentAmountDone", "PaymentAmountToBe",
    "PurchaseInvoiceAmount", "PurchaseInvoiceDone",
    "CreateTime"
)
SELECT
    s."SellOrderItemId",
    0,
    s.qty,
    ROUND((s.qty * s.price)::numeric, 2),
    ROUND((s.qty * s.price)::numeric, 2),
    0,
    ROUND((s.qty * s.price)::numeric, 2),
    ROUND((s.qty * s.price)::numeric, 2),
    0,
    0,
    0,
    0,
    0,
    0,
    NOW() AT TIME ZONE 'utc'
FROM public.sellorderitem s
WHERE NOT EXISTS (
    SELECT 1 FROM public.sellorderitemextend e WHERE e."SellOrderItemId" = s."SellOrderItemId"
);

-- -----------------------------------------------------------------------------
-- 5b) stockinnotify 行级字段（迁移 20260327120000 + 20260329140000 节选，仅 ALTER）
--     与当前 EF 单表模型一致；修复旧库主表缺列导致的 42703（如 column s.Brand does not exist）。
--     不清空数据、不 DROP stockinnotifyitem。
--     【重要】请整段执行（含下面全部 ADD）。若只执行 PurchaseOrderItemId 两行，仍会因缺少 Brand 等列报错。
-- -----------------------------------------------------------------------------
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "PurchaseOrderItemId" character varying(36) NOT NULL DEFAULT '';
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "SellOrderItemId" character varying(36) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Pn" character varying(128) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Brand" character varying(64) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectQty" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ReceiveQty" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "PassedQty" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "Cost" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectTotal" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ReceiveTotal" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS "ExpectedArrivalDate" timestamp with time zone NULL;
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON c.oid = a.attrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = 'stockinnotify'
      AND a.attname = 'PurchaseOrderItemId' AND a.attnum > 0 AND NOT a.attisdropped
  ) THEN
    ALTER TABLE public.stockinnotify ALTER COLUMN "PurchaseOrderItemId" DROP DEFAULT;
  END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 5c) qcitem 列名与 EF 一致（StockInNotifyItemId -> ArrivalStockInNotifyId）
--     与迁移 20260329140000 节选一致。旧库仅改名、不清数据；否则保存质检会 DbUpdateException。
-- -----------------------------------------------------------------------------
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON c.oid = a.attrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = 'qcitem'
      AND a.attname = 'StockInNotifyItemId' AND a.attnum > 0 AND NOT a.attisdropped
  ) AND NOT EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON c.oid = a.attrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = 'qcitem'
      AND a.attname = 'ArrivalStockInNotifyId' AND a.attnum > 0 AND NOT a.attisdropped
  ) THEN
    ALTER TABLE public.qcitem RENAME COLUMN "StockInNotifyItemId" TO "ArrivalStockInNotifyId";
  END IF;
END $$;

-- stockinnotify：旧结构主表无 PurchaseOrderItemId（在 stockinnotifyitem 上）；单表重构后才
-- 有该列。仅当列真实存在时再建索引，避免 42703。
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON c.oid = a.attrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = 'stockinnotify'
      AND a.attname = 'PurchaseOrderItemId' AND a.attnum > 0 AND NOT a.attisdropped
  ) THEN
    EXECUTE 'CREATE INDEX IF NOT EXISTS "IX_stockinnotify_PurchaseOrderItemId" ON public.stockinnotify ("PurchaseOrderItemId")';
  END IF;

  IF EXISTS (
    SELECT 1 FROM pg_attribute a
    JOIN pg_class c ON c.oid = a.attrelid
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public' AND c.relname = 'stockinnotify'
      AND a.attname = 'PurchaseOrderId' AND a.attnum > 0 AND NOT a.attisdropped
  ) THEN
    EXECUTE 'CREATE INDEX IF NOT EXISTS "IX_stockinnotify_PurchaseOrderId" ON public.stockinnotify ("PurchaseOrderId")';
  END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 6) 付款单银行水单号列（迁移 20260327173000）
-- -----------------------------------------------------------------------------
ALTER TABLE public.financepayment ADD COLUMN IF NOT EXISTS "BankSlipNo" character varying(100) NULL;
COMMENT ON COLUMN public.financepayment."BankSlipNo" IS '银行水单号码';

-- =============================================================================
-- 另：财务主从表等大块结构见迁移 20260324120000_EnsureFinanceTablesIfMissing（内含
-- financepayment / financereceipt / financesellinvoice 等 CREATE TABLE）。
-- 若远程库完全空库，请优先用 dotnet ef database update 一次到位。
--
-- 「到货通知」单表重构（TRUNCATE qc/stockinnotify、DROP stockinnotifyitem 等）在
-- 20260329140000_StockInNotifySingleTableAndItemExtends — 有生产数据时切勿盲跑，
-- 必须在维护窗口备份后由 DBA 评估执行。
-- 执行该迁移后，stockinnotify 上会出现 PurchaseOrderItemId，届时可再跑一次本脚本末尾
-- 索引块（或单独执行两个 CREATE INDEX），或直接使用 dotnet ef database update。
-- =============================================================================

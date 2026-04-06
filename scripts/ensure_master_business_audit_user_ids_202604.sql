-- =============================================================================
-- 16 张主业务表：create_by_user_id / modify_by_user_id（varchar(36)，可空）
-- 对应迁移：
--   20260410120000_AddRfqCreateByUserId（仅 rfq.create_by_user_id）
--   20260411120000_AddSellOrderPurchaseOrderAuditUserIds（sellorder / purchaseorder）
--   20260412100000_AddMasterBusinessAuditUserIds（其余主表；rfq 仅 modify_by_user_id）
-- PostgreSQL，可重复执行。优先：dotnet ef database update。
-- =============================================================================

-- customerinfo
ALTER TABLE IF EXISTS public.customerinfo
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- vendorinfo
ALTER TABLE IF EXISTS public.vendorinfo
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- rfq（create 可能已由 ensure_rfq_create_by_user_id_202604.sql 加过）
ALTER TABLE IF EXISTS public.rfq
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- quote
ALTER TABLE IF EXISTS public.quote
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- sellorder / purchaseorder
ALTER TABLE IF EXISTS public.sellorder
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.purchaseorder
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- purchaserequisition
ALTER TABLE IF EXISTS public.purchaserequisition
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- stockoutrequest
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- qcinfo
ALTER TABLE IF EXISTS public.qcinfo
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- stockin / stockout
ALTER TABLE IF EXISTS public.stockin
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.stockout
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- 财务主表
ALTER TABLE IF EXISTS public.financereceipt
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.financepayment
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.financepurchaseinvoice
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.financesellinvoice
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

-- paymentrequest：表可能不存在（与 EF 迁移一致）
DO $$
BEGIN
  IF to_regclass('public.paymentrequest') IS NOT NULL THEN
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'create_by_user_id'
    ) THEN
      ALTER TABLE public.paymentrequest ADD COLUMN create_by_user_id character varying(36) NULL;
    END IF;
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'modify_by_user_id'
    ) THEN
      ALTER TABLE public.paymentrequest ADD COLUMN modify_by_user_id character varying(36) NULL;
    END IF;
  END IF;
END $$;

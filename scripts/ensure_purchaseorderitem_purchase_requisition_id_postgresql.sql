-- =============================================================================
-- PO 明细增加 purchase_requisition_id（可重复执行）
-- 对应 EF 迁移：20260707120000_PurchaseOrderItemPurchaseRequisitionId
--
-- 用法：在 DBeaver 中【仅选中并执行本文件】，不要与 backfill 脚本一起执行。
-- 若结果面板仍显示 is_deleted 报错，那是上一次跑 backfill 的残留，可忽略并清空后重跑本脚本。
-- =============================================================================

-- Step 1：加列（单独执行这一段即可消除 “column purchase_requisition_id does not exist”）
ALTER TABLE IF EXISTS public.purchaseorderitem
  ADD COLUMN IF NOT EXISTS purchase_requisition_id character varying(36) NULL;

-- Step 2：外键（自动识别 purchaserequisition 主键列名：PurchaseRequisitionId 或 purchase_requisition_id）
DO $fk$
DECLARE
  pr_pk text;
BEGIN
  SELECT kcu.column_name
    INTO pr_pk
  FROM information_schema.table_constraints tc
  JOIN information_schema.key_column_usage kcu
    ON kcu.constraint_name = tc.constraint_name
   AND kcu.table_schema = tc.table_schema
   AND kcu.table_name = tc.table_name
  WHERE tc.table_schema = 'public'
    AND tc.table_name = 'purchaserequisition'
    AND tc.constraint_type = 'PRIMARY KEY'
  LIMIT 1;

  IF pr_pk IS NULL THEN
    RAISE EXCEPTION '未找到 public.purchaserequisition 主键列，请先确认 PR 表存在';
  END IF;

  EXECUTE 'ALTER TABLE public.purchaseorderitem DROP CONSTRAINT IF EXISTS "FK_purchaseorderitem_purchaserequisition_purchase_requisition_id"';

  EXECUTE format(
    'ALTER TABLE public.purchaseorderitem
       ADD CONSTRAINT "FK_purchaseorderitem_purchaserequisition_purchase_requisition_id"
       FOREIGN KEY (purchase_requisition_id)
       REFERENCES public.purchaserequisition (%I)
       ON DELETE RESTRICT',
    pr_pk
  );
END $fk$;

CREATE INDEX IF NOT EXISTS ix_purchaseorderitem_purchase_requisition_id
  ON public.purchaseorderitem (purchase_requisition_id)
  WHERE purchase_requisition_id IS NOT NULL;

COMMENT ON COLUMN public.purchaseorderitem.purchase_requisition_id IS
  '来源采购申请 ID；从 PR 生成 PO 时写入，用于 PR 完成度与下游展示';

DO $hist$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
  ) THEN
    INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    SELECT '20260707120000_PurchaseOrderItemPurchaseRequisitionId', '9.0.11'
    WHERE NOT EXISTS (
      SELECT 1 FROM public."__EFMigrationsHistory" h
      WHERE h."MigrationId" = '20260707120000_PurchaseOrderItemPurchaseRequisitionId'
    );
  END IF;
END $hist$;

-- 验证
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'purchaseorderitem'
  AND column_name = 'purchase_requisition_id';

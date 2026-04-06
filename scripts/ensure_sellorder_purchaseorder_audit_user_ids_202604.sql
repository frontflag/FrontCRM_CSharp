-- =============================================================================
-- sellorder / purchaseorder：create_by_user_id、modify_by_user_id（GUID，与 JWT NameIdentifier 一致）
-- 对应迁移：20260411120000_AddSellOrderPurchaseOrderAuditUserIds
-- PostgreSQL，可重复执行。
-- =============================================================================

ALTER TABLE IF EXISTS public.sellorder
    ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.sellorder
    ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

ALTER TABLE IF EXISTS public.purchaseorder
    ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.purchaseorder
    ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'create_by_user_id'
  ) THEN
    EXECUTE $cmt$COMMENT ON COLUMN public.sellorder.create_by_user_id IS '创建销售订单时的登录用户 ID（user 表主键 GUID）'$cmt$;
  END IF;
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'modify_by_user_id'
  ) THEN
    EXECUTE $cmt$COMMENT ON COLUMN public.sellorder.modify_by_user_id IS '最后修改销售订单时的登录用户 ID（GUID）'$cmt$;
  END IF;
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'create_by_user_id'
  ) THEN
    EXECUTE $cmt$COMMENT ON COLUMN public.purchaseorder.create_by_user_id IS '创建采购订单时的登录用户 ID（user 表主键 GUID）'$cmt$;
  END IF;
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'modify_by_user_id'
  ) THEN
    EXECUTE $cmt$COMMENT ON COLUMN public.purchaseorder.modify_by_user_id IS '最后修改采购订单时的登录用户 ID（GUID）'$cmt$;
  END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260411120000_AddSellOrderPurchaseOrderAuditUserIds', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260411120000_AddSellOrderPurchaseOrderAuditUserIds'
)
AND EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'create_by_user_id'
)
AND EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'create_by_user_id'
);

-- =============================================================================
-- rfq.create_by_user_id：需求「创建人」用户 ID（GUID，与 JWT NameIdentifier 一致）
-- 对应迁移：20260410120000_AddRfqCreateByUserId（CRM.Core.Models.RFQ.CreateByUserId）
-- PostgreSQL，可重复执行。
-- 推荐：优先 dotnet ef database update；本脚本用于手工对齐或应急。
-- =============================================================================

ALTER TABLE IF EXISTS public.rfq
    ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL;

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'rfq' AND column_name = 'create_by_user_id'
  ) THEN
    EXECUTE $cmt$COMMENT ON COLUMN public.rfq.create_by_user_id IS '创建需求时的登录用户 ID（user 表主键 GUID）'$cmt$;
  END IF;
END $$;

-- 若已通过本脚本加列但未写入 EF 历史，补一行，避免后续 dotnet ef 再执行同一迁移时报错
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260410120000_AddRfqCreateByUserId', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260410120000_AddRfqCreateByUserId'
)
AND EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name = 'rfq'
      AND column_name = 'create_by_user_id'
);

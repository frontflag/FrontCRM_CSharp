-- PostgreSQL：approval_record 已有 ""ItemDescription""，但 __EFMigrationsHistory 缺少
-- 20260326212000_AddApprovalRecordItemDescription 时补写，避免 42701。
-- 对应：CRM.Infrastructure/Migrations/20260326212000_AddApprovalRecordItemDescription.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326212000_AddApprovalRecordItemDescription', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326212000_AddApprovalRecordItemDescription'
)
AND EXISTS (
    SELECT 1
    FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public'
      AND c.relname = 'approval_record'
      AND NOT a.attisdropped
      AND a.attnum > 0
      AND a.attname = 'ItemDescription'
);

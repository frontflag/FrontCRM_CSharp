-- PostgreSQL：customerinfo 已有 ""AuditRemark""，但 __EFMigrationsHistory 缺少
-- 20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults 时补写，避免 42701。
-- 迁移还包含 Status 旧值映射与 DEFAULT 修改；若你从未执行过该段 SQL，补历史后请自行核对
-- customerinfo.""Status"" 与 DEFAULT 是否与现网业务一致。
-- 对应：CRM.Infrastructure/Migrations/20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults'
)
AND EXISTS (
    SELECT 1
    FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public'
      AND c.relname = 'customerinfo'
      AND NOT a.attisdropped
      AND a.attnum > 0
      AND a.attname = 'AuditRemark'
);

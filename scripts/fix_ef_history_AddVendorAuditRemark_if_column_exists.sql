-- PostgreSQL：vendorinfo 已有 ""AuditRemark""，但 __EFMigrationsHistory 缺少
-- 20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults 时补写，避免 42701。
-- 迁移还包含 vendorinfo.""Status"" 映射与 DEFAULT；若从未执行过该段，补历史后请自行核对。
-- 对应：CRM.Infrastructure/Migrations/20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults'
)
AND EXISTS (
    SELECT 1
    FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public'
      AND c.relname = 'vendorinfo'
      AND NOT a.attisdropped
      AND a.attnum > 0
      AND a.attname = 'AuditRemark'
);

-- PostgreSQL：customerinfo / vendorinfo 已有 ""EnglishOfficialName""，但 __EFMigrationsHistory 缺少
-- 20260402120000_AddCustomerVendorEnglishOfficialName 时补写，避免 42701。
-- 须两表列都存在（与本迁移 Up 一致）再插入。
-- 对应：CRM.Infrastructure/Migrations/20260402120000_AddCustomerVendorEnglishOfficialName.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260402120000_AddCustomerVendorEnglishOfficialName', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260402120000_AddCustomerVendorEnglishOfficialName'
)
AND EXISTS (
    SELECT 1 FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public' AND c.relname = 'customerinfo'
      AND NOT a.attisdropped AND a.attnum > 0 AND a.attname = 'EnglishOfficialName'
)
AND EXISTS (
    SELECT 1 FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public' AND c.relname = 'vendorinfo'
      AND NOT a.attisdropped AND a.attnum > 0 AND a.attname = 'EnglishOfficialName'
);

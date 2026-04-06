-- PostgreSQL：customerinfo 已存在 Province/City/District，但 __EFMigrationsHistory 缺少
-- 20260326103000_AddCustomerRegionColumns 时，补写历史，避免幂等迁移脚本重复 ADD COLUMN 报 42701。
-- 执行前请确认三列均已存在且含义与迁移一致（varchar(50) 可空）。

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326103000_AddCustomerRegionColumns', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326103000_AddCustomerRegionColumns'
)
AND (
    SELECT COUNT(*)::int
    FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public'
      AND c.relname = 'customerinfo'
      AND NOT a.attisdropped
      AND a.attnum > 0
      AND a.attname IN ('Province', 'City', 'District')
) = 3;

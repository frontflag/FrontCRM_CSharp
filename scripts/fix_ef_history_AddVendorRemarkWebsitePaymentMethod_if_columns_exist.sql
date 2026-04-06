-- PostgreSQL：vendorinfo 已存在 Remark/Website/PurchaserName/PaymentMethod，
-- 但 __EFMigrationsHistory 缺少 20260326120000_AddVendorRemarkWebsitePaymentMethod 时补写，避免 42701。
-- 对应迁移：CRM.Infrastructure/Migrations/20260326120000_AddVendorRemarkWebsitePaymentMethod.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326120000_AddVendorRemarkWebsitePaymentMethod', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326120000_AddVendorRemarkWebsitePaymentMethod'
)
AND (
    SELECT COUNT(*)::int
    FROM pg_catalog.pg_attribute a
    JOIN pg_catalog.pg_class c ON a.attrelid = c.oid
    JOIN pg_catalog.pg_namespace n ON c.relnamespace = n.oid
    WHERE n.nspname = 'public'
      AND c.relname = 'vendorinfo'
      AND NOT a.attisdropped
      AND a.attnum > 0
      AND a.attname IN ('Remark', 'Website', 'PurchaserName', 'PaymentMethod')
) = 4;

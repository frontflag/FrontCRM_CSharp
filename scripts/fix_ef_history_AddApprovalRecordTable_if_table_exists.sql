-- PostgreSQL：public.approval_record 已存在，但 __EFMigrationsHistory 缺少
-- 20260326203000_AddApprovalRecordTable 时补写，避免 42P07。
-- 请确认索引存在：IX_approval_record_ActionTime、IX_approval_record_BizType_BusinessId（若缺失请手工 CREATE INDEX）。
-- 对应：CRM.Infrastructure/Migrations/20260326203000_AddApprovalRecordTable.cs

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260326203000_AddApprovalRecordTable', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260326203000_AddApprovalRecordTable'
)
AND to_regclass('public.approval_record') IS NOT NULL;

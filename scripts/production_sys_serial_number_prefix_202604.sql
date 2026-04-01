-- =============================================================================
-- 生产环境：流水号前缀与新编码规则对齐（无法执行 dotnet ef 时手工执行）
-- 数据库：PostgreSQL，表 public.sys_serial_number
-- 对应迁移：20260401130000_UpdateSysSerialPrefixes202604
-- =============================================================================
-- 执行前：建议备份 sys_serial_number；在业务低峰执行。
-- 执行后：新产生的单号使用新 Prefix + 5 位 32 进制；已存在业务单号不会自动变更。
-- =============================================================================

BEGIN;

-- 1) 前缀列长度（须先执行，否则 PAY_DEL 等无法写入）
ALTER TABLE IF EXISTS sys_serial_number
    ALTER COLUMN "Prefix" TYPE character varying(16);

-- 2) 按 ModuleCode 更新前缀（与 ApplicationDbContext 种子一致）
UPDATE sys_serial_number SET "Prefix" = 'CUS'     WHERE "ModuleCode" = 'Customer';
UPDATE sys_serial_number SET "Prefix" = 'VEN'     WHERE "ModuleCode" = 'Vendor';
UPDATE sys_serial_number SET "Prefix" = 'RFQ'     WHERE "ModuleCode" = 'RFQ';
UPDATE sys_serial_number SET "Prefix" = 'QUO'     WHERE "ModuleCode" = 'Quotation';
UPDATE sys_serial_number SET "Prefix" = 'SO'      WHERE "ModuleCode" = 'SalesOrder';
UPDATE sys_serial_number SET "Prefix" = 'PO'      WHERE "ModuleCode" = 'PurchaseOrder';
UPDATE sys_serial_number SET "Prefix" = 'STI'     WHERE "ModuleCode" = 'StockIn';
UPDATE sys_serial_number SET "Prefix" = 'STO'     WHERE "ModuleCode" = 'StockOut';
UPDATE sys_serial_number SET "Prefix" = 'REC'     WHERE "ModuleCode" = 'Receipt';
UPDATE sys_serial_number SET "Prefix" = 'PAY_DEL' WHERE "ModuleCode" = 'Payment';
UPDATE sys_serial_number SET "Prefix" = 'INVI'    WHERE "ModuleCode" = 'InputInvoice';
UPDATE sys_serial_number SET "Prefix" = 'INVO'    WHERE "ModuleCode" = 'OutputInvoice';
UPDATE sys_serial_number SET "Prefix" = 'STK'     WHERE "ModuleCode" = 'Stock';
UPDATE sys_serial_number SET "Prefix" = 'POR'     WHERE "ModuleCode" = 'PurchaseRequisition';
UPDATE sys_serial_number SET "Prefix" = 'STOR'    WHERE "ModuleCode" = 'StockOutRequest';
UPDATE sys_serial_number SET "Prefix" = 'PAK'     WHERE "ModuleCode" = 'PickingTask';
UPDATE sys_serial_number SET "Prefix" = 'STIR'    WHERE "ModuleCode" = 'ArrivalNotice';
UPDATE sys_serial_number SET "Prefix" = 'QC'      WHERE "ModuleCode" = 'QcRecord';
UPDATE sys_serial_number SET "Prefix" = 'PAYR'    WHERE "ModuleCode" = 'PaymentRequest';
UPDATE sys_serial_number SET "Prefix" = 'PAY'     WHERE "ModuleCode" = 'FinancePayment';

-- 3) Prefix 唯一索引（若历史上未创建则补上；已存在则跳过）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_sys_serial_number_Prefix'
    ) THEN
        CREATE UNIQUE INDEX "IX_sys_serial_number_Prefix" ON sys_serial_number ("Prefix");
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 可选 A：业务库已清空，希望「下一张单」从约定序号起点重新计数（按需取消注释）
-- ---------------------------------------------------------------------------
-- UPDATE sys_serial_number SET "CurrentSequence" = -1 WHERE "ModuleCode" IN ('Customer', 'Vendor');
-- UPDATE sys_serial_number SET "CurrentSequence" = 2025 WHERE "ModuleCode" NOT IN ('Customer', 'Vendor');

COMMIT;

-- 验证
-- SELECT "ModuleCode", "ModuleName", "Prefix", "CurrentSequence" FROM sys_serial_number ORDER BY "Id";

-- =============================================================================
-- 若库中存在表 __EFMigrationsHistory，且以后仍可能用 dotnet ef 连接同一库：
-- 可手工插入一条，避免日后 ef 重复执行同一迁移（ProductVersion 按你们部署的 EF 版本填写）
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20260401130000_UpdateSysSerialPrefixes202604', '9.0.11')
-- ON CONFLICT ("MigrationId") DO NOTHING;
-- =============================================================================

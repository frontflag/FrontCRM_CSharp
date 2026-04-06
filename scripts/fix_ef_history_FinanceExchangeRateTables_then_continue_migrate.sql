-- 场景：库里已有 public.financeexchangeratesetting（手工或其它脚本建过），
-- 但 __EFMigrationsHistory 里没有 20260403120000_FinanceExchangeRateTables，
-- 导致 dotnet ef / CRM.DbMigrator 在执行该迁移时 42P07 失败，后续迁移（含 sell_order_item_code 等）都不会跑。
--
-- 用法：在目标库（如 FrontCRM）用 psql / pgAdmin 执行本脚本，然后重新运行：
--   dotnet run --project CRM.DbMigrator -- --apply --force-dev
--
-- 说明：若该迁移从未部分执行过，通常只需「补历史」一行；若当时在第一句之后中断，
-- 可能缺少 financeexchangeratechangelog，下面 IF NOT EXISTS 会补齐。

-- 1) 变更日志表（与 Migration 20260403120000 一致）
CREATE TABLE IF NOT EXISTS public.financeexchangeratechangelog (
    "FinanceExchangeRateChangeLogId" character varying(36) NOT NULL,
    "UsdToCny" numeric(12,4) NOT NULL,
    "UsdToHkd" numeric(12,4) NOT NULL,
    "UsdToEur" numeric(12,4) NOT NULL,
    "ChangeUserId" character varying(36),
    "ChangeUserName" character varying(100),
    "ChangeSummary" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_financeexchangeratechangelog" PRIMARY KEY ("FinanceExchangeRateChangeLogId")
);

CREATE INDEX IF NOT EXISTS "IX_financeexchangeratechangelog_CreateTime"
    ON public.financeexchangeratechangelog ("CreateTime");

-- 2) 默认汇率行（若 setting 表已有主键行则跳过）
INSERT INTO public.financeexchangeratesetting (
    "FinanceExchangeRateSettingId", "UsdToCny", "UsdToHkd", "UsdToEur",
    "EditorUserId", "EditorUserName", "CreateTime", "ModifyTime")
VALUES (
    '00000000-0000-4000-8000-0000000000E1',
    6.9194, 7.8367, 0.8725,
    NULL, NULL,
    NOW() AT TIME ZONE 'UTC', NULL)
ON CONFLICT ("FinanceExchangeRateSettingId") DO NOTHING;

-- 3) 标记该迁移已应用（请与项目 EF Core 版本大致一致，仅作记录用）
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260403120000_FinanceExchangeRateTables', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM public."__EFMigrationsHistory"
    WHERE "MigrationId" = '20260403120000_FinanceExchangeRateTables');

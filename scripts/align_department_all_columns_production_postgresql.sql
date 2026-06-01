-- 生产环境登录 42703（如 FinanceDataAccess 不存在）时执行本脚本
-- 补齐 sys_department 部门权限相关列（与近期 API 版本一致）
-- 执行后重启 CRM.API，用户重新登录。

BEGIN;

-- 1) 销售/采购 只读|读写
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "SaleDataAccess" smallint NOT NULL DEFAULT 0;
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "PurchaseDataAccess" smallint NOT NULL DEFAULT 0;

-- 2) 物流数据范围
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "LogisticsDataScope" smallint NOT NULL DEFAULT 0;
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "LogisticsDataAccess" smallint NOT NULL DEFAULT 0;

-- 3) 财务数据范围（截图报错 FinanceDataAccess 缺列）
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "FinanceDataScope" smallint NOT NULL DEFAULT 0;
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "FinanceDataAccess" smallint NOT NULL DEFAULT 0;

-- 4) 隐藏客户/供应商管理
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "HideCustomerManagement" boolean NOT NULL DEFAULT false;
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "HideVendorManagement" boolean NOT NULL DEFAULT false;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
  ('20260526120000_DepartmentDataAccessMode', '9.0.0'),
  ('20260526140000_DepartmentLogisticsDataScope', '9.0.0'),
  ('20260526160000_DepartmentFinanceDataScope', '9.0.0'),
  ('20260526170000_DepartmentHideCustomerVendorManagement', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- 校验（应返回 8 行，column_name 如下）
-- SELECT column_name, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'sys_department'
--   AND column_name IN (
--     'SaleDataAccess','PurchaseDataAccess',
--     'LogisticsDataScope','LogisticsDataAccess',
--     'FinanceDataScope','FinanceDataAccess',
--     'HideCustomerManagement','HideVendorManagement'
--   )
-- ORDER BY column_name;

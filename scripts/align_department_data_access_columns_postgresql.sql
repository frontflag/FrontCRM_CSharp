-- 部门表：销售/采购数据「只读 vs 读写」（与迁移 20260526120000_DepartmentDataAccessMode 一致）
-- 登录报错 42703: 字段 s.PurchaseDataAccess 不存在 时执行本脚本后重启 API。

BEGIN;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "SaleDataAccess" smallint NOT NULL DEFAULT 0;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "PurchaseDataAccess" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sys_department."SaleDataAccess" IS '销售数据访问：0读写 1只读（与 SaleDataScope 独立）';
COMMENT ON COLUMN public.sys_department."PurchaseDataAccess" IS '采购数据访问：0读写 1只读（与 PurchaseDataScope 独立）';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260526120000_DepartmentDataAccessMode', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

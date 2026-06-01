-- 部门表：隐藏客户管理 / 隐藏供应商管理（与迁移 20260526170000_DepartmentHideCustomerVendorManagement 一致）

BEGIN;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "HideCustomerManagement" boolean NOT NULL DEFAULT false;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "HideVendorManagement" boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.sys_department."HideCustomerManagement" IS '隐藏客户管理菜单并拦截客户模块（与 SaleDataScope 独立）';
COMMENT ON COLUMN public.sys_department."HideVendorManagement" IS '隐藏供应商管理菜单并拦截供应商模块（与 PurchaseDataScope 独立）';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260526170000_DepartmentHideCustomerVendorManagement', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- 关系表 / 核销流水软删：补 is_deleted，RBAC 唯一改为「未删除」过滤唯一。
-- 幂等，可重复执行。对应 EF：20260905120000_SoftDeleteJunctionsAndWriteOffs
-- 未执行时登录会 500：字段 sys_user_role.is_deleted 不存在。

ALTER TABLE public.finance_receivable_write_off
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.finance_receivable_write_off.is_deleted IS '软删除：反核销后为 true，默认查询排除';

ALTER TABLE public.freight_forwarder_company_bank
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.freight_forwarder_company_bank.is_deleted IS '软删除；默认查询排除';

ALTER TABLE public.company_bankinfo
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.company_bankinfo.is_deleted IS '软删除；公司银行整表保存时对缺席行标删，同 Id 可复活';

ALTER TABLE public.sys_user_role
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.sys_user_role.is_deleted IS '软删除；分配角色时多余关系标删，同用户+角色可复活';

ALTER TABLE public.sys_role_permission
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.sys_role_permission.is_deleted IS '软删除；分配权限时多余关系标删，同角色+权限可复活';

ALTER TABLE public.sys_user_department
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.sys_user_department.is_deleted IS '软删除；分配部门时多余关系标删，同用户+部门可复活';

DROP INDEX IF EXISTS public."IX_sys_user_role_UserId_RoleId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_user_role_UserId_RoleId_alive"
  ON public.sys_user_role ("UserId", "RoleId")
  WHERE is_deleted = false;

DROP INDEX IF EXISTS public."IX_sys_role_permission_RoleId_PermissionId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_role_permission_RoleId_PermissionId_alive"
  ON public.sys_role_permission ("RoleId", "PermissionId")
  WHERE is_deleted = false;

DROP INDEX IF EXISTS public."IX_sys_user_department_UserId_DepartmentId";
CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_user_department_UserId_DepartmentId_alive"
  ON public.sys_user_department ("UserId", "DepartmentId")
  WHERE is_deleted = false;

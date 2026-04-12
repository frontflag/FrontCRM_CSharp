-- 为销售职员角色绑定 rfq.write / rfq.create（编辑需求、新建需求 POST 与路由 rfqs/create）。
-- 部门员工 DEPT_EMPLOYEE 不再在此脚本中批量授予：销售部员工由主部门 IdentityType=1 时 RbacService 合并权限；
-- 采购部员工由采购主部门合并 rfq.read+rfq.write（不含 rfq.create），避免采购误拿「新建需求」。
-- 幂等：可重复执行。
BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'sales_operator'
  AND p."PermissionCode" IN ('rfq.write', 'rfq.create')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- 执行后请让用户重新登录，JWT / permission-summary 才会带上新权限。
--
-- 若销售员工仍无新建需求：确认主部门为销售部且 IdentityType=1，或为其绑定 sales_operator。

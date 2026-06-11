-- 补全字段级权限 sales.amount.read（销售金额/单价查看）。
-- 适用：早期环境未执行 apply_migrations.sql / seed_initial_rbac_admin.sql 中对应 INSERT 时。
-- 幂等：可重复执行。
BEGIN;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT '30000000-0000-4000-8000-000000000011', 'sales.amount.read', '销售金额-查看', 'api', 'sales', 'amount.read', 1, NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission sp WHERE sp."PermissionCode" = 'sales.amount.read'
);

-- SYS_ADMIN：全部权限（若已存在则跳过）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND p."PermissionCode" = 'sales.amount.read'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 销售职员扩展角色 sales_operator
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'sales_operator'
  AND p."PermissionCode" = 'sales.amount.read'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 部门组织角色（与 seed_dept_org_roles.sql DEPT_EMPLOYEE 只读集合一致）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('DEPT_EMPLOYEE', 'DEPT_MANAGER', 'DEPT_DIRECTOR')
  AND p."PermissionCode" = 'sales.amount.read'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- 执行后请相关用户重新登录，permission-summary / JWT 才会带上新权限。
-- 校验：
-- SELECT "PermissionCode", "PermissionName", "Resource", "Status" FROM sys_permission WHERE "PermissionCode" = 'sales.amount.read';

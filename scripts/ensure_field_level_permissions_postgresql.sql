-- 补全字段级权限（客户/供应商敏感信息、销售/采购金额查看）。
-- 适用：早期环境未执行 seed_initial_rbac_admin.sql / apply_migrations.sql 对应段落时。
-- 幂等：可重复执行。
BEGIN;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT v."PermissionId", v."PermissionCode", v."PermissionName", v."PermissionType", v."Resource", v."Action", 1, NOW()
FROM (VALUES
    ('30000000-0000-4000-8000-000000000003', 'customer.info.read',  '客户敏感信息-查看', 'api', 'customer',  'info.read'),
    ('30000000-0000-4000-8000-000000000006', 'vendor.info.read',    '供应商敏感信息-查看', 'api', 'vendor',    'info.read'),
    ('30000000-0000-4000-8000-000000000011', 'sales.amount.read',   '销售金额-查看',   'api', 'sales',     'amount.read'),
    ('30000000-0000-4000-8000-000000000014', 'purchase.amount.read','采购金额-查看',   'api', 'purchase',  'amount.read')
) AS v("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action")
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission sp WHERE sp."PermissionCode" = v."PermissionCode"
);

-- SYS_ADMIN：绑定上述全部权限
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND p."PermissionCode" IN (
    'customer.info.read', 'vendor.info.read', 'sales.amount.read', 'purchase.amount.read'
  )
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- sales_operator：销售侧字段权限
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'sales_operator'
  AND p."PermissionCode" IN ('customer.info.read', 'sales.amount.read')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- purchase_buyer / PURCHASER 等采购业务角色：采购侧字段权限（角色不存在则跳过）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('purchase_buyer', 'PURCHASER')
  AND p."PermissionCode" IN ('vendor.info.read', 'purchase.amount.read')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 部门组织角色 DEPT_EMPLOYEE 只读集合（与 seed_dept_org_roles.sql 一致）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'DEPT_EMPLOYEE'
  AND p."PermissionCode" IN (
    'customer.info.read', 'vendor.info.read', 'sales.amount.read', 'purchase.amount.read'
  )
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 总监 / 经理：除 rbac.manage 外通常已有全权限；此处仅补缺字段权限
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('DEPT_MANAGER', 'DEPT_DIRECTOR')
  AND p."PermissionCode" IN (
    'customer.info.read', 'vendor.info.read', 'sales.amount.read', 'purchase.amount.read'
  )
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- 执行后请相关用户重新登录。
-- 校验：
-- SELECT "PermissionCode", "PermissionName", "Resource", "Status"
-- FROM sys_permission
-- WHERE "PermissionCode" IN ('sales.amount.read','purchase.amount.read','customer.info.read','vendor.info.read')
-- ORDER BY 1;

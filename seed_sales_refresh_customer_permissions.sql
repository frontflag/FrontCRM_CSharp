-- 销售参数「刷新客户」独立权限（幂等）
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime") VALUES
('31000000-0000-4000-8000-00000000001a', 'system.params.sales.refresh-customer.read', '系统-销售参数-刷新客户-查看', 'api', 'system.params.sales.refresh-customer', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000001b', 'system.params.sales.refresh-customer.write', '系统-销售参数-刷新客户-维护', 'api', 'system.params.sales.refresh-customer', 'write', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND p."PermissionCode" IN (
    'system.params.sales.refresh-customer.read',
    'system.params.sales.refresh-customer.write'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

-- 已有销售参数权限的角色：兼容升级自动获得对应刷新客户权限（可在角色编辑中单独去掉）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, rp_exist."RoleId", p_new."PermissionId", NOW()
FROM sys_role_permission rp_exist
JOIN sys_permission p_old ON p_old."PermissionId" = rp_exist."PermissionId"
  AND p_old."PermissionCode" = 'system.params.sales.read'
JOIN sys_permission p_new ON p_new."PermissionCode" = 'system.params.sales.refresh-customer.read'
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role_permission rp
  WHERE rp."RoleId" = rp_exist."RoleId" AND rp."PermissionId" = p_new."PermissionId"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, rp_exist."RoleId", p_new."PermissionId", NOW()
FROM sys_role_permission rp_exist
JOIN sys_permission p_old ON p_old."PermissionId" = rp_exist."PermissionId"
  AND p_old."PermissionCode" = 'system.params.sales.write'
JOIN sys_permission p_new ON p_new."PermissionCode" = 'system.params.sales.refresh-customer.write'
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role_permission rp
  WHERE rp."RoleId" = rp_exist."RoleId" AND rp."PermissionId" = p_new."PermissionId"
);

SELECT "PermissionCode" FROM sys_permission
WHERE "PermissionCode" LIKE 'system.params.sales.refresh-customer.%'
ORDER BY 1;

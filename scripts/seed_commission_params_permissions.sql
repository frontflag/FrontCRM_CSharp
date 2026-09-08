-- 提成参数权限（幂等，按 PermissionCode 跳过；不复用客户报价单已占用的 030～032）。
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT v."PermissionId", v."PermissionCode", v."PermissionName", v."PermissionType", v."Resource", v."Action", v."Status", NOW()
FROM (VALUES
  ('31000000-0000-4000-8000-0000000000a1', 'system.params.commission.read', '系统-提成参数-查看', 'api', 'system.params.commission', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a2', 'system.params.commission.write', '系统-提成参数-维护', 'api', 'system.params.commission', 'write', 1),
  ('31000000-0000-4000-8000-0000000000a3', 'system.params.commission.sales.read', '系统-提成参数-业务员系数-查看', 'api', 'system.params.commission.sales', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a4', 'system.params.commission.sales.write', '系统-提成参数-业务员系数-维护', 'api', 'system.params.commission.sales', 'write', 1),
  ('31000000-0000-4000-8000-0000000000a5', 'system.params.commission.purchase.read', '系统-提成参数-采购员系数-查看', 'api', 'system.params.commission.purchase', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a6', 'system.params.commission.purchase.write', '系统-提成参数-采购员系数-维护', 'api', 'system.params.commission.purchase', 'write', 1)
) AS v("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status")
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p."PermissionCode" = v."PermissionCode"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER')
  AND p."PermissionCode" LIKE 'system.params.commission%'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, rp_exist."RoleId", p_new."PermissionId", NOW()
FROM sys_role_permission rp_exist
JOIN sys_permission p_old ON p_old."PermissionId" = rp_exist."PermissionId"
JOIN sys_permission p_new ON (
  (p_old."PermissionCode" = 'system.params.finance.read' AND p_new."PermissionCode" IN (
    'system.params.commission.read',
    'system.params.commission.sales.read',
    'system.params.commission.purchase.read'
  ))
  OR (p_old."PermissionCode" = 'system.params.finance.write' AND p_new."PermissionCode" IN (
    'system.params.commission.write',
    'system.params.commission.sales.write',
    'system.params.commission.purchase.write'
  ))
)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role_permission rp
  WHERE rp."RoleId" = rp_exist."RoleId" AND rp."PermissionId" = p_new."PermissionId"
);

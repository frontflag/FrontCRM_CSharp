-- 物流看板只读权限：授予已有库存明细查看权限的角色
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    'a0000000-0000-4000-8000-000000000103',
    'analytics-logistics.read',
    '物流看板-查看',
    'api',
    'analytics',
    'read',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'analytics-logistics.read'
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE p."PermissionCode" = 'analytics-logistics.read'
  AND r."Status" = 1
  AND EXISTS (
      SELECT 1 FROM sys_role_permission rp2
      JOIN sys_permission p2 ON p2."PermissionId" = rp2."PermissionId"
      WHERE rp2."RoleId" = r."RoleId" AND p2."PermissionCode" = 'inventory.read'
  )
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

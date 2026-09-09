-- 提成主菜单入口权限（幂等）
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT v."PermissionId", v."PermissionCode", v."PermissionName", v."PermissionType", v."Resource", v."Action", v."Status", NOW()
FROM (VALUES
  ('c1000000-0000-4000-8000-000000000101', 'commission-estimated-sales.read', '业务预计提成-查看', 'api', 'commission-estimated-sales', 'read', 1),
  ('c1000000-0000-4000-8000-000000000102', 'commission-estimated-purchase.read', '采购预计提成-查看', 'api', 'commission-estimated-purchase', 'read', 1),
  ('c1000000-0000-4000-8000-000000000103', 'commission-official-sales.read', '业务正式提成-查看', 'api', 'commission-official-sales', 'read', 1),
  ('c1000000-0000-4000-8000-000000000104', 'commission-official-purchase.read', '采购正式提成-查看', 'api', 'commission-official-purchase', 'read', 1)
) AS v("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status")
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p."PermissionCode" = v."PermissionCode"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER')
  AND p."PermissionCode" IN (
    'commission-estimated-sales.read',
    'commission-estimated-purchase.read',
    'commission-official-sales.read',
    'commission-official-purchase.read'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

DELETE FROM sys_role_permission rp
USING sys_permission p, sys_role r
WHERE rp."PermissionId" = p."PermissionId"
  AND rp."RoleId" = r."RoleId"
  AND p."PermissionCode" IN (
    'commission-estimated-sales.read',
    'commission-estimated-purchase.read',
    'commission-official-sales.read',
    'commission-official-purchase.read'
  )
  AND r."RoleCode" NOT IN ('SYS_ADMIN', 'SYS_MANAGER');

SELECT p."PermissionCode", p."PermissionName", r."RoleCode"
FROM sys_permission p
JOIN sys_role_permission rp ON rp."PermissionId" = p."PermissionId"
JOIN sys_role r ON r."RoleId" = rp."RoleId"
WHERE p."PermissionCode" LIKE 'commission-%'
ORDER BY p."PermissionCode", r."RoleCode";

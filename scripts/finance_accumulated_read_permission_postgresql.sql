-- 库存滚存只读权限：授予已有 finance-receipt.read 或 finance-payment.read 的角色

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    'a0000000-0000-4000-8000-000000000105',
    'finance-accumulated.read',
    '库存滚存-查看',
    'api',
    'finance',
    'read',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'finance-accumulated.read'
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE p."PermissionCode" = 'finance-accumulated.read'
  AND r."Status" = 1
  AND EXISTS (
      SELECT 1 FROM sys_role_permission rp2
      JOIN sys_permission p2 ON p2."PermissionId" = rp2."PermissionId"
      WHERE rp2."RoleId" = r."RoleId"
        AND p2."PermissionCode" IN (
            'finance-payment.read',
            'finance-receipt.read'
        )
  )
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

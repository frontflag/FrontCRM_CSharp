-- 采购订单更换供应商权限（幂等）
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    gen_random_uuid()::text,
    'purchase-order.change-vendor',
    '更换采购订单供应商',
    'api',
    'purchase-order',
    'change-vendor',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'purchase-order.change-vendor'
);

-- 系统管理员拥有该权限（若已存在则跳过）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'purchase-order.change-vendor'
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

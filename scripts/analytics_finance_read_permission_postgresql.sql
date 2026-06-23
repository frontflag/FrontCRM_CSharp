-- 财务看板只读权限：授予已有任一 finance-*.read 或销/采订单 read 的角色

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    'a0000000-0000-4000-8000-000000000104',
    'analytics-finance.read',
    '财务看板-查看',
    'api',
    'analytics',
    'read',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'analytics-finance.read'
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE p."PermissionCode" = 'analytics-finance.read'
  AND r."Status" = 1
  AND EXISTS (
      SELECT 1 FROM sys_role_permission rp2
      JOIN sys_permission p2 ON p2."PermissionId" = rp2."PermissionId"
      WHERE rp2."RoleId" = r."RoleId"
        AND p2."PermissionCode" IN (
            'finance-payment.read',
            'finance-receipt.read',
            'finance-purchase-invoice.read',
            'finance-sell-invoice.read',
            'sales-order.read',
            'purchase-order.read'
        )
  )
  AND NOT EXISTS (
      SELECT 1 FROM sys_role_permission rp
      WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

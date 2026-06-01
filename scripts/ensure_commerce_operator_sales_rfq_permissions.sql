-- =============================================================================
-- 商务部职员角色 commerce_operator：补全销售侧写权限
--   新建需求 rfq.create / 销售订单 sales-order.write / 申请出库通知（依赖 SO 写权限）
--
-- 说明：
--   1) 角色表补绑；主部门 IdentityType=4 用户登录时 RbacService 亦会合并相同功能码
--   2) 商务部用户执行后须重新登录以刷新 permission-summary
--
-- 执行：psql "postgresql://..." -v ON_ERROR_STOP=1 -f scripts/ensure_commerce_operator_sales_rfq_permissions.sql
-- =============================================================================

BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN (
    'customer.read', 'customer.write',
    'rfq.read', 'rfq.write', 'rfq.create',
    'sales-order.read', 'sales-order.write',
    'draft.write')
  AND p."Status" = 1
WHERE r."RoleCode" = 'commerce_operator'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

SELECT r."RoleCode",
       array_agg(p."PermissionCode" ORDER BY p."PermissionCode") AS permissions
FROM sys_role r
JOIN sys_role_permission rp ON rp."RoleId" = r."RoleId"
JOIN sys_permission p ON p."PermissionId" = rp."PermissionId" AND p."Status" = 1
WHERE r."RoleCode" = 'commerce_operator'
GROUP BY r."RoleCode";

COMMIT;

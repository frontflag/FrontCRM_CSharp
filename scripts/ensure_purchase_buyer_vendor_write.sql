-- 采购员角色 purchase_buyer 需 vendor.write 才能进入「新建供应商」路由与调用创建 API（与 routes VendorCreate meta 一致）。
-- 幂等：可重复执行。
BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'purchase_buyer'
  AND p."PermissionCode" = 'vendor.write'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- 采购员 purchase_buyer：菜单「采购申请」与列表路由原依赖 purchase-requisition.read；
-- 与 purchase-order.read 对齐，此处为角色显式授予 PR 读写（幂等）。
BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'purchase_buyer'
  AND p."PermissionCode" IN ('purchase-requisition.read', 'purchase-requisition.write')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- purchase_buyer：创建采购订单 API 需 purchase-order.write 或（purchase-requisition.write + purchase-order.read）
BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'purchase_buyer'
  AND p."PermissionCode" IN ('purchase-order.read', 'purchase-order.write')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- ---------------------------------------------------------------------------
-- 若采购员账号仍无法「新建供应商」：登录后 JWT 需含 purchase_buyer + vendor.write。
-- 常见原因：曾在「员工管理」保存过用户，旧版前端会把 purchase_buyer 从 sys_user_role 删掉。
-- 部署新版前后端后，可按用户名补回角色绑定（将 YOUR_USER 改为登录名）：
--
-- INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
-- SELECT gen_random_uuid()::text, u."UserId", r."RoleId", NOW()
-- FROM "user" u
-- CROSS JOIN sys_role r
-- WHERE u."UserName" = 'YOUR_USER' AND r."RoleCode" = 'purchase_buyer'
--   AND NOT EXISTS (
--     SELECT 1 FROM sys_user_role x WHERE x."UserId" = u."UserId" AND x."RoleId" = r."RoleId");
-- 然后用户重新登录。

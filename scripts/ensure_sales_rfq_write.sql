-- 为销售相关角色绑定 rfq.write，用于「新建需求 / 编辑需求」路由与 RFQsController 写接口。
-- 角色说明：
--   sales_operator — 销售职员（常与销售部账号一并绑定）
--   DEPT_EMPLOYEE — 部门员工（演示/种子中销售员工常用；若需限制仅销售部，请改条件或仅用 sales_operator）
-- 幂等：可重复执行。
--
-- 补充：主部门为销售部（IdentityType=1）时，RbacService 会在 permission-summary 中合并 rfq.read + rfq.write；
-- 库内角色仍建议绑定完整，便于员工管理、审计与非销售主部门账号。
BEGIN;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('DEPT_EMPLOYEE', 'sales_operator')
  AND p."PermissionCode" = 'rfq.write'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- ---------------------------------------------------------------------------
-- 执行后请让用户重新登录，JWT / permission-summary 才会带上 rfq.write。
--
-- 若仍无权限：检查该用户 sys_user_role 是否包含上述角色之一（员工编辑页保存不当可能丢角色）。
-- 将 YOUR_USER 改为登录名后执行：
--
-- INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
-- SELECT gen_random_uuid()::text, u."UserId", r."RoleId", NOW()
-- FROM "user" u
-- CROSS JOIN sys_role r
-- WHERE u."UserName" = 'YOUR_USER' AND r."RoleCode" IN ('DEPT_EMPLOYEE', 'sales_operator')
--   AND NOT EXISTS (
--     SELECT 1 FROM sys_user_role x WHERE x."UserId" = u."UserId" AND x."RoleId" = r."RoleId");
-- 然后重新登录。

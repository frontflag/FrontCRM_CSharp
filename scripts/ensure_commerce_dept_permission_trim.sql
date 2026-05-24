-- =============================================================================
-- 商务部（主部门 IdentityType=4）员工权限收紧
-- 去除：供应商只读、采购订单只读、草稿只读、进项发票只读
--
-- 说明：
--   1) commerce_operator 角色：去掉 draft.read（保留 draft.write 若需维护草稿）
--   2) DEPT_EMPLOYEE / DEPT_MANAGER / DEPT_DIRECTOR / biz_all 为全员或总监经理共用，
--      不能全局删权限；运行时由 RbacService 对 IdentityType=4 剥离（需部署含该逻辑的 API）。
--   3) 商务部用户执行本脚本后须重新登录以刷新 permission-summary。
--
-- 执行：psql "postgresql://..." -v ON_ERROR_STOP=1 -f scripts/ensure_commerce_dept_permission_trim.sql
-- =============================================================================

BEGIN;

-- ---------- 1) commerce_operator：去掉草稿只读 ----------
DELETE FROM sys_role_permission rp
USING sys_role r, sys_permission p
WHERE rp."RoleId" = r."RoleId"
  AND rp."PermissionId" = p."PermissionId"
  AND r."RoleCode" = 'commerce_operator'
  AND p."PermissionCode" = 'draft.read';

-- ---------- 2) 校验：商务部账号仍绑定的上述权限（角色表层面，不含运行时剥离）----------
-- 期望 commerce 主部门用户：无 vendor.read / purchase-order.read / draft.read / finance-purchase-invoice.read
-- （若总监/经理带 biz_all 或 DEPT_MANAGER 全权限，库内仍可能有行；登录后由 RbacService 剥离）
SELECT u."UserName",
       d."DepartmentName",
       d."IdentityType",
       array_agg(DISTINCT p."PermissionCode" ORDER BY p."PermissionCode") AS held_codes
FROM "user" u
JOIN sys_user_department ud ON ud."UserId" = u."UserId" AND ud."IsPrimary" = TRUE
JOIN sys_department d ON d."DepartmentId" = ud."DepartmentId"
JOIN sys_user_role ur ON ur."UserId" = u."UserId"
JOIN sys_role_permission rp ON rp."RoleId" = ur."RoleId"
JOIN sys_permission p ON p."PermissionId" = rp."PermissionId" AND p."Status" = 1
WHERE d."DepartmentName" = '商务部'
  AND p."PermissionCode" IN (
      'vendor.read', 'vendor.info.read',
      'purchase-order.read', 'purchase.amount.read',
      'draft.read',
      'finance-purchase-invoice.read')
GROUP BY u."UserName", d."DepartmentName", d."IdentityType"
ORDER BY u."UserName";

COMMIT;

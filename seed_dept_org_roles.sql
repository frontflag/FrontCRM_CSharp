-- =============================================================================
-- 部门组织角色编码标准（与数据权限 DataPermissionService 对齐）
--   DEPT_DIRECTOR  部门总监 — 可看下属经理 + 员工
--   DEPT_MANAGER   部门经理 — 可看下属员工
--   DEPT_EMPLOYEE  部门员工 — 仅本人
--
-- 前置：已存在 sys_role / sys_permission / sys_role_permission（如 apply_migrations.sql）。
-- 可重复执行：INSERT 使用 WHERE NOT EXISTS；历史 DEPT_STAFF 重命名为 DEPT_EMPLOYEE。
-- =============================================================================

BEGIN;

-- ---------- 1) 角色行：固定 RoleId 与 seed_dept_demo_users 中经理/员工 ID 兼容 ----------
INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime")
SELECT '21000000-0000-4000-8000-000000000001', 'DEPT_MANAGER', '部门经理', '部门经理（部门内二级：可查看下属员工）', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_MANAGER');

-- 若同时存在 STAFF 与 EMPLOYEE：先把用户迁到 EMPLOYEE 行再删 STAFF（避免 RoleCode 唯一冲突）
UPDATE sys_user_role ur
SET "RoleId" = (SELECT r2."RoleId" FROM sys_role r2 WHERE r2."RoleCode" = 'DEPT_EMPLOYEE' LIMIT 1)
WHERE EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_STAFF')
  AND EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_EMPLOYEE')
  AND ur."RoleId" IN (SELECT r1."RoleId" FROM sys_role r1 WHERE r1."RoleCode" = 'DEPT_STAFF');

DELETE FROM sys_role_permission
WHERE "RoleId" IN (SELECT "RoleId" FROM sys_role WHERE "RoleCode" = 'DEPT_STAFF')
  AND EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_EMPLOYEE');

DELETE FROM sys_role
WHERE "RoleCode" = 'DEPT_STAFF'
  AND EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_EMPLOYEE');

-- 仅剩 STAFF 时：重命名为 DEPT_EMPLOYEE（保留原 RoleId）
UPDATE sys_role
SET "RoleCode" = 'DEPT_EMPLOYEE',
    "RoleName" = '部门员工',
    "Description" = '部门员工（部门内一级：仅本人；原 DEPT_STAFF 已统一为此编码）'
WHERE "RoleCode" = 'DEPT_STAFF';

INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime")
SELECT '21000000-0000-4000-8000-000000000002', 'DEPT_EMPLOYEE', '部门员工', '部门员工（部门内一级：仅本人）', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_EMPLOYEE');

INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime")
SELECT '21000000-0000-4000-8000-000000000003', 'DEPT_DIRECTOR', '部门总监', '部门总监（部门内三级：可查看下属经理与员工）', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_role WHERE "RoleCode" = 'DEPT_DIRECTOR');

-- ---------- 2) 权限：总监 / 经理 = 除 rbac.manage 外全部；员工 = 只读集合 ----------
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('DEPT_MANAGER', 'DEPT_DIRECTOR')
  AND p."PermissionCode" <> 'rbac.manage'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'DEPT_EMPLOYEE'
  AND p."PermissionCode" IN (
    'customer.read', 'customer.info.read',
    'vendor.read', 'vendor.info.read',
    'rfq.read',
    'sales-order.read', 'sales-order.write', 'sales.amount.read',
    'purchase-order.read', 'purchase.amount.read',
    'draft.read',
    'finance-receipt.read', 'finance-payment.read',
    'finance-sell-invoice.read', 'finance-purchase-invoice.read'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

COMMIT;

-- 校验
SELECT "RoleCode", "RoleName", "RoleId" FROM sys_role
WHERE "RoleCode" IN ('DEPT_DIRECTOR', 'DEPT_MANAGER', 'DEPT_EMPLOYEE')
ORDER BY "RoleCode";

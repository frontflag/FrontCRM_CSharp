-- 财务侧栏菜单依赖 permissionCodes 中含 finance-*（见 AppLayout.vue）。
-- 需绑定对象：
--   • finance_operator、biz_all（业务角色）
--   • DEPT_DIRECTOR、DEPT_MANAGER（组织角色：财务部总监/经理常仅用此角色，不用 finance_operator）
-- 若财务权限行是后加的，seed_dept_org_roles 里「总监/经理=全部权限」不会在历史上自动补绑，须执行本脚本或重跑该段 CROSS JOIN。
BEGIN;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime") VALUES
('p0000000-0000-4000-8000-000000000013', 'finance-receipt.read', '收款-查看', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000014', 'finance-receipt.write', '收款-维护', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000015', 'finance-payment.read', '付款-查看', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000016', 'finance-payment.write', '付款-维护', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000017', 'finance-sell-invoice.read', '销项发票-查看', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000018', 'finance-sell-invoice.write', '销项发票-维护', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-000000000019', 'finance-purchase-invoice.read', '进项发票-查看', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('p0000000-0000-4000-8000-00000000001a', 'finance-purchase-invoice.write', '进项发票-维护', 'api', NULL, NULL, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z')
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'finance_operator'
  AND p."PermissionCode" IN (
    'finance-receipt.read','finance-receipt.write',
    'finance-payment.read','finance-payment.write',
    'finance-sell-invoice.read','finance-sell-invoice.write',
    'finance-purchase-invoice.read','finance-purchase-invoice.write')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId");

-- 业务全权限角色通常也应能进财务页（若使用 biz_all）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'biz_all'
  AND p."PermissionCode" IN (
    'finance-receipt.read','finance-receipt.write',
    'finance-payment.read','finance-payment.write',
    'finance-sell-invoice.read','finance-sell-invoice.write',
    'finance-purchase-invoice.read','finance-purchase-invoice.write')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId");

-- 部门总监 / 部门经理（与 Kerry 等「财务部 + DEPT_DIRECTOR」账号一致）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('DEPT_DIRECTOR', 'DEPT_MANAGER')
  AND p."PermissionCode" IN (
    'finance-receipt.read','finance-receipt.write',
    'finance-payment.read','finance-payment.write',
    'finance-sell-invoice.read','finance-sell-invoice.write',
    'finance-purchase-invoice.read','finance-purchase-invoice.write')
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId");

-- 财务部仅挂 DEPT_EMPLOYEE 的账号默认只有 finance-payment.read，无法保存/付款完成（API 需 finance-payment.write）。
-- 为其补挂 finance_operator（须存在该角色；与 seed_departments_and_demo_employees 中 RoleId 一致）。
INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime") VALUES
('r0000000-0000-4000-8000-000000000012', 'finance_operator', '财务职员权限', '财务部门员工（付款/收款维护）', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z')
ON CONFLICT ("RoleCode") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", rf."RoleId", NOW()
FROM "user" u
JOIN sys_user_department ud ON ud."UserId" = u."UserId" AND ud."IsPrimary" IS TRUE
JOIN sys_department d ON d."DepartmentId" = ud."DepartmentId"
CROSS JOIN sys_role rf
WHERE rf."RoleCode" = 'finance_operator'
  AND (
    d."IdentityType" = 5
    OR COALESCE(d."DepartmentName", '') LIKE '%财务%'
    OR d."DepartmentName" ILIKE '%Finance%'
    OR d."DepartmentName" ILIKE '%Accounting%'
  )
  AND EXISTS (
    SELECT 1 FROM sys_user_role ur
    JOIN sys_role re ON re."RoleId" = ur."RoleId"
    WHERE ur."UserId" = u."UserId" AND re."RoleCode" = 'DEPT_EMPLOYEE'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_user_role ur2
    JOIN sys_role r2 ON r2."RoleId" = ur2."RoleId"
    WHERE ur2."UserId" = u."UserId" AND r2."RoleCode" = 'finance_operator'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_user_role ur3
    JOIN sys_role r3 ON r3."RoleId" = ur3."RoleId"
    WHERE ur3."UserId" = u."UserId" AND r3."RoleCode" IN ('DEPT_MANAGER', 'DEPT_DIRECTOR', 'SYS_ADMIN')
  );

COMMIT;

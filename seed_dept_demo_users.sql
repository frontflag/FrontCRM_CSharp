-- =============================================================================
-- 演示数据：销售/采购/物流/财务 四部门，每部门经理 + 员工各 1 名
-- 密码均为：123456（BCrypt，与 CRM.API AuthService 一致）
--
-- 前置：已执行 create_full_database.sql 与 seed_initial_rbac_admin.sql（需存在「总公司」与 sys_permission）。
-- 可重复执行：部门/角色用 ON CONFLICT；用户用 WHERE NOT EXISTS；关联用 NOT EXISTS。
-- =============================================================================

BEGIN;

-- 密码均为 123456 → BCrypt（与 BCrypt.Net 一致）
-- ---------- 1) 四个子部门（父级：总公司 seed 中的 DepartmentId）----------
INSERT INTO sys_department (
    "DepartmentId", "DepartmentName", "ParentId", "Path", "Level",
    "SaleDataScope", "PurchaseDataScope", "IdentityType", "Status", "CreateTime"
) VALUES
('10000000-0000-4000-8000-000000000101', '销售部', '10000000-0000-4000-8000-000000000001',
 '/10000000-0000-4000-8000-000000000001/10000000-0000-4000-8000-000000000101', 2, 2, 2, 1, 1, NOW()),
('10000000-0000-4000-8000-000000000102', '采购部', '10000000-0000-4000-8000-000000000001',
 '/10000000-0000-4000-8000-000000000001/10000000-0000-4000-8000-000000000102', 2, 2, 2, 2, 1, NOW()),
('10000000-0000-4000-8000-000000000103', '物流部', '10000000-0000-4000-8000-000000000001',
 '/10000000-0000-4000-8000-000000000001/10000000-0000-4000-8000-000000000103', 2, 2, 2, 6, 1, NOW()),
('10000000-0000-4000-8000-000000000104', '财务部', '10000000-0000-4000-8000-000000000001',
 '/10000000-0000-4000-8000-000000000001/10000000-0000-4000-8000-000000000104', 2, 2, 2, 5, 1, NOW())
ON CONFLICT ("DepartmentId") DO NOTHING;

-- ---------- 2) 角色：部门经理（除 rbac 管理外全权限） / 员工（只读类权限）----------
-- 部门角色编码以 seed_dept_org_roles.sql 为准：DEPT_MANAGER / DEPT_EMPLOYEE / DEPT_DIRECTOR
INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime") VALUES
('21000000-0000-4000-8000-000000000001', 'DEPT_MANAGER', '部门经理', '业务全权限（不含 rbac.manage）', 1, NOW()),
('21000000-0000-4000-8000-000000000002', 'DEPT_EMPLOYEE', '部门员工', '只读 + 查看敏感字段类权限', 1, NOW())
ON CONFLICT ("RoleCode") DO NOTHING;

-- 经理：除 rbac.manage 外全部权限
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'DEPT_MANAGER'
  AND p."PermissionCode" <> 'rbac.manage'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 员工：只读等（与前端/接口常用读权限对齐）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'DEPT_EMPLOYEE'
  AND p."PermissionCode" IN (
    'customer.read', 'customer.info.read',
    'vendor.read', 'vendor.info.read',
    'rfq.read', 'rfq.write',
    'sales-order.read', 'sales.amount.read',
    'purchase-order.read', 'purchase.amount.read',
    'draft.read',
    'finance-receipt.read', 'finance-payment.read',
    'finance-sell-invoice.read', 'finance-purchase-invoice.read'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- ---------- 3) 八个用户（登录名 ASCII，便于输入）----------
INSERT INTO "user" ("UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain", "IsActive", "Status", "RealName", "CreateTime")
SELECT * FROM (VALUES
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a01', 'sales_mgr',      'sales_mgr@demo.local',      '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '销售经理', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a02', 'sales_staff',    'sales_staff@demo.local',    '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '销售员工', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a03', 'purchase_mgr',   'purchase_mgr@demo.local',   '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '采购经理', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a04', 'purchase_staff', 'purchase_staff@demo.local', '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '采购员工', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a05', 'logistics_mgr',  'logistics_mgr@demo.local',  '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '物流经理', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a06', 'logistics_staff','logistics_staff@demo.local','$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '物流员工', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a07', 'finance_mgr',    'finance_mgr@demo.local',    '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '财务经理', NOW()),
  ('b1eebc99-9c0b-4ef8-bb6d-6bb9bd380a08', 'finance_staff',  'finance_staff@demo.local',  '$2a$11$W5kKrMCkxOmtTPJaxCtlsuFfVyBhvkZnoI8LZGmX4rc2WkB/9rCXa', 'init_salt', '123456', TRUE, 1, '财务员工', NOW())
) AS v("UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain", "IsActive", "Status", "RealName", "CreateTime")
WHERE NOT EXISTS (SELECT 1 FROM "user" u WHERE u."UserName" = v."UserName");

-- ---------- 4) 用户 <-> 角色 ----------
INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", NOW()
FROM "user" u
CROSS JOIN sys_role r
WHERE (u."UserName", r."RoleCode") IN (
  ('sales_mgr', 'DEPT_MANAGER'), ('sales_staff', 'DEPT_EMPLOYEE'),
  ('purchase_mgr', 'DEPT_MANAGER'), ('purchase_staff', 'DEPT_EMPLOYEE'),
  ('logistics_mgr', 'DEPT_MANAGER'), ('logistics_staff', 'DEPT_EMPLOYEE'),
  ('finance_mgr', 'DEPT_MANAGER'), ('finance_staff', 'DEPT_EMPLOYEE')
)
AND NOT EXISTS (
  SELECT 1 FROM sys_user_role x WHERE x."UserId" = u."UserId" AND x."RoleId" = r."RoleId"
);

-- ---------- 5) 用户 <-> 部门（主部门）----------
INSERT INTO sys_user_department ("UserDepartmentId", "UserId", "DepartmentId", "IsPrimary", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", d."DepartmentId", TRUE, NOW()
FROM "user" u
JOIN sys_department d ON (u."UserName", d."DepartmentName") IN (
  ('sales_mgr', '销售部'), ('sales_staff', '销售部'),
  ('purchase_mgr', '采购部'), ('purchase_staff', '采购部'),
  ('logistics_mgr', '物流部'), ('logistics_staff', '物流部'),
  ('finance_mgr', '财务部'), ('finance_staff', '财务部')
)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_user_department x
  WHERE x."UserId" = u."UserId" AND x."DepartmentId" = d."DepartmentId"
);

COMMIT;

-- 校验
SELECT u."UserName", u."RealName", d."DepartmentName", ro."RoleCode"
FROM "user" u
LEFT JOIN sys_user_department ud ON ud."UserId" = u."UserId"
LEFT JOIN sys_department d ON d."DepartmentId" = ud."DepartmentId"
LEFT JOIN sys_user_role ur ON ur."UserId" = u."UserId"
LEFT JOIN sys_role ro ON ro."RoleId" = ur."RoleId"
WHERE u."UserName" LIKE '%_mgr' OR u."UserName" LIKE '%_staff'
ORDER BY u."UserName";

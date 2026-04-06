-- =============================================================================
-- FrontCRM：演示部门 + 演示员工（与界面截图一致，可重复执行）
--
-- 部门：商务部、财务部、物流部、采购部、采购运营部、销售部（一级，销售/采购数据范围=本部门及下级）
-- 员工：18 个账号（Admin + 各部门总监/经理/员工；不含 log_staff、财务部演示账号）
--
-- 默认密码：
--   Admin     → Admin123
--   其余职员 → 123456
--
-- 前置（需已存在对应 RoleCode；若空库请先执行 seed_initial_rbac_admin.sql，并确保存在业务角色）：
--   SYS_ADMIN, DEPT_DIRECTOR, DEPT_MANAGER, DEPT_EMPLOYEE,
--   biz_all, sales_operator, purchase_buyer, commerce_operator,
--   purchase_ops_operator, logistics_operator
-- 可与 prod-migration.sql 中「角色」段或下列「可选：业务角色」一并准备。
--
-- 执行：
--   psql "postgresql://..." -v ON_ERROR_STOP=1 -f seed_departments_and_demo_employees.sql
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

BEGIN;

-- ---------- 可选：业务角色（与 prod-migration 一致；ON CONFLICT 跳过已存在）----------
INSERT INTO sys_role ("RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime") VALUES
('r0000000-0000-4000-8000-000000000005', 'purchase_buyer', '采购员', '询价分配/采购执行（轮询角色码）', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000006', 'biz_all', '业务全权限(非RBAC)', '总监/经理业务菜单', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000007', 'sales_operator', '销售职员权限', '销售部门员工', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000008', 'purchase_operator', '采购职员权限', '采购部门非买家或经理辅助', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000009', 'commerce_operator', '商务职员权限', '商务部门员工', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000010', 'purchase_ops_operator', '采购运营职员权限', '采购运营员工', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000011', 'logistics_operator', '物流职员权限', '物流部门员工', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('r0000000-0000-4000-8000-000000000012', 'finance_operator', '财务职员权限', '财务部门员工', 1, TIMESTAMPTZ '2026-01-01T00:00:00Z')
ON CONFLICT ("RoleCode") DO NOTHING;

-- ---------- 部门（IdentityType: 1销售 2采购 3采购运营 4商务 5财务 6物流；数据范围 3=本部门及下级）----------
INSERT INTO sys_department (
    "DepartmentId", "DepartmentName", "ParentId", "Path", "Level",
    "SaleDataScope", "PurchaseDataScope", "IdentityType", "Status", "CreateTime"
) VALUES
('d0000000-0000-4000-8000-000000000001', '销售部', NULL, '/SALES', 1, 3, 3, 1, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('d0000000-0000-4000-8000-000000000002', '采购部', NULL, '/PURCHASE', 1, 3, 3, 2, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('d0000000-0000-4000-8000-000000000003', '商务部', NULL, '/COMMERCE', 1, 3, 3, 4, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('d0000000-0000-4000-8000-000000000004', '采购运营部', NULL, '/PURCHASE_OPS', 1, 3, 3, 3, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('d0000000-0000-4000-8000-000000000005', '物流部', NULL, '/LOGISTICS', 1, 3, 3, 6, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z'),
('d0000000-0000-4000-8000-000000000006', '财务部', NULL, '/FINANCE', 1, 3, 3, 5, 1, TIMESTAMPTZ '2026-01-01T00:00:00Z')
ON CONFLICT ("DepartmentId") DO NOTHING;

-- ---------- 用户（BCrypt：Password 列；Salt 占位与现有注册逻辑一致）----------
INSERT INTO "user" (
    "UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain",
    "IsActive", "Status", "RealName", "CreateTime"
) VALUES
(
    'u0000000-0000-4000-8000-000000000000',
    'Admin',
    'admin@local',
    '$2a$11$NG0gQf4DfJDQRP47SJ3OXueDdnuKS3gNjU4lLFCZ35Q9ypDPRFJfu',
    'init_salt',
    'Admin123',
    TRUE,
    1,
    '系统管理员',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000010001',
    'sales_director',
    'sales.zj@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '销售总监',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000010002',
    'sales_manager',
    'sales.jl@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '销售经理',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000010003',
    'sales_staff',
    'sales.yg@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '销售员工',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020001',
    'pur_director',
    'pur.zj@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购总监',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020002',
    'pur_manager',
    'pur.jl@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购经理',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020003',
    'pur_buyer1',
    'pur.cg1@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购员一',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020004',
    'pur_buyer2',
    'pur.cg2@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购员二',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020005',
    'pur_buyer3',
    'pur.cg3@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购员三',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000020006',
    'pur_buyer4',
    'pur.cg4@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购员四',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000030001',
    'biz_director',
    'biz.zj@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '商务总监',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000030002',
    'biz_manager',
    'biz.jl@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '商务经理',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000030003',
    'biz_staff',
    'biz.yg@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '商务员工',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000040001',
    'purops_director',
    'purops.zj@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购运营总监',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000040002',
    'purops_manager',
    'purops.jl@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购运营经理',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000040003',
    'purops_staff',
    'purops.yg@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '采购运营员工',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000050001',
    'log_director',
    'log.zj@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '物流总监',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
),
(
    'u0000000-0000-4000-8000-000000050002',
    'log_manager',
    'log.jl@local',
    '$2a$11$RTn5NkLMwh4A475vXK6bHu8/YMUv8PLyeJd7EgqdWetIdy4Wnds.e',
    'init_salt',
    '123456',
    TRUE,
    1,
    '物流经理',
    TIMESTAMPTZ '2026-01-01T00:00:00Z'
)
ON CONFLICT ("UserId") DO NOTHING;

-- ---------- 主部门 ----------
INSERT INTO sys_user_department ("UserDepartmentId", "UserId", "DepartmentId", "IsPrimary", "CreateTime")
SELECT gen_random_uuid()::text, t."UserId", t."DepartmentId", TRUE, TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM (VALUES
    ('u0000000-0000-4000-8000-000000000000', 'd0000000-0000-4000-8000-000000000001'),
    ('u0000000-0000-4000-8000-000000010001', 'd0000000-0000-4000-8000-000000000001'),
    ('u0000000-0000-4000-8000-000000010002', 'd0000000-0000-4000-8000-000000000001'),
    ('u0000000-0000-4000-8000-000000010003', 'd0000000-0000-4000-8000-000000000001'),
    ('u0000000-0000-4000-8000-000000020001', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000020002', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000020003', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000020004', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000020005', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000020006', 'd0000000-0000-4000-8000-000000000002'),
    ('u0000000-0000-4000-8000-000000030001', 'd0000000-0000-4000-8000-000000000003'),
    ('u0000000-0000-4000-8000-000000030002', 'd0000000-0000-4000-8000-000000000003'),
    ('u0000000-0000-4000-8000-000000030003', 'd0000000-0000-4000-8000-000000000003'),
    ('u0000000-0000-4000-8000-000000040001', 'd0000000-0000-4000-8000-000000000004'),
    ('u0000000-0000-4000-8000-000000040002', 'd0000000-0000-4000-8000-000000000004'),
    ('u0000000-0000-4000-8000-000000040003', 'd0000000-0000-4000-8000-000000000004'),
    ('u0000000-0000-4000-8000-000000050001', 'd0000000-0000-4000-8000-000000000005'),
    ('u0000000-0000-4000-8000-000000050002', 'd0000000-0000-4000-8000-000000000005')
) AS t("UserId", "DepartmentId")
WHERE EXISTS (SELECT 1 FROM "user" u WHERE u."UserId" = t."UserId")
  AND EXISTS (SELECT 1 FROM sys_department d WHERE d."DepartmentId" = t."DepartmentId")
ON CONFLICT ("UserId", "DepartmentId") DO NOTHING;

-- ---------- 角色（按 RoleCode 绑定，兼容不同 RoleId 种子）----------
INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'Admin' AND r."RoleCode" = 'SYS_ADMIN'
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'sales_director' AND r."RoleCode" IN ('DEPT_DIRECTOR', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'sales_manager' AND r."RoleCode" IN ('DEPT_MANAGER', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'sales_staff' AND r."RoleCode" IN ('DEPT_EMPLOYEE', 'sales_operator')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'pur_director' AND r."RoleCode" IN ('DEPT_DIRECTOR', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'pur_manager' AND r."RoleCode" IN ('DEPT_MANAGER', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" IN ('pur_buyer1', 'pur_buyer2', 'pur_buyer3', 'pur_buyer4')
  AND r."RoleCode" IN ('DEPT_EMPLOYEE', 'purchase_buyer')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'biz_director' AND r."RoleCode" IN ('DEPT_DIRECTOR', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'biz_manager' AND r."RoleCode" IN ('DEPT_MANAGER', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'biz_staff' AND r."RoleCode" IN ('DEPT_EMPLOYEE', 'commerce_operator')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'purops_director' AND r."RoleCode" IN ('DEPT_DIRECTOR', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'purops_manager' AND r."RoleCode" IN ('DEPT_MANAGER', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'purops_staff' AND r."RoleCode" IN ('DEPT_EMPLOYEE', 'purchase_ops_operator')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'log_director' AND r."RoleCode" IN ('DEPT_DIRECTOR', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", TIMESTAMPTZ '2026-01-01T00:00:00Z'
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'log_manager' AND r."RoleCode" IN ('DEPT_MANAGER', 'biz_all')
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

COMMIT;

-- ---------- 校验 ----------
SELECT d."DepartmentName", d."IdentityType", d."SaleDataScope", d."PurchaseDataScope"
FROM sys_department d
WHERE d."DepartmentId" LIKE 'd0000000-%'
ORDER BY d."DepartmentName";

SELECT u."UserName", u."RealName", u."Email", d."DepartmentName" AS main_dept
FROM "user" u
LEFT JOIN sys_user_department ud ON ud."UserId" = u."UserId" AND ud."IsPrimary" = TRUE
LEFT JOIN sys_department d ON d."DepartmentId" = ud."DepartmentId"
WHERE u."UserId" LIKE 'u0000000-%'
ORDER BY u."UserName";

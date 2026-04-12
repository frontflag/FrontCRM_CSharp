-- =============================================================================
-- FrontCRM：初始化 RBAC + 管理员（幂等，可重复执行）
--
-- 前置：已执行 create_full_database.sql（或 EF 已建全表）。
-- 说明：
--   • 系统管理员角色代码必须为 SYS_ADMIN（与 RbacService.IsSysAdmin 一致）。
--   • 前端路由 + API [RequirePermission] 涉及的权限码见下方 INSERT。
--   • 默认账号：admin / admin123（与 update_admin_password.sql 相同 BCrypt）。
--   • 「基础参数」：业务流水号已在 create_full_database.sql 的 sys_serial_number 种子中；
--     可选再执行 create_debug_table.sql。
--
-- 在腾讯云 / psql 中执行：
--   psql "postgresql://用户:密码@主机:端口/FrontCRM" -v ON_ERROR_STOP=1 -f seed_initial_rbac_admin.sql
-- =============================================================================

BEGIN;

-- ---------- 1) 根部门（数据权限范围 0=全部）----------
INSERT INTO sys_department (
    "DepartmentId", "DepartmentName", "ParentId", "Path", "Level",
    "SaleDataScope", "PurchaseDataScope", "IdentityType", "Status", "CreateTime"
) VALUES (
    '10000000-0000-4000-8000-000000000001',
    '总公司',
    NULL,
    '/10000000-0000-4000-8000-000000000001',
    1,
    0,
    0,
    0,
    1,
    NOW()
)
ON CONFLICT ("DepartmentId") DO NOTHING;

-- ---------- 2) 系统管理员角色 ----------
INSERT INTO sys_role (
    "RoleId", "RoleCode", "RoleName", "Description", "Status", "CreateTime"
) VALUES (
    '20000000-0000-4000-8000-000000000001',
    'SYS_ADMIN',
    '系统管理员',
    '拥有全部功能；RoleCode 不可改（代码依此识别 IsSysAdmin）',
    1,
    NOW()
)
ON CONFLICT ("RoleCode") DO NOTHING;

-- ---------- 3) 权限定义（与 CRM.API RequirePermission + 前端路由 meta.permission 对齐）----------
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime") VALUES
('30000000-0000-4000-8000-000000000001', 'customer.read', '客户-查看', 'api', 'customer', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000002', 'customer.write', '客户-维护', 'api', 'customer', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000003', 'customer.info.read', '客户敏感信息-查看', 'api', 'customer', 'info.read', 1, NOW()),
('30000000-0000-4000-8000-000000000004', 'vendor.read', '供应商-查看', 'api', 'vendor', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000005', 'vendor.write', '供应商-维护', 'api', 'vendor', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000006', 'vendor.info.read', '供应商敏感信息-查看', 'api', 'vendor', 'info.read', 1, NOW()),
('30000000-0000-4000-8000-000000000007', 'rfq.read', '询价/需求-查看', 'api', 'rfq', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000008', 'rfq.write', '询价/需求-维护', 'api', 'rfq', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000020', 'rfq.create', '询价/需求-创建', 'api', 'rfq', 'create', 1, NOW()),
('30000000-0000-4000-8000-000000000009', 'sales-order.read', '销售订单-查看', 'api', 'sales-order', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000010', 'sales-order.write', '销售订单-维护', 'api', 'sales-order', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000011', 'sales.amount.read', '销售金额-查看', 'api', 'sales', 'amount.read', 1, NOW()),
('30000000-0000-4000-8000-000000000012', 'purchase-order.read', '采购订单-查看', 'api', 'purchase-order', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000013', 'purchase-order.write', '采购订单-维护', 'api', 'purchase-order', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000014', 'purchase.amount.read', '采购金额-查看', 'api', 'purchase', 'amount.read', 1, NOW()),
('30000000-0000-4000-8000-000000000015', 'draft.read', '草稿-查看', 'api', 'draft', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000016', 'draft.write', '草稿-维护', 'api', 'draft', 'write', 1, NOW()),
('30000000-0000-4000-8000-000000000017', 'rbac.manage', '用户/角色/权限管理', 'api', 'rbac', 'manage', 1, NOW()),
('30000000-0000-4000-8000-000000000018', 'finance-receipt.read', '收款-查看', 'api', 'finance-receipt', 'read', 1, NOW()),
('30000000-0000-4000-8000-000000000019', 'finance-receipt.write', '收款-维护', 'api', 'finance-receipt', 'write', 1, NOW()),
('30000000-0000-4000-8000-00000000001a', 'finance-payment.read', '付款-查看', 'api', 'finance-payment', 'read', 1, NOW()),
('30000000-0000-4000-8000-00000000001b', 'finance-payment.write', '付款-维护', 'api', 'finance-payment', 'write', 1, NOW()),
('30000000-0000-4000-8000-00000000001c', 'finance-sell-invoice.read', '销项发票-查看', 'api', 'finance-sell-invoice', 'read', 1, NOW()),
('30000000-0000-4000-8000-00000000001d', 'finance-sell-invoice.write', '销项发票-维护', 'api', 'finance-sell-invoice', 'write', 1, NOW()),
('30000000-0000-4000-8000-00000000001e', 'finance-purchase-invoice.read', '进项发票-查看', 'api', 'finance-purchase-invoice', 'read', 1, NOW()),
('30000000-0000-4000-8000-00000000001f', 'finance-purchase-invoice.write', '进项发票-维护', 'api', 'finance-purchase-invoice', 'write', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

-- ---------- 4) SYS_ADMIN 绑定全部权限（按 PermissionCode 解析 Id，避免重复执行冲突）----------
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND p."Status" = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- ---------- 5) admin 用户（BCrypt: admin123；user 表无 UserName 唯一约束，用 UPDATE + 条件 INSERT）----------
UPDATE "user" SET
    "Password" = '$2a$11$vqvt8BRISDc6itm/ANyMGOo39xI8vkqEI.8IRvfcEW2mV9IA77to.',
    "Salt" = COALESCE(NULLIF("Salt", ''), 'init_salt'),
    "PasswordPlain" = 'admin123',
    "IsActive" = TRUE,
    "Status" = 1,
    "Email" = COALESCE("Email", 'admin@frontcrm.com')
WHERE "UserName" = 'admin';

INSERT INTO "user" (
    "UserId", "UserName", "Email", "Password", "Salt", "PasswordPlain",
    "IsActive", "Status", "CreateTime"
)
SELECT
    'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    'admin',
    'admin@frontcrm.com',
    '$2a$11$vqvt8BRISDc6itm/ANyMGOo39xI8vkqEI.8IRvfcEW2mV9IA77to.',
    'init_salt',
    'admin123',
    TRUE,
    1,
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "user" u2 WHERE u2."UserName" = 'admin');

-- ---------- 6) admin -> SYS_ADMIN ----------
INSERT INTO sys_user_role ("UserRoleId", "UserId", "RoleId", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", r."RoleId", NOW()
FROM "user" u
CROSS JOIN sys_role r
WHERE u."UserName" = 'admin' AND r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
    SELECT 1 FROM sys_user_role x
    WHERE x."UserId" = u."UserId" AND x."RoleId" = r."RoleId"
  );

-- ---------- 7) admin -> 根部门（主部门）----------
INSERT INTO sys_user_department ("UserDepartmentId", "UserId", "DepartmentId", "IsPrimary", "CreateTime")
SELECT gen_random_uuid()::text, u."UserId", d."DepartmentId", TRUE, NOW()
FROM "user" u
CROSS JOIN sys_department d
WHERE u."UserName" = 'admin'
  AND d."DepartmentId" = '10000000-0000-4000-8000-000000000001'
  AND NOT EXISTS (
    SELECT 1 FROM sys_user_department x
    WHERE x."UserId" = u."UserId" AND x."DepartmentId" = d."DepartmentId"
  );

COMMIT;

-- ---------- 校验 ----------
SELECT 'roles' AS t, "RoleCode", "RoleName" FROM sys_role ORDER BY "RoleCode";
SELECT 'admin roles' AS t, ur.* FROM sys_user_role ur
JOIN "user" u ON u."UserId" = ur."UserId" WHERE u."UserName" = 'admin';
SELECT 'permission count' AS t, COUNT(*)::text FROM sys_permission;
SELECT 'admin' AS t, "UserName", "Email", "IsActive" FROM "user" WHERE "UserName" = 'admin';

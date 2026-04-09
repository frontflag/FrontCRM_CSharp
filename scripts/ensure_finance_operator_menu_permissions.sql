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

COMMIT;

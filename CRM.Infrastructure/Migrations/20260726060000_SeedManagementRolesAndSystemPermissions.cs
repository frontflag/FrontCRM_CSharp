using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 三级管理角色：SYS_MANAGER（Admin）、SYS_BIZ_MANAGER（Manager）及 system.* 权限种子。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260726060000_SeedManagementRolesAndSystemPermissions")]
    public partial class SeedManagementRolesAndSystemPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 管理角色
INSERT INTO sys_role (""RoleId"", ""RoleCode"", ""RoleName"", ""Description"", ""Status"", ""CreateTime"") VALUES
('20000000-0000-4000-8000-000000000002', 'SYS_MANAGER', '平台管理员', '产品 Admin：部分系统管理 + 可建 Manager；RoleCode 固定', 1, NOW()),
('20000000-0000-4000-8000-000000000003', 'SYS_BIZ_MANAGER', '业务经理', '产品 Manager：业务数据全量 + 管普通员工；RoleCode 固定', 1, NOW())
ON CONFLICT (""RoleCode"") DO NOTHING;

-- system.* 权限
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-000000000001', 'system.org.users.read', '系统-员工-查看', 'api', 'system.org.users', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000002', 'system.org.users.write', '系统-员工-维护', 'api', 'system.org.users', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000003', 'system.org.users.reset-password', '系统-员工-重置密码', 'api', 'system.org.users', 'reset-password', 1, NOW()),
('31000000-0000-4000-8000-000000000004', 'system.org.departments.read', '系统-部门-查看', 'api', 'system.org.departments', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000005', 'system.org.departments.write', '系统-部门-维护', 'api', 'system.org.departments', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000006', 'system.org.user-config.read', '系统-用户配置-查看', 'api', 'system.org.user-config', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000007', 'system.org.user-config.write', '系统-用户配置-维护', 'api', 'system.org.user-config', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000008', 'system.rbac.roles.read', '系统-角色-查看', 'api', 'system.rbac.roles', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000009', 'system.rbac.roles.write', '系统-角色-维护', 'api', 'system.rbac.roles', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000000a', 'system.rbac.permissions.read', '系统-权限-查看', 'api', 'system.rbac.permissions', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000000b', 'system.rbac.permissions.write', '系统-权限-维护', 'api', 'system.rbac.permissions', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000000c', 'system.params.company.read', '系统-公司信息-查看', 'api', 'system.params.company', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000000d', 'system.params.company.write', '系统-公司信息-维护', 'api', 'system.params.company', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000000e', 'system.params.dict.read', '系统-数据字典-查看', 'api', 'system.params.dict', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000000f', 'system.params.dict.write', '系统-数据字典-维护', 'api', 'system.params.dict', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000010', 'system.params.sales.read', '系统-销售参数-查看', 'api', 'system.params.sales', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000011', 'system.params.sales.write', '系统-销售参数-维护', 'api', 'system.params.sales', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000001a', 'system.params.sales.refresh-customer.read', '系统-销售参数-刷新客户-查看', 'api', 'system.params.sales.refresh-customer', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000001b', 'system.params.sales.refresh-customer.write', '系统-销售参数-刷新客户-维护', 'api', 'system.params.sales.refresh-customer', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000012', 'system.params.purchase.read', '系统-采购参数-查看', 'api', 'system.params.purchase', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000013', 'system.params.purchase.write', '系统-采购参数-维护', 'api', 'system.params.purchase', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000014', 'system.params.purchase.refresh-vendor.read', '系统-采购参数-刷新供应商-查看', 'api', 'system.params.purchase.refresh-vendor', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000015', 'system.params.purchase.refresh-vendor.write', '系统-采购参数-刷新供应商-维护', 'api', 'system.params.purchase.refresh-vendor', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000016', 'system.params.finance.read', '系统-财务参数-查看', 'api', 'system.params.finance', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000017', 'system.params.finance.write', '系统-财务参数-维护', 'api', 'system.params.finance', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000018', 'system.logs.login.read', '系统-登录日志-查看', 'api', 'system.logs.login', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000019', 'system.logs.operation.read', '系统-操作日志-查看', 'api', 'system.logs.operation', 'read', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

-- SYS_ADMIN 绑定全部 system.*
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_ADMIN'
  AND p.""PermissionCode"" LIKE 'system.%'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );

-- SYS_MANAGER 默认开放面（不含角色/权限定义、不含刷新供应商/刷新客户）
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_MANAGER'
  AND p.""PermissionCode"" IN (
    'system.org.users.read','system.org.users.write','system.org.users.reset-password',
    'system.org.departments.read','system.org.departments.write',
    'system.org.user-config.read','system.org.user-config.write',
    'system.params.company.read','system.params.company.write',
    'system.params.dict.read','system.params.dict.write',
    'system.params.sales.read','system.params.sales.write',
    'system.params.purchase.read','system.params.purchase.write',
    'system.params.finance.read','system.params.finance.write',
    'system.logs.login.read','system.logs.operation.read'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );

-- SYS_BIZ_MANAGER 仅员工
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_BIZ_MANAGER'
  AND p.""PermissionCode"" IN (
    'system.org.users.read','system.org.users.write','system.org.users.reset-password'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sys_role_permission WHERE ""RoleId"" IN (
  SELECT ""RoleId"" FROM sys_role WHERE ""RoleCode"" IN ('SYS_MANAGER','SYS_BIZ_MANAGER')
);
DELETE FROM sys_role_permission WHERE ""PermissionId"" IN (
  SELECT ""PermissionId"" FROM sys_permission WHERE ""PermissionCode"" LIKE 'system.%'
);
DELETE FROM sys_permission WHERE ""PermissionCode"" LIKE 'system.%';
DELETE FROM sys_role WHERE ""RoleCode"" IN ('SYS_MANAGER','SYS_BIZ_MANAGER');
");
        }
    }
}

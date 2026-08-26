using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报表参数：侧栏 + 页内「报表全局参数」权限；SYS_ADMIN / SYS_MANAGER 默认同开。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260925120000_SeedReportParamsPermissions")]
    public partial class SeedReportParamsPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-00000000002b', 'system.params.report.read', '系统-报表参数-查看', 'api', 'system.params.report', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000002c', 'system.params.report.write', '系统-报表参数-维护', 'api', 'system.params.report', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000002d', 'system.params.report.global.read', '系统-报表参数-报表全局参数-查看', 'api', 'system.params.report.global', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000002e', 'system.params.report.global.write', '系统-报表参数-报表全局参数-维护', 'api', 'system.params.report.global', 'write', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_ADMIN'
  AND p.""PermissionCode"" IN (
    'system.params.report.read','system.params.report.write',
    'system.params.report.global.read','system.params.report.global.write'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_MANAGER'
  AND p.""PermissionCode"" IN (
    'system.params.report.read','system.params.report.write',
    'system.params.report.global.read','system.params.report.global.write'
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
DELETE FROM sys_role_permission WHERE ""PermissionId"" IN (
  SELECT ""PermissionId"" FROM sys_permission WHERE ""PermissionCode"" IN (
    'system.params.report.read','system.params.report.write',
    'system.params.report.global.read','system.params.report.global.write'
  )
);
DELETE FROM sys_permission WHERE ""PermissionCode"" IN (
  'system.params.report.read','system.params.report.write',
  'system.params.report.global.read','system.params.report.global.write'
);
");
        }
    }
}

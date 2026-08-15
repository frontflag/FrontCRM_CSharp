using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>系统日志：导出日志查看权限；已有操作日志权限的角色默认同开。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260902120000_SeedExportLogPermission")]
    public partial class SeedExportLogPermission : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-00000000002a', 'system.logs.export.read', '系统-导出日志-查看', 'api', 'system.logs.export', 'read', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_ADMIN'
  AND p.""PermissionCode"" = 'system.logs.export.read'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, rp_exist.""RoleId"", p_new.""PermissionId"", NOW()
FROM sys_role_permission rp_exist
JOIN sys_permission p_old ON p_old.""PermissionId"" = rp_exist.""PermissionId""
JOIN sys_permission p_new ON p_new.""PermissionCode"" = 'system.logs.export.read'
WHERE p_old.""PermissionCode"" = 'system.logs.operation.read'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = rp_exist.""RoleId"" AND rp.""PermissionId"" = p_new.""PermissionId""
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sys_role_permission
WHERE ""PermissionId"" IN (
  SELECT ""PermissionId"" FROM sys_permission WHERE ""PermissionCode"" = 'system.logs.export.read'
);
DELETE FROM sys_permission WHERE ""PermissionCode"" = 'system.logs.export.read';
");
        }
    }
}

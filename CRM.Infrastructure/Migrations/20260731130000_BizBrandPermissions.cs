using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260731130000_BizBrandPermissions")]
    public partial class BizBrandPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""ResourceType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"")
VALUES
  ('30000000-0000-4000-8000-0000000000b0', 'biz-brand.read', '品牌-查看', 'api', 'biz-brand', 'read', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000b1', 'biz-brand.write', '品牌-维护', 'api', 'biz-brand', 'write', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
JOIN sys_permission p ON p.""PermissionCode"" IN ('biz-brand.read', 'biz-brand.write')
  AND p.""Status"" = 1
WHERE r.""RoleCode"" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x.""RoleId"" = r.""RoleId"" AND x.""PermissionId"" = p.""PermissionId""
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sys_role_permission rp
USING sys_permission p
WHERE rp.""PermissionId"" = p.""PermissionId""
  AND p.""PermissionCode"" IN ('biz-brand.read', 'biz-brand.write');

DELETE FROM sys_permission
WHERE ""PermissionCode"" IN ('biz-brand.read', 'biz-brand.write');
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 补齐 sys_permission.draft.write（与 DraftsController RequirePermission 一致）。
    /// prod 种子曾仅有 draft.read，导致角色配置下拉无 draft.write、保存草稿 403。
    /// 已为拥有 draft.read 的角色自动补绑 draft.write。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518120000_AddSysPermissionDraftWrite")]
    public partial class AddSysPermissionDraftWrite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"")
SELECT 'p0000000-0000-4000-8000-00000000001c', 'draft.write', '草稿维护', 'api', NULL, NULL, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE ""PermissionCode"" = 'draft.write');

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, rp.""RoleId"", p_write.""PermissionId"", NOW()
FROM sys_role_permission rp
JOIN sys_permission p_read ON p_read.""PermissionId"" = rp.""PermissionId"" AND p_read.""PermissionCode"" = 'draft.read'
JOIN sys_permission p_write ON p_write.""PermissionCode"" = 'draft.write' AND p_write.""Status"" = 1
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role_permission x
  WHERE x.""RoleId"" = rp.""RoleId"" AND x.""PermissionId"" = p_write.""PermissionId""
);

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'purchase_buyer'
  AND p.""PermissionCode"" IN ('draft.read', 'draft.write')
  AND p.""Status"" = 1
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
  AND p.""PermissionCode"" = 'draft.write';

DELETE FROM sys_permission WHERE ""PermissionCode"" = 'draft.write';
");
        }
    }
}

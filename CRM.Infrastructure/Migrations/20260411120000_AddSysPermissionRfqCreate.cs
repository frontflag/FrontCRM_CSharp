using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 新增权限 rfq.create（创建需求 POST）；采购主部门用户在 RbacService 汇总中会剥离该码。
    /// 为常见销售相关 RoleCode 补绑，便于 JWT 与员工管理展示。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411120000_AddSysPermissionRfqCreate")]
    public partial class AddSysPermissionRfqCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"")
SELECT gen_random_uuid()::text, 'rfq.create', '需求创建', 'api', 'rfq', 'create', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE ""PermissionCode"" = 'rfq.create');

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
JOIN sys_permission p ON p.""PermissionCode"" = 'rfq.create' AND p.""Status"" = 1
WHERE r.""RoleCode"" IN ('SYS_ADMIN', 'SALES', 'DEPT_DIRECTOR', 'DEPT_MANAGER', 'biz_all', 'sales_operator')
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
  AND p.""PermissionCode"" = 'rfq.create';

DELETE FROM sys_permission WHERE ""PermissionCode"" = 'rfq.create';
");
        }
    }
}

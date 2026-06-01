using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// commerce_operator 补绑销售侧写权限；商务部主部门用户在 RbacService 汇总中亦会合并 rfq.create / sales-order.write 等。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260724120000_CommerceOperatorSalesRfqPermissions")]
    public partial class CommerceOperatorSalesRfqPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
JOIN sys_permission p ON p.""PermissionCode"" IN (
    'customer.read', 'customer.write',
    'rfq.read', 'rfq.write', 'rfq.create',
    'sales-order.read', 'sales-order.write',
    'draft.write')
  AND p.""Status"" = 1
WHERE r.""RoleCode"" = 'commerce_operator'
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
USING sys_role r, sys_permission p
WHERE rp.""RoleId"" = r.""RoleId""
  AND rp.""PermissionId"" = p.""PermissionId""
  AND r.""RoleCode"" = 'commerce_operator'
  AND p.""PermissionCode"" IN (
    'customer.write',
    'rfq.write', 'rfq.create',
    'sales-order.write');
");
        }
    }
}

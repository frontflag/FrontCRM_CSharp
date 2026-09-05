using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260905190100_SeedCustomerQuotePermissions")]
public partial class SeedCustomerQuotePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-000000000030', 'customer-quote.read', '客户报价单-查看', 'api', 'customer-quote', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000031', 'customer-quote.write', '客户报价单-维护', 'api', 'customer-quote', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000032', 'customer-quote.send', '客户报价单-发送', 'api', 'customer-quote', 'send', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" IN ('SYS_ADMIN', 'SYS_MANAGER', 'commerce_operator', 'sale_operator')
  AND p.""PermissionCode"" IN ('customer-quote.read', 'customer-quote.write', 'customer-quote.send')
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
    'customer-quote.read', 'customer-quote.write', 'customer-quote.send'
  )
);
DELETE FROM sys_permission WHERE ""PermissionCode"" IN (
  'customer-quote.read', 'customer-quote.write', 'customer-quote.send'
);
");
    }
}

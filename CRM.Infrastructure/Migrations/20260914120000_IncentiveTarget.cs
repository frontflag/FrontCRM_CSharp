using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260914120000_IncentiveTarget")]
public partial class AddIncentiveTarget : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.incentive_target (
  id                 varchar(36)    NOT NULL,
  user_id            varchar(36)    NOT NULL,
  role_type          smallint       NOT NULL,
  period_kind        smallint       NOT NULL,
  period_key         varchar(8)     NOT NULL,
  target_amount_usd  numeric(18,2)  NOT NULL,
  create_time        timestamptz    NOT NULL,
  modify_time        timestamptz    NOT NULL,
  is_deleted         boolean        NOT NULL DEFAULT false,
  CONSTRAINT pk_incentive_target PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_incentive_target_user_period
  ON public.incentive_target (user_id, role_type, period_kind, period_key)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ix_incentive_target_user
  ON public.incentive_target (user_id)
  WHERE is_deleted = false;

INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('c1000000-0000-4000-8000-000000000105', 'incentive-target.read', '激励目标-查看', 'api', 'incentive-target', 'read', 1, NOW()),
('c1000000-0000-4000-8000-000000000106', 'incentive-target.write', '激励目标-维护', 'api', 'incentive-target', 'write', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" IN ('SYS_ADMIN', 'SYS_MANAGER', 'commerce_operator', 'sale_operator', 'purchase_operator', 'purchase_buyer')
  AND p.""PermissionCode"" IN ('incentive-target.read', 'incentive-target.write')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.incentive_target;
DELETE FROM sys_role_permission WHERE ""PermissionId"" IN (
  SELECT ""PermissionId"" FROM sys_permission WHERE ""PermissionCode"" IN ('incentive-target.read', 'incentive-target.write')
);
DELETE FROM sys_permission WHERE ""PermissionCode"" IN ('incentive-target.read', 'incentive-target.write');
");
    }
}

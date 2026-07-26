using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售/采购/财务参数：页内子菜单全部独立权限（约定 system.params.{area}.{feature}.read|write）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260726080000_SeedParamsPageSubPermissions")]
    public partial class SeedParamsPageSubPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-00000000001a', 'system.params.sales.refresh-customer.read', '系统-销售参数-刷新客户-查看', 'api', 'system.params.sales.refresh-customer', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000001b', 'system.params.sales.refresh-customer.write', '系统-销售参数-刷新客户-维护', 'api', 'system.params.sales.refresh-customer', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000001c', 'system.params.purchase.assignee-count.read', '系统-采购参数-报价人数-查看', 'api', 'system.params.purchase.assignee-count', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000001d', 'system.params.purchase.assignee-count.write', '系统-采购参数-报价人数-维护', 'api', 'system.params.purchase.assignee-count', 'write', 1, NOW()),
('31000000-0000-4000-8000-00000000001e', 'system.params.purchase.quoter-pool.read', '系统-采购参数-报价员池-查看', 'api', 'system.params.purchase.quoter-pool', 'read', 1, NOW()),
('31000000-0000-4000-8000-00000000001f', 'system.params.purchase.quoter-pool.write', '系统-采购参数-报价员池-维护', 'api', 'system.params.purchase.quoter-pool', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000020', 'system.params.purchase.default-assign-method.read', '系统-采购参数-默认分配方式-查看', 'api', 'system.params.purchase.default-assign-method', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000021', 'system.params.purchase.default-assign-method.write', '系统-采购参数-默认分配方式-维护', 'api', 'system.params.purchase.default-assign-method', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000022', 'system.params.purchase.demand-protection.read', '系统-采购参数-需求保护-查看', 'api', 'system.params.purchase.demand-protection', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000023', 'system.params.purchase.demand-protection.write', '系统-采购参数-需求保护-维护', 'api', 'system.params.purchase.demand-protection', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000024', 'system.params.finance.exchange-rates.read', '系统-财务参数-汇率-查看', 'api', 'system.params.finance.exchange-rates', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000025', 'system.params.finance.exchange-rates.write', '系统-财务参数-汇率-维护', 'api', 'system.params.finance.exchange-rates', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000026', 'system.params.finance.purchase-cost-params.read', '系统-财务参数-采购系数-查看', 'api', 'system.params.finance.purchase-cost-params', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000027', 'system.params.finance.purchase-cost-params.write', '系统-财务参数-采购系数-维护', 'api', 'system.params.finance.purchase-cost-params', 'write', 1, NOW()),
('31000000-0000-4000-8000-000000000028', 'system.params.finance.payment-banks.read', '系统-财务参数-付款银行-查看', 'api', 'system.params.finance.payment-banks', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000029', 'system.params.finance.payment-banks.write', '系统-财务参数-付款银行-维护', 'api', 'system.params.finance.payment-banks', 'write', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

-- 刷新供应商若尚未存在则补齐（早期种子可能已有）
INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"") VALUES
('31000000-0000-4000-8000-000000000014', 'system.params.purchase.refresh-vendor.read', '系统-采购参数-刷新供应商-查看', 'api', 'system.params.purchase.refresh-vendor', 'read', 1, NOW()),
('31000000-0000-4000-8000-000000000015', 'system.params.purchase.refresh-vendor.write', '系统-采购参数-刷新供应商-维护', 'api', 'system.params.purchase.refresh-vendor', 'write', 1, NOW())
ON CONFLICT (""PermissionCode"") DO NOTHING;

-- SYS_ADMIN：全部 system.*
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r.""RoleCode"" = 'SYS_ADMIN'
  AND p.""PermissionCode"" LIKE 'system.params.%'
  AND (
    p.""PermissionCode"" LIKE 'system.params.sales.%.%'
    OR p.""PermissionCode"" LIKE 'system.params.purchase.%.%'
    OR p.""PermissionCode"" LIKE 'system.params.finance.%.%'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp.""RoleId"" = r.""RoleId"" AND rp.""PermissionId"" = p.""PermissionId""
  );

-- 兼容：已有模块 read → 对应子项 read；已有模块 write → 对应子项 write
INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
SELECT gen_random_uuid()::text, rp_exist.""RoleId"", p_new.""PermissionId"", NOW()
FROM sys_role_permission rp_exist
JOIN sys_permission p_old ON p_old.""PermissionId"" = rp_exist.""PermissionId""
JOIN sys_permission p_new ON (
  (p_old.""PermissionCode"" = 'system.params.sales.read' AND p_new.""PermissionCode"" = 'system.params.sales.refresh-customer.read')
  OR (p_old.""PermissionCode"" = 'system.params.sales.write' AND p_new.""PermissionCode"" = 'system.params.sales.refresh-customer.write')
  OR (p_old.""PermissionCode"" = 'system.params.purchase.read' AND p_new.""PermissionCode"" IN (
    'system.params.purchase.assignee-count.read',
    'system.params.purchase.quoter-pool.read',
    'system.params.purchase.default-assign-method.read',
    'system.params.purchase.demand-protection.read',
    'system.params.purchase.refresh-vendor.read'
  ))
  OR (p_old.""PermissionCode"" = 'system.params.purchase.write' AND p_new.""PermissionCode"" IN (
    'system.params.purchase.assignee-count.write',
    'system.params.purchase.quoter-pool.write',
    'system.params.purchase.default-assign-method.write',
    'system.params.purchase.demand-protection.write',
    'system.params.purchase.refresh-vendor.write'
  ))
  OR (p_old.""PermissionCode"" = 'system.params.finance.read' AND p_new.""PermissionCode"" IN (
    'system.params.finance.exchange-rates.read',
    'system.params.finance.purchase-cost-params.read',
    'system.params.finance.payment-banks.read'
  ))
  OR (p_old.""PermissionCode"" = 'system.params.finance.write' AND p_new.""PermissionCode"" IN (
    'system.params.finance.exchange-rates.write',
    'system.params.finance.purchase-cost-params.write',
    'system.params.finance.payment-banks.write'
  ))
)
WHERE NOT EXISTS (
  SELECT 1 FROM sys_role_permission rp
  WHERE rp.""RoleId"" = rp_exist.""RoleId"" AND rp.""PermissionId"" = p_new.""PermissionId""
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sys_role_permission WHERE ""PermissionId"" IN (
  SELECT ""PermissionId"" FROM sys_permission WHERE ""PermissionCode"" IN (
    'system.params.sales.refresh-customer.read','system.params.sales.refresh-customer.write',
    'system.params.purchase.assignee-count.read','system.params.purchase.assignee-count.write',
    'system.params.purchase.quoter-pool.read','system.params.purchase.quoter-pool.write',
    'system.params.purchase.default-assign-method.read','system.params.purchase.default-assign-method.write',
    'system.params.purchase.demand-protection.read','system.params.purchase.demand-protection.write',
    'system.params.finance.exchange-rates.read','system.params.finance.exchange-rates.write',
    'system.params.finance.purchase-cost-params.read','system.params.finance.purchase-cost-params.write',
    'system.params.finance.payment-banks.read','system.params.finance.payment-banks.write'
  )
);
DELETE FROM sys_permission WHERE ""PermissionCode"" IN (
  'system.params.sales.refresh-customer.read','system.params.sales.refresh-customer.write',
  'system.params.purchase.assignee-count.read','system.params.purchase.assignee-count.write',
  'system.params.purchase.quoter-pool.read','system.params.purchase.quoter-pool.write',
  'system.params.purchase.default-assign-method.read','system.params.purchase.default-assign-method.write',
  'system.params.purchase.demand-protection.read','system.params.purchase.demand-protection.write',
  'system.params.finance.exchange-rates.read','system.params.finance.exchange-rates.write',
  'system.params.finance.purchase-cost-params.read','system.params.finance.purchase-cost-params.write',
  'system.params.finance.payment-banks.read','system.params.finance.payment-banks.write'
);
");
        }
    }
}

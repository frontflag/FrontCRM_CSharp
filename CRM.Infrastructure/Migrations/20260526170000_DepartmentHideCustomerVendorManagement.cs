using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>部门表：销售/采购范围下「隐藏客户管理」「隐藏供应商管理」开关。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260526170000_DepartmentHideCustomerVendorManagement")]
    public partial class DepartmentHideCustomerVendorManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""HideCustomerManagement"" boolean NOT NULL DEFAULT false;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""HideVendorManagement"" boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.sys_department.""HideCustomerManagement"" IS '隐藏客户管理菜单并拦截客户模块（与 SaleDataScope 独立）';
COMMENT ON COLUMN public.sys_department.""HideVendorManagement"" IS '隐藏供应商管理菜单并拦截供应商模块（与 PurchaseDataScope 独立）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""HideCustomerManagement"";
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""HideVendorManagement"";
");
        }
    }
}

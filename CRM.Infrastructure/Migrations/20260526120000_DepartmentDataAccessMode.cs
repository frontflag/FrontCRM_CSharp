using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>部门表增加销售/采购数据「只读 vs 读写」配置（与数据范围正交）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260526120000_DepartmentDataAccessMode")]
    public partial class DepartmentDataAccessMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""SaleDataAccess"" smallint NOT NULL DEFAULT 0;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""PurchaseDataAccess"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sys_department.""SaleDataAccess"" IS '销售数据访问：0读写 1只读（与 SaleDataScope 独立）';
COMMENT ON COLUMN public.sys_department.""PurchaseDataAccess"" IS '采购数据访问：0读写 1只读（与 PurchaseDataScope 独立）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""SaleDataAccess"";
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""PurchaseDataAccess"";
");
        }
    }
}

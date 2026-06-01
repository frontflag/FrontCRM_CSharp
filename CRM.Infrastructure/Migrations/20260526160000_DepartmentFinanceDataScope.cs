using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>部门表增加财务数据范围与只读/读写（付款管理/收款管理菜单组）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260526160000_DepartmentFinanceDataScope")]
    public partial class DepartmentFinanceDataScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""FinanceDataScope"" smallint NOT NULL DEFAULT 0;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""FinanceDataAccess"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sys_department.""FinanceDataScope"" IS '财务数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止';
COMMENT ON COLUMN public.sys_department.""FinanceDataAccess"" IS '财务数据访问：0读写 1只读（与 FinanceDataScope 独立）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""FinanceDataScope"";
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""FinanceDataAccess"";
");
        }
    }
}

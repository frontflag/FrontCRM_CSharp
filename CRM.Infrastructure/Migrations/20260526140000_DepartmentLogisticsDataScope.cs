using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>部门表增加物流数据范围与只读/读写（入库/出库/库存/报关菜单组）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260526140000_DepartmentLogisticsDataScope")]
    public partial class DepartmentLogisticsDataScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""LogisticsDataScope"" smallint NOT NULL DEFAULT 0;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS ""LogisticsDataAccess"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sys_department.""LogisticsDataScope"" IS '物流数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止';
COMMENT ON COLUMN public.sys_department.""LogisticsDataAccess"" IS '物流数据访问：0读写 1只读（与 LogisticsDataScope 独立）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""LogisticsDataScope"";
ALTER TABLE public.sys_department DROP COLUMN IF EXISTS ""LogisticsDataAccess"";
");
        }
    }
}

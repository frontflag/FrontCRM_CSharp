using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// pickingtaskitem：是否备货补充拣货（与销售关联采购类型池之外的备货批次）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260421100000_AddPickingTaskItemStockingSupplement")]
    public partial class AddPickingTaskItemStockingSupplement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.pickingtaskitem ADD COLUMN IF NOT EXISTS ""IsStockingSupplement"" boolean NOT NULL DEFAULT false;
COMMENT ON COLUMN public.pickingtaskitem.""IsStockingSupplement"" IS '备货补充拣货：true=按销单行型号品牌匹配的备货库存';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.pickingtaskitem DROP COLUMN IF EXISTS ""IsStockingSupplement"";");
        }
    }
}

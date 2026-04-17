using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 质检主表增加计划入库日，供从质检生成入库单时写入 StockIn.StockInDate（替代前端误用「当前时间」）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260623100000_QcInfoStockInPlanDate")]
    public partial class QcInfoStockInPlanDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.qcinfo
  ADD COLUMN IF NOT EXISTS ""StockInPlanDate"" timestamp with time zone NULL;
COMMENT ON COLUMN public.qcinfo.""StockInPlanDate"" IS '质检保存时填写的计划入库日；质检列表生成入库单时作为入库日期';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS ""StockInPlanDate"";
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// RFQ 主状态：废弃 6（旧「已关闭」），统一为 7；并更新列注释与 sys_dict 风格说明对齐。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260426180000_NormalizeRfqStatus6To7AndComment")]
    public partial class NormalizeRfqStatus6To7AndComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.rfq SET status = 7 WHERE status = 6;
COMMENT ON COLUMN public.rfq.status IS '主状态：0待分配 1已分配 2报价中 3已报价 4已选价 5已转订单 6废弃(迁移至7) 7已关闭 8已取消';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 无法区分 7 中哪些由 6 迁移而来，不回滚数据；仅恢复旧版注释口径（仍含 6 语义）。
            migrationBuilder.Sql(@"
COMMENT ON COLUMN public.rfq.status IS '状态：0待分配 1已分配 2报价中 3已报价 4已选价 5已转订单 6已关闭';
");
        }
    }
}

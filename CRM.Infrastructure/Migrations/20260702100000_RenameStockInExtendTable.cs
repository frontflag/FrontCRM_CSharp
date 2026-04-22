using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 入库主单扩展表 <c>stockinextend</c> 重命名为 <c>stock_in_extend</c>（与库存链 snake_case 一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260702100000_RenameStockInExtendTable")]
    public partial class RenameStockInExtendTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinextend RENAME TO stock_in_extend;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_in_extend RENAME TO stockinextend;
");
        }
    }
}

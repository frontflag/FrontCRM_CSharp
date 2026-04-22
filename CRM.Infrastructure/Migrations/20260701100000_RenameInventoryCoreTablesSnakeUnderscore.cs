using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 将库存核心物理表名改为 snake_case 下划线形式（与脚本 scripts/rename_inventory_core_tables_postgresql.sql 一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260701100000_RenameInventoryCoreTablesSnakeUnderscore")]
    public partial class RenameInventoryCoreTablesSnakeUnderscore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinitemextend RENAME TO stock_in_item_extend;
ALTER TABLE IF EXISTS public.stockinitem RENAME TO stock_in_item;
ALTER TABLE IF EXISTS public.stockinextend RENAME TO stock_in_extend;
ALTER TABLE IF EXISTS public.stockin RENAME TO stock_in;
ALTER TABLE IF EXISTS public.stockitem RENAME TO stock_item;
ALTER TABLE IF EXISTS public.stockoutitemextend RENAME TO stock_out_item_extend;
ALTER TABLE IF EXISTS public.stockoutitem RENAME TO stock_out_item;
ALTER TABLE IF EXISTS public.stockout RENAME TO stock_out;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out RENAME TO stockout;
ALTER TABLE IF EXISTS public.stock_out_item RENAME TO stockoutitem;
ALTER TABLE IF EXISTS public.stock_out_item_extend RENAME TO stockoutitemextend;
ALTER TABLE IF EXISTS public.stock_item RENAME TO stockitem;
ALTER TABLE IF EXISTS public.stock_in RENAME TO stockin;
ALTER TABLE IF EXISTS public.stock_in_extend RENAME TO stockinextend;
ALTER TABLE IF EXISTS public.stock_in_item RENAME TO stockinitem;
ALTER TABLE IF EXISTS public.stock_in_item_extend RENAME TO stockinitemextend;
");
        }
    }
}

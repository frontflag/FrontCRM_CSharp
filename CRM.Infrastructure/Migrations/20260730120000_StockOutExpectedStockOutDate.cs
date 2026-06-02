using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>出库单主表：预计出库日期（批量出库时用户指定）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260730120000_StockOutExpectedStockOutDate")]
    public partial class StockOutExpectedStockOutDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out
  ADD COLUMN IF NOT EXISTS expected_stock_out_date timestamp with time zone NULL;

COMMENT ON COLUMN public.stock_out.expected_stock_out_date IS '预计出库日期（timestamptz，存 UTC；批量出库时由用户指定）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out DROP COLUMN IF EXISTS expected_stock_out_date;
");
        }
    }
}

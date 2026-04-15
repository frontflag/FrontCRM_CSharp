using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockout：补充整型列 <c>Type</c>（与既有 <c>StockOutType</c> 出库类型列并存）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260516100000_StockOutTypeIntColumn")]
    public partial class StockOutTypeIntColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout
  ADD COLUMN IF NOT EXISTS ""Type"" integer NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.stockout.""Type"" IS '整型分类（与 StockOutType 出库类型独立；默认 0）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""Type"";");
        }
    }
}

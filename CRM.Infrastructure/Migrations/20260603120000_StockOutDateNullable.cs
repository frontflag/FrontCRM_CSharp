using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>出库单主表：实际出库日期改为可空（批量生成时不写入，标记完成时填写）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603120000_StockOutDateNullable")]
    public partial class StockOutDateNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_out
  ALTER COLUMN ""StockOutDate"" DROP NOT NULL;

-- 准备出库(2) 状态的出库单不应有实际出库日期；清理历史误写入的创建时间
UPDATE public.stock_out
SET ""StockOutDate"" = NULL
WHERE ""Status"" = 2;

COMMENT ON COLUMN public.stock_out.""StockOutDate"" IS '实际出库日期（timestamptz，存 UTC；标记完成时填写，批量生成出库单时不自动写入）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.stock_out SET ""StockOutDate"" = COALESCE(""StockOutDate"", ""CreateTime"") WHERE ""StockOutDate"" IS NULL;

ALTER TABLE IF EXISTS public.stock_out
  ALTER COLUMN ""StockOutDate"" SET NOT NULL;
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stock：业务编号 StockCode（历史可空；新行由流水号服务赋值）；非空值唯一。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260430120000_StockStockCode")]
    public partial class StockStockCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""StockCode"" character varying(32) NULL;
COMMENT ON COLUMN public.stock.""StockCode"" IS '库存业务编号 STK+5位32进制；历史行可空';
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_StockCode_unique_not_null""
  ON public.stock (""StockCode"")
  WHERE ""StockCode"" IS NOT NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_stock_StockCode_unique_not_null"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""StockCode"";
");
        }
    }
}

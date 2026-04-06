using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stock：库存类型 Type（smallint），与销售/采购订单类型 1/2/3 对齐。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260420120000_AddStockInventoryType")]
    public partial class AddStockInventoryType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""Type"" smallint NOT NULL DEFAULT 1;
COMMENT ON COLUMN public.stock.""Type"" IS '库存类型：1客单库存 2备货库存 3样品库存';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""Type"";");
        }
    }
}

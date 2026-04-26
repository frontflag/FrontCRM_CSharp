using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 在库明细 <c>stock_item.TransferType</c>：手工移库源行整行出清后打标 10，供库存列表过滤。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260426190000_StockItemTransferType")]
    public partial class StockItemTransferType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_item ADD COLUMN IF NOT EXISTS ""TransferType"" smallint NULL;
COMMENT ON COLUMN public.stock_item.""TransferType"" IS '手工移库源行整行出清后=10；列表默认不展示。';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock_item DROP COLUMN IF EXISTS ""TransferType"";
");
        }
    }
}

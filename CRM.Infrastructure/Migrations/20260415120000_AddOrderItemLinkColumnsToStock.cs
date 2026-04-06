using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stock：冗余采购/销售明细行（过账入库时从 stockin 头写入）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260415120000_AddOrderItemLinkColumnsToStock")]
    public partial class AddOrderItemLinkColumnsToStock : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""SellOrderItemId"";
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// inventoryledger：冗余采购/销售明细行（与 stock 一致，入库/出库/盘点流水写入时复制）。
    /// 说明：项目中库存进出流水表为 inventoryledger，无独立 stockinout 物理表名。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260415130000_AddOrderItemLinkColumnsToInventoryLedger")]
    public partial class AddOrderItemLinkColumnsToInventoryLedger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""SellOrderItemId"";
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 入库单头表去掉采销行快照列（改由 <c>stockinitemextend</c> 按明细承载）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260629130000_RemoveStockInOrderLineSnapshotColumns")]
    public partial class RemoveStockInOrderLineSnapshotColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_code;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS sell_order_item_id character varying(36) NULL;
");
        }
    }
}

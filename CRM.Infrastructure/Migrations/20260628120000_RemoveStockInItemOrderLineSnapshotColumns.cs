using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 入库明细表去掉与头表重复的采销行快照列（改由 stockin / stockinitemextend 承载）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260628120000_RemoveStockInItemOrderLineSnapshotColumns")]
    public partial class RemoveStockInItemOrderLineSnapshotColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_order_item_code;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS sell_order_item_id character varying(36) NULL;
");
        }
    }
}

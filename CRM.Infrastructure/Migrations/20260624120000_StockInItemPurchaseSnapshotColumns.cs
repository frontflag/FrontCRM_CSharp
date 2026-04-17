using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 入库明细表冗余采购/销售行快照：PN、品牌、采购/销售明细编号与主键、币别（与 stockoutitem / stockitem 对齐）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260624120000_StockInItemPurchaseSnapshotColumns")]
    public partial class StockInItemPurchaseSnapshotColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_pn character varying(200) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_brand character varying(200) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS sell_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockinitem ADD COLUMN IF NOT EXISTS currency smallint NULL;

COMMENT ON COLUMN public.stockinitem.purchase_pn IS '采购明细 PN 快照';
COMMENT ON COLUMN public.stockinitem.purchase_brand IS '采购明细品牌快照';
COMMENT ON COLUMN public.stockinitem.purchase_order_item_code IS '采购明细业务编号快照';
COMMENT ON COLUMN public.stockinitem.purchase_order_item_id IS '采购明细主键快照';
COMMENT ON COLUMN public.stockinitem.sell_order_item_code IS '销售明细业务编号快照';
COMMENT ON COLUMN public.stockinitem.sell_order_item_id IS '销售明细主键快照';
COMMENT ON COLUMN public.stockinitem.currency IS '采购币别（与 purchaseorderitem.currency 一致）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS currency;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_brand;
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS purchase_pn;
");
        }
    }
}

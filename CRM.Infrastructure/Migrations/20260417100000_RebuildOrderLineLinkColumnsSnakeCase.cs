using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stock / stockin / inventoryledger：删除 Pascal 与重复 snake 列后，统一为与 purchaseorderitem 一致的 snake_case 四列。
    /// 适用空库或可不保留冗余列数据的场景；有数据时请先做备份或自行 UPDATE 回填。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260417100000_RebuildOrderLineLinkColumnsSnakeCase")]
    public partial class RebuildOrderLineLinkColumnsSnakeCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- ========== stock ==========
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""SellOrderItemId"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS sell_order_item_id;

ALTER TABLE IF EXISTS public.stock ADD COLUMN purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN sell_order_item_id character varying(36) NULL;

-- ========== inventoryledger ==========
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS ""SellOrderItemId"";
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS sell_order_item_id;

ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN sell_order_item_id character varying(36) NULL;

-- ========== stockin ==========
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SellOrderItemId"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_id;

ALTER TABLE IF EXISTS public.stockin ADD COLUMN purchase_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN purchase_order_item_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN sell_order_item_id character varying(36) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 回滚为历史迁移中的 Pascal 列（仅结构，数据不恢复）
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;

ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.inventoryledger DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.inventoryledger ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;

ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS purchase_order_item_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_code;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS sell_order_item_id;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;
");
        }
    }
}

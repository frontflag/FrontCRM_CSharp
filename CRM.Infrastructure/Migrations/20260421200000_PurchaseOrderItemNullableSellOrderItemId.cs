using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 备货/手工采购明细无销售行：sell_order_item_id 允许 NULL，避免占位 GUID 违反 FK。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260421200000_PurchaseOrderItemNullableSellOrderItemId")]
    public partial class PurchaseOrderItemNullableSellOrderItemId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.purchaseorderitem
  DROP CONSTRAINT IF EXISTS ""FK_purchaseorderitem_sellorderitem_sell_order_item_id"";

UPDATE public.purchaseorderitem
SET sell_order_item_id = NULL
WHERE lower(trim(sell_order_item_id)) = '00000000-0000-0000-0000-000000000000';

ALTER TABLE public.purchaseorderitem
  ALTER COLUMN sell_order_item_id DROP NOT NULL;

ALTER TABLE public.purchaseorderitem
  ADD CONSTRAINT ""FK_purchaseorderitem_sellorderitem_sell_order_item_id""
  FOREIGN KEY (sell_order_item_id) REFERENCES public.sellorderitem (""SellOrderItemId"")
  ON DELETE RESTRICT;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 无安全回滚：恢复 NOT NULL 需先为每条 NULL 明细填入真实 sellorderitem 主键。
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260405120000_SellOrderItemExtendProgressP0")]
    public partial class SellOrderItemExtendProgressP0 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""QtyAlreadyPurchased"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""QtyNotPurchase"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""QtyStockOutActual"" numeric(18,4) NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.financereceiptitem
    ADD COLUMN IF NOT EXISTS ""VerifiedAmount"" numeric(18,2) NOT NULL DEFAULT 0;

-- 历史：已核销完成的收款明细，累计核销额视为全额
UPDATE public.financereceiptitem
SET ""VerifiedAmount"" = ""ReceiptConvertAmount""
WHERE ""VerificationStatus"" = 2 AND (""VerifiedAmount"" IS NULL OR ""VerifiedAmount"" = 0);

-- 采购进度 + 主表 PurchasedQty（purchaseorderitem 列为 sell_order_item_id / qty；sellorderitem 为 purchased_qty / qty）
UPDATE public.sellorderitem i
SET purchased_qty = COALESCE(sub.sum_po, 0)
FROM (
    SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
    FROM public.purchaseorderitem
    GROUP BY sell_order_item_id
) sub
WHERE sub.sell_order_item_id = i.""SellOrderItemId"";

UPDATE public.sellorderitemextend e
SET
    ""QtyAlreadyPurchased"" = COALESCE(po.sum_po, 0),
    ""QtyNotPurchase"" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(po.sum_po, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
    FROM public.purchaseorderitem
    GROUP BY sell_order_item_id
) po ON po.sell_order_item_id = i.""SellOrderItemId""
WHERE e.""SellOrderItemId"" = i.""SellOrderItemId"";

-- 出库通知进度（Status<>2 计入已占用通知量）
UPDATE public.sellorderitemextend e
SET
    ""QtyStockOutNotify"" = COALESCE(nr.sum_q, 0),
    ""QtyStockOutNotifyNot"" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(nr.sum_q, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT ""SalesOrderItemId"" AS sid, SUM(""Quantity"")::numeric AS sum_q
    FROM public.stockoutrequest
    WHERE ""Status"" IS DISTINCT FROM 2
    GROUP BY ""SalesOrderItemId""
) nr ON nr.sid = i.""SellOrderItemId""
WHERE e.""SellOrderItemId"" = i.""SellOrderItemId"";

-- 实出数量：已出库通知(Status=1) 且 存在已确认出库单(SourceId=通知主键, Status=2)
UPDATE public.sellorderitemextend e
SET ""QtyStockOutActual"" = COALESCE(sa.sum_out, 0)
FROM public.sellorderitem i
LEFT JOIN (
    SELECT r.""SalesOrderItemId"" AS sid, SUM(so.""TotalQuantity"")::numeric AS sum_out
    FROM public.stockoutrequest r
    INNER JOIN public.stockout so
        ON so.""SourceId"" = r.""UserId"" AND so.""Status"" = 2
    WHERE r.""Status"" = 1
    GROUP BY r.""SalesOrderItemId""
) sa ON sa.sid = i.""SellOrderItemId""
WHERE e.""SellOrderItemId"" = i.""SellOrderItemId"";

-- 收款核销汇总到扩展表
UPDATE public.sellorderitemextend e
SET
    ""ReceiptAmountFinish"" = COALESCE(rv.sum_v, 0),
    ""ReceiptAmountNot"" = GREATEST(0::numeric, e.""ReceiptAmount"" - COALESCE(rv.sum_v, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT ""SellOrderItemId"" AS sid, SUM(""VerifiedAmount"")::numeric AS sum_v
    FROM public.financereceiptitem
    WHERE ""SellOrderItemId"" IS NOT NULL AND BTRIM(""SellOrderItemId"") <> ''
    GROUP BY ""SellOrderItemId""
) rv ON rv.sid = i.""SellOrderItemId""
WHERE e.""SellOrderItemId"" = i.""SellOrderItemId"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QtyStockOutActual"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QtyNotPurchase"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""QtyAlreadyPurchased"";
ALTER TABLE IF EXISTS public.financereceiptitem DROP COLUMN IF EXISTS ""VerifiedAmount"";
");
        }
    }
}

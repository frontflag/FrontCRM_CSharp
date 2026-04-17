-- 与 Migration 20260405120000_SellOrderItemExtendProgressP0 等价，但修正了列名：
--   purchaseorderitem 使用 sell_order_item_id、qty（不是 "SellOrderItemId"、"Qty"）
--   sellorderitem 使用 purchased_qty、qty（不是 "PurchasedQty"、"Qty"）
-- 在 DbMigrator 仍失败时可手工执行；若此前迁移已部分执行，本脚本多为幂等（ADD COLUMN IF NOT EXISTS）。
-- PostgreSQL

ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS "QtyAlreadyPurchased" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS "QtyNotPurchase" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS "QtyStockOutActual" numeric(18,4) NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS public.financereceiptitem
    ADD COLUMN IF NOT EXISTS "VerifiedAmount" numeric(18,2) NOT NULL DEFAULT 0;

UPDATE public.financereceiptitem
SET "VerifiedAmount" = "ReceiptConvertAmount"
WHERE "VerificationStatus" = 2 AND ("VerifiedAmount" IS NULL OR "VerifiedAmount" = 0);

UPDATE public.sellorderitem i
SET purchased_qty = COALESCE(sub.sum_po, 0)
FROM (
    SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
    FROM public.purchaseorderitem
    GROUP BY sell_order_item_id
) sub
WHERE sub.sell_order_item_id = i."SellOrderItemId";

UPDATE public.sellorderitemextend e
SET
    "QtyAlreadyPurchased" = COALESCE(po.sum_po, 0),
    "QtyNotPurchase" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(po.sum_po, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT sell_order_item_id, SUM(qty)::numeric AS sum_po
    FROM public.purchaseorderitem
    GROUP BY sell_order_item_id
) po ON po.sell_order_item_id = i."SellOrderItemId"
WHERE e."SellOrderItemId" = i."SellOrderItemId";

UPDATE public.sellorderitemextend e
SET
    "QtyStockOutNotify" = COALESCE(nr.sum_q, 0),
    "QtyStockOutNotifyNot" = GREATEST(0::numeric, COALESCE(i.qty, 0) - COALESCE(nr.sum_q, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT "SalesOrderItemId" AS sid, SUM("Quantity")::numeric AS sum_q
    FROM public.stockoutrequest
    WHERE "Status" IS DISTINCT FROM 2
    GROUP BY "SalesOrderItemId"
) nr ON nr.sid = i."SellOrderItemId"
WHERE e."SellOrderItemId" = i."SellOrderItemId";

UPDATE public.sellorderitemextend e
SET "QtyStockOutActual" = COALESCE(sa.sum_out, 0)
FROM public.sellorderitem i
LEFT JOIN (
    SELECT r."SalesOrderItemId" AS sid, SUM(so."TotalQuantity")::numeric AS sum_out
    FROM public.stockoutrequest r
    INNER JOIN public.stock_out so
        ON so."SourceId" = r."UserId" AND so."Status" = 2
    WHERE r."Status" = 1
    GROUP BY r."SalesOrderItemId"
) sa ON sa.sid = i."SellOrderItemId"
WHERE e."SellOrderItemId" = i."SellOrderItemId";

UPDATE public.sellorderitemextend e
SET
    "ReceiptAmountFinish" = COALESCE(rv.sum_v, 0),
    "ReceiptAmountNot" = GREATEST(0::numeric, e."ReceiptAmount" - COALESCE(rv.sum_v, 0))
FROM public.sellorderitem i
LEFT JOIN (
    SELECT "SellOrderItemId" AS sid, SUM("VerifiedAmount")::numeric AS sum_v
    FROM public.financereceiptitem
    WHERE "SellOrderItemId" IS NOT NULL AND BTRIM("SellOrderItemId") <> ''
    GROUP BY "SellOrderItemId"
) rv ON rv.sid = i."SellOrderItemId"
WHERE e."SellOrderItemId" = i."SellOrderItemId";


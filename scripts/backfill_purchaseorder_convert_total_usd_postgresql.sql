-- Backfill purchaseorder.convert_total as USD: SUM(ROUND(qty * convert_price, 2)) per active line.
-- Run after deploying PurchaseOrderService convert_total fix.

UPDATE purchaseorder o
SET convert_total = COALESCE(sub.usd_total, 0)
FROM (
    SELECT
        i.purchase_order_id,
        SUM(ROUND((i.qty * i.convert_price)::numeric, 2)) AS usd_total
    FROM purchaseorderitem i
    WHERE COALESCE(i.is_deleted, false) = false
    GROUP BY i.purchase_order_id
) sub
WHERE o."PurchaseOrderId" = sub.purchase_order_id;

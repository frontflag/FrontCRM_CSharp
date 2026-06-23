-- Backfill sellorder.convert_total as USD: SUM(ROUND(qty * convert_price, 2)) per active line.
-- Run after deploying SalesOrderService convert_total fix.

UPDATE sellorder o
SET convert_total = COALESCE(sub.usd_total, 0)
FROM (
    SELECT
        i.sell_order_id,
        SUM(ROUND((i.qty * i.convert_price)::numeric, 2)) AS usd_total
    FROM sellorderitem i
    WHERE COALESCE(i.is_deleted, false) = false
      AND i.status = 0
    GROUP BY i.sell_order_id
) sub
WHERE o."SellOrderId" = sub.sell_order_id;

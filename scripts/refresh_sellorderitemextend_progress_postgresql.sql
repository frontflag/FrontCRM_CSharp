-- =============================================================================
-- 销售明细扩展进度未刷新 → 列表仍显示「待采购/待入库」、无法「申请出库」
--
-- 典型场景：硬删除后手工 INSERT sellorderitemextend，只恢复了金额/数量默认值，
--           未按 PO / 入库单重算 PurchaseProgressStatus、StockInProgressStatus。
--
-- 申请出库条件（前端 salesOrderStatus.ts）：
--   1) stockOutApplyPurchaseGateOk：关联 PO 主表 status >= 30（已确认）
--   2) purchaseProgressStatus <> 0（非「待采购」），或 PurchasedStock_AvailableQty > 0（备货池）
--
-- 修复（推荐）：销售订单详情页点「刷新」，或
--   POST /api/v1/sales-orders/{SellOrderId}/refresh-item-extends
--   SO0020L → SellOrderId = cd27f4d2-0c04-47cc-b873-c52e4dab9904
--
-- 无 API 时：执行下方「二、重算」SQL（与 SellOrderItemExtendSyncService 采购/入库口径一致）
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 零、全库扫描：扩展仍为「待采购」，但 PO 已有关联数量（补回 extend 后常见）
-- -----------------------------------------------------------------------------
SELECT
    so.sell_order_code,
    soi.sell_order_item_code,
    soi."SellOrderItemId",
    soi.qty,
    ext."PurchaseProgressStatus",
    ext."StockInProgressStatus",
    ext."QtyAlreadyPurchased",
    COALESCE(po_sum.sum_po, 0) AS po_qty_sum,
    COALESCE(si_sum.sum_in, 0) AS stock_in_qty_sum
FROM public.sellorderitem soi
JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
LEFT JOIN (
    SELECT sell_order_item_id, SUM(qty) AS sum_po
    FROM public.purchaseorderitem
    WHERE sell_order_item_id IS NOT NULL AND TRIM(sell_order_item_id) <> ''
    GROUP BY sell_order_item_id
) po_sum ON po_sum.sell_order_item_id = soi."SellOrderItemId"
LEFT JOIN (
    SELECT siext.sell_order_item_id, SUM(sii."Quantity") AS sum_in
    FROM public.stock_in_item_extend siext
    JOIN public.stock_in_item sii ON sii."ItemId" = siext."StockInItemId"
    JOIN public.stock_in sin
        ON sin."StockInId" = sii."StockInId"
       AND sin."Status" = 2
       AND sin."StockInType" = 10
       AND COALESCE(sin.is_deleted, false) = false
    WHERE siext.sell_order_item_id IS NOT NULL
      AND COALESCE(siext.is_deleted, false) = false
    GROUP BY siext.sell_order_item_id
) si_sum ON si_sum.sell_order_item_id = soi."SellOrderItemId"
WHERE COALESCE(ext."PurchaseProgressStatus", 0) = 0
  AND COALESCE(po_sum.sum_po, 0) > 0
ORDER BY so.sell_order_code, soi.sell_order_item_code;

-- -----------------------------------------------------------------------------
-- 一、诊断 SO0020L（销行 b7fabdc1-fa63-4377-90a8-5440fde96f25 / SO0020L-3）
-- -----------------------------------------------------------------------------
SELECT
    soi."SellOrderItemId",
    soi.sell_order_item_code,
    soi.qty AS line_qty,
    soi.purchased_qty,
    ext."QtyAlreadyPurchased",
    ext."QtyStockOutNotifyNot",
    ext."PurchaseProgressStatus",
    ext."StockInProgressStatus",
    ext."StockOutProgressStatus",
    ext."PurchasedStock_AvailableQty",
    po.purchase_order_code,
    po.status AS po_header_status,
    CASE WHEN po.status >= 30 THEN true ELSE false END AS purchase_gate_ok,
    sin."StockInCode",
    sin."Status" AS stock_in_status,
    sin."StockInType"
FROM public.sellorderitem soi
JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
LEFT JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
LEFT JOIN public.purchaseorderitem poi ON poi.sell_order_item_id = soi."SellOrderItemId"
LEFT JOIN public.purchaseorder po ON po."PurchaseOrderId" = poi.purchase_order_id
LEFT JOIN public.stock_in_item_extend siext ON siext.sell_order_item_id = soi."SellOrderItemId"
LEFT JOIN public.stock_in sin ON sin."StockInId" = siext."StockInId"
WHERE so.sell_order_code = 'SO0020L'
  AND soi."SellOrderItemId" = 'b7fabdc1-fa63-4377-90a8-5440fde96f25';

-- -----------------------------------------------------------------------------
-- 二、重算单条销行扩展（按 SellOrderItemId；可改 WHERE 条件批量处理）
-- -----------------------------------------------------------------------------
BEGIN;

WITH line AS (
    SELECT soi."SellOrderItemId" AS id, soi.qty
    FROM public.sellorderitem soi
    WHERE soi."SellOrderItemId" = 'b7fabdc1-fa63-4377-90a8-5440fde96f25'
),
po_agg AS (
    SELECT l.id,
           COALESCE(SUM(poi.qty), 0)::numeric(18,4) AS sum_po
    FROM line l
    LEFT JOIN public.purchaseorderitem poi ON poi.sell_order_item_id = l.id
    GROUP BY l.id
),
notify_agg AS (
    SELECT l.id,
           COALESCE(SUM(sor."Quantity"), 0)::numeric(18,4) AS sum_notify
    FROM line l
    LEFT JOIN public.stockout_notify sor
        ON sor."SalesOrderItemId" = l.id
       AND sor."Status" <> 2
       AND COALESCE(sor.is_deleted, false) = false
    GROUP BY l.id
),
si_agg AS (
    SELECT l.id,
           COALESCE(SUM(sii."Quantity"), 0)::numeric(18,4) AS sum_in
    FROM line l
    INNER JOIN public.stock_in_item_extend siext
        ON siext.sell_order_item_id = l.id AND COALESCE(siext.is_deleted, false) = false
    INNER JOIN public.stock_in_item sii ON sii."ItemId" = siext."StockInItemId"
    INNER JOIN public.stock_in sin
        ON sin."StockInId" = sii."StockInId"
       AND sin."Status" = 2
       AND sin."StockInType" = 10
       AND COALESCE(sin.is_deleted, false) = false
    GROUP BY l.id
)
UPDATE public.sellorderitemextend ext
SET
    "QtyAlreadyPurchased" = p.sum_po,
    "QtyNotPurchase" = GREATEST(0::numeric, l.qty - p.sum_po),
    "QtyStockOutNotify" = n.sum_notify,
    "QtyStockOutNotifyNot" = GREATEST(0::numeric, l.qty - n.sum_notify),
    "PurchaseProgressStatus" = CASE
        WHEN p.sum_po <= 0 THEN 0::smallint
        WHEN p.sum_po + 0.0000001 >= l.qty THEN 2::smallint
        ELSE 1::smallint
    END,
    "StockInProgressStatus" = CASE
        WHEN COALESCE(s.sum_in, 0) <= 0 THEN 0::smallint
        WHEN COALESCE(s.sum_in, 0) + 0.0000001 >= l.qty THEN 2::smallint
        ELSE 1::smallint
    END,
    "ModifyTime" = NOW() AT TIME ZONE 'UTC'
FROM line l
JOIN po_agg p ON p.id = l.id
JOIN notify_agg n ON n.id = l.id
LEFT JOIN si_agg s ON s.id = l.id
WHERE ext."SellOrderItemId" = l.id;

UPDATE public.sellorderitem soi
SET purchased_qty = sub.sum_po,
    "ModifyTime" = NOW() AT TIME ZONE 'UTC'
FROM (
    SELECT poi.sell_order_item_id AS id, COALESCE(SUM(poi.qty), 0)::numeric(18,4) AS sum_po
    FROM public.purchaseorderitem poi
    WHERE poi.sell_order_item_id = 'b7fabdc1-fa63-4377-90a8-5440fde96f25'
    GROUP BY poi.sell_order_item_id
) sub
WHERE soi."SellOrderItemId" = sub.id;

COMMIT;

-- 三、复检：PurchaseProgressStatus / StockInProgressStatus 应 >= 1；po_header_status >= 30
-- 重新执行「一、诊断」

-- =============================================================================
-- 销售明细扩展：批量重算出库利润（ProfitOutBizUsd / ProfitOutRateBiz）
--
-- 口径（与 SellOrderOutboundProfitCalc、SellOrderItemExtendSyncService 一致）：
--   1) 优先：已完成销售出库单下 stock_out_item_extend 的真实采购单价 USD × 出库数量
--      利润 = Σ extend."ProfitOutBizUsd"（全为 0 且成本 > 0 时用 销售收入 − 实际成本）
--   2) 回退：无可用批次快照时，PO 加权均价 × 已出库数量
--
-- 推荐（在线）：
--   Debug → 数据 →「刷新出库利润」
--   或销售订单详情 →「刷新」：POST /api/v1/sales-orders/{SellOrderId}/refresh-item-extends
-- 本脚本适用于：离线批量修复历史数据、部署新口径后一次性对齐 extend 表。
--
-- 执行前请先跑「一、预览」；确认后再执行「二、批量更新」。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 一、预览：新旧出库利润差异（仅展示有已出库数量的明细）
-- -----------------------------------------------------------------------------
WITH lines AS (
    SELECT
        so.sell_order_code,
        soi.sell_order_item_code,
        soi."SellOrderItemId" AS id,
        soi.convert_price,
        ext."QtyStockOutActual" AS out_qty,
        ext."ProfitOutBizUsd" AS old_profit_usd,
        ext."ProfitOutRateBiz" AS old_rate
    FROM public.sellorderitem soi
    JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
    JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
    WHERE COALESCE(ext.is_deleted, false) = false
      AND COALESCE(soi.is_deleted, false) = false
      -- 可选：限定单张销售订单
      -- AND so.sell_order_code = 'SO0020L'
      -- 可选：限定单条明细
      -- AND soi."SellOrderItemId" = 'b7fabdc1-fa63-4377-90a8-5440fde96f25'
),
po_weighted AS (
    SELECT
        l.id,
        COALESCE(SUM(poi.qty), 0)::numeric(18,4) AS po_qty_total,
        CASE
            WHEN COALESCE(SUM(poi.qty), 0) > 0
                THEN ROUND(SUM(poi.qty * poi.convert_price) / SUM(poi.qty), 6)
            ELSE 0::numeric(18,6)
        END AS avg_po_cost_usd
    FROM lines l
    LEFT JOIN public.purchaseorderitem poi ON poi.sell_order_item_id = l.id
    GROUP BY l.id
),
outbound_ext AS (
    SELECT
        l.id,
        SUM(
            CASE
                WHEN e."QtyStockOut" > 0 THEN e."QtyStockOut"
                WHEN soi."ActualQty" > 0 THEN soi."ActualQty"
                ELSE soi."Quantity"
            END
        )::numeric(18,4) AS ext_qty_sum,
        SUM(
            (
                CASE
                    WHEN e."QtyStockOut" > 0 THEN e."QtyStockOut"
                    WHEN soi."ActualQty" > 0 THEN soi."ActualQty"
                    ELSE soi."Quantity"
                END
            ) * e."PurchasePriceUsd"
        )::numeric(18,6) AS actual_cost_raw,
        SUM(e."ProfitOutBizUsd")::numeric(18,2) AS profit_sum_raw,
        BOOL_OR(e."PurchasePriceUsd" > 0 OR e."ProfitOutBizUsd" <> 0) AS has_usable_actual
    FROM lines l
    INNER JOIN public.stock_out so
        ON so."SellOrderItemId" = l.id
       AND so."Status" IN (2, 4)
       AND so."StockOutType" = 10
       AND COALESCE(so.is_deleted, false) = false
    INNER JOIN public.stock_out_item soi
        ON soi."StockOutId" = so."StockOutId"
       AND COALESCE(soi.is_deleted, false) = false
    INNER JOIN public.stock_out_item_extend e
        ON e."StockOutItemId" = soi."ItemId"
       AND COALESCE(e.is_deleted, false) = false
    GROUP BY l.id
),
calc AS (
    SELECT
        l.sell_order_code,
        l.sell_order_item_code,
        l.id,
        l.out_qty,
        ROUND(l.out_qty * l.convert_price, 2) AS rev_out_usd,
        COALESCE(p.avg_po_cost_usd, 0) AS avg_po_cost_usd,
        COALESCE(oe.has_usable_actual, false) AS use_actual_batch_cost,
        CASE
            WHEN COALESCE(oe.has_usable_actual, false) AND COALESCE(oe.ext_qty_sum, 0) > 0
                THEN ROUND(oe.actual_cost_raw, 2)
            ELSE ROUND(l.out_qty * COALESCE(p.avg_po_cost_usd, 0), 2)
        END AS new_cost_usd,
        CASE
            WHEN l.out_qty <= 0 THEN 0::numeric(18,2)
            WHEN COALESCE(oe.has_usable_actual, false) AND COALESCE(oe.ext_qty_sum, 0) > 0 THEN
                CASE
                    WHEN ROUND(COALESCE(oe.profit_sum_raw, 0), 2) = 0
                         AND ROUND(COALESCE(oe.actual_cost_raw, 0), 2) > 0
                        THEN ROUND(ROUND(l.out_qty * l.convert_price, 2) - ROUND(oe.actual_cost_raw, 2), 2)
                    ELSE ROUND(COALESCE(oe.profit_sum_raw, 0), 2)
                END
            ELSE ROUND(ROUND(l.out_qty * l.convert_price, 2) - ROUND(l.out_qty * COALESCE(p.avg_po_cost_usd, 0), 2), 2)
        END AS new_profit_usd
    FROM lines l
    LEFT JOIN po_weighted p ON p.id = l.id
    LEFT JOIN outbound_ext oe ON oe.id = l.id
    WHERE l.out_qty > 0
)
SELECT
    c.sell_order_code,
    c.sell_order_item_code,
    c.id AS sell_order_item_id,
    c.out_qty,
    c.use_actual_batch_cost,
    c.avg_po_cost_usd,
    c.rev_out_usd,
    c.new_cost_usd,
    c.old_profit_usd,
    c.new_profit_usd,
    (c.new_profit_usd - c.old_profit_usd) AS profit_delta,
    CASE
        WHEN c.new_cost_usd > 0 THEN ROUND(c.rev_out_usd / c.new_cost_usd, 6)
        ELSE 0::numeric(18,6)
    END AS new_rate,
    c.old_rate
FROM calc c
WHERE ABS(c.new_profit_usd - c.old_profit_usd) >= 0.01
   OR ABS(
        CASE WHEN c.new_cost_usd > 0 THEN ROUND(c.rev_out_usd / c.new_cost_usd, 6) ELSE 0 END
        - COALESCE(c.old_rate, 0)
      ) >= 0.000001
ORDER BY ABS(c.new_profit_usd - c.old_profit_usd) DESC, c.sell_order_code, c.sell_order_item_code;

-- -----------------------------------------------------------------------------
-- 二、批量更新 sellorderitemextend 出库利润字段
-- -----------------------------------------------------------------------------
BEGIN;

WITH lines AS (
    SELECT
        soi."SellOrderItemId" AS id,
        soi.convert_price,
        ext."QtyStockOutActual" AS out_qty
    FROM public.sellorderitem soi
    JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
    WHERE COALESCE(ext.is_deleted, false) = false
      AND COALESCE(soi.is_deleted, false) = false
      -- 与「一、预览」保持相同可选过滤条件
      -- AND soi.sell_order_id = 'cd27f4d2-0c04-47cc-b873-c52e4dab9904'
      -- AND soi."SellOrderItemId" = 'b7fabdc1-fa63-4377-90a8-5440fde96f25'
),
po_weighted AS (
    SELECT
        l.id,
        CASE
            WHEN COALESCE(SUM(poi.qty), 0) > 0
                THEN ROUND(SUM(poi.qty * poi.convert_price) / SUM(poi.qty), 6)
            ELSE 0::numeric(18,6)
        END AS avg_po_cost_usd
    FROM lines l
    LEFT JOIN public.purchaseorderitem poi ON poi.sell_order_item_id = l.id
    GROUP BY l.id
),
outbound_ext AS (
    SELECT
        l.id,
        SUM(
            CASE
                WHEN e."QtyStockOut" > 0 THEN e."QtyStockOut"
                WHEN soi."ActualQty" > 0 THEN soi."ActualQty"
                ELSE soi."Quantity"
            END
        )::numeric(18,4) AS ext_qty_sum,
        SUM(
            (
                CASE
                    WHEN e."QtyStockOut" > 0 THEN e."QtyStockOut"
                    WHEN soi."ActualQty" > 0 THEN soi."ActualQty"
                    ELSE soi."Quantity"
                END
            ) * e."PurchasePriceUsd"
        )::numeric(18,6) AS actual_cost_raw,
        SUM(e."ProfitOutBizUsd")::numeric(18,2) AS profit_sum_raw,
        BOOL_OR(e."PurchasePriceUsd" > 0 OR e."ProfitOutBizUsd" <> 0) AS has_usable_actual
    FROM lines l
    INNER JOIN public.stock_out so
        ON so."SellOrderItemId" = l.id
       AND so."Status" IN (2, 4)
       AND so."StockOutType" = 10
       AND COALESCE(so.is_deleted, false) = false
    INNER JOIN public.stock_out_item soi
        ON soi."StockOutId" = so."StockOutId"
       AND COALESCE(soi.is_deleted, false) = false
    INNER JOIN public.stock_out_item_extend e
        ON e."StockOutItemId" = soi."ItemId"
       AND COALESCE(e.is_deleted, false) = false
    GROUP BY l.id
),
calc AS (
    SELECT
        l.id,
        l.out_qty,
        ROUND(l.out_qty * l.convert_price, 2) AS rev_out_usd,
        COALESCE(p.avg_po_cost_usd, 0) AS avg_po_cost_usd,
        COALESCE(oe.has_usable_actual, false) AS has_usable_actual,
        CASE
            WHEN COALESCE(oe.has_usable_actual, false) AND COALESCE(oe.ext_qty_sum, 0) > 0
                THEN ROUND(oe.actual_cost_raw, 2)
            ELSE ROUND(l.out_qty * COALESCE(p.avg_po_cost_usd, 0), 2)
        END AS cost_usd,
        CASE
            WHEN l.out_qty <= 0 THEN 0::numeric(18,2)
            WHEN COALESCE(oe.has_usable_actual, false) AND COALESCE(oe.ext_qty_sum, 0) > 0 THEN
                CASE
                    WHEN ROUND(COALESCE(oe.profit_sum_raw, 0), 2) = 0
                         AND ROUND(COALESCE(oe.actual_cost_raw, 0), 2) > 0
                        THEN ROUND(ROUND(l.out_qty * l.convert_price, 2) - ROUND(oe.actual_cost_raw, 2), 2)
                    ELSE ROUND(COALESCE(oe.profit_sum_raw, 0), 2)
                END
            ELSE ROUND(ROUND(l.out_qty * l.convert_price, 2) - ROUND(l.out_qty * COALESCE(p.avg_po_cost_usd, 0), 2), 2)
        END AS profit_usd
    FROM lines l
    LEFT JOIN po_weighted p ON p.id = l.id
    LEFT JOIN outbound_ext oe ON oe.id = l.id
)
UPDATE public.sellorderitemextend ext
SET
    "ProfitOutBizUsd" = c.profit_usd,
    "ProfitOutRateBiz" = CASE
        WHEN c.cost_usd > 0 THEN ROUND(c.rev_out_usd / c.cost_usd, 6)
        ELSE 0::numeric(18,6)
    END,
    "ProfitOutFinUsd" = c.profit_usd,
    "ProfitOutRateFin" = CASE
        WHEN c.cost_usd > 0 THEN ROUND(c.rev_out_usd / c.cost_usd, 6)
        ELSE 0::numeric(18,6)
    END,
    "ModifyTime" = NOW() AT TIME ZONE 'UTC'
FROM calc c
WHERE ext."SellOrderItemId" = c.id;

COMMIT;

-- -----------------------------------------------------------------------------
-- 三、复检：仍使用 PO 加权回退、但已有出库 extend 快照的明细（应人工核对）
-- -----------------------------------------------------------------------------
WITH lines AS (
    SELECT soi."SellOrderItemId" AS id, so.sell_order_code, soi.sell_order_item_code
    FROM public.sellorderitem soi
    JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
    JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
    WHERE ext."QtyStockOutActual" > 0
      AND COALESCE(ext.is_deleted, false) = false
),
has_extend AS (
    SELECT DISTINCT l.id
    FROM lines l
    INNER JOIN public.stock_out so
        ON so."SellOrderItemId" = l.id
       AND so."Status" IN (2, 4)
       AND so."StockOutType" = 10
       AND COALESCE(so.is_deleted, false) = false
    INNER JOIN public.stock_out_item soi
        ON soi."StockOutId" = so."StockOutId"
       AND COALESCE(soi.is_deleted, false) = false
    INNER JOIN public.stock_out_item_extend e
        ON e."StockOutItemId" = soi."ItemId"
       AND COALESCE(e.is_deleted, false) = false
       AND (e."PurchasePriceUsd" > 0 OR e."ProfitOutBizUsd" <> 0)
)
SELECT l.sell_order_code, l.sell_order_item_code, l.id AS sell_order_item_id
FROM lines l
LEFT JOIN has_extend h ON h.id = l.id
WHERE h.id IS NULL
ORDER BY l.sell_order_code, l.sell_order_item_code;

-- =============================================================================
-- stock 汇总桶数量与 stock_item 明细层不一致 — 诊断与全量修复
--
-- 问题类型：
--   stock（汇总桶）上的 QtyRepertory / QtyRepertoryAvailable 等字段，应为同桶下
--   未软删 stock_item 的合计；若删除入库/删除明细后未执行 RecalculateStockAggregateTotals，
--   则汇总桶会残留旧值，导致库存中心「可用数量」、出货通知「在库可用」偏大。
--
-- 典型症状（与代码 GetAvailableQtyForSellOrderItemAsync 一致）：
--   stock_bucket_avail > stockitem_avail（明细层仅计 is_deleted = false）
--
-- 使用顺序：
--   1) 跑「一、全量统计」确认影响面
--   2) 跑「二、差异明细」抽查
--   3) 在事务内跑「三、PREVIEW」核对将改动的行
--   4) 确认后跑「四、修复」
--   5) 跑「五、修复后验证」应返回 0 行
--
-- 注意：修复后库存中心/出货通知可用量会下降为明细层真实值，属预期行为。
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 一、全量统计：有多少汇总桶与明细层不一致
-- -----------------------------------------------------------------------------
SELECT
  COUNT(*) AS mismatched_bucket_count,
  COALESCE(SUM(diff), 0) AS total_overstatement_qty_avail
FROM (
  SELECT
    s."StockId",
    s."QtyRepertoryAvailable"
      - COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS diff
  FROM public.stock s
  LEFT JOIN public.stock_item si ON si."StockAggregateId" = s."StockId"
  WHERE NOT s.is_deleted
  GROUP BY s."StockId", s."QtyRepertoryAvailable"
  HAVING s."QtyRepertoryAvailable"
       <> COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0)
) t;

-- -----------------------------------------------------------------------------
-- 二、差异明细（按高估可用量降序，便于抽查）
-- -----------------------------------------------------------------------------
SELECT
  s."StockCode",
  s.sell_order_item_id,
  s."Type" AS stock_type,
  s."QtyRepertory" AS bucket_repertory,
  s."QtyRepertoryAvailable" AS bucket_avail,
  COALESCE(SUM(si."QtyRepertory") FILTER (WHERE NOT si.is_deleted), 0) AS detail_repertory,
  COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS detail_avail,
  s."QtyRepertoryAvailable"
    - COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS diff_avail,
  COUNT(si."StockItemId") FILTER (WHERE NOT si.is_deleted) AS active_detail_rows,
  COUNT(si."StockItemId") FILTER (WHERE si.is_deleted) AS deleted_detail_rows
FROM public.stock s
LEFT JOIN public.stock_item si ON si."StockAggregateId" = s."StockId"
WHERE NOT s.is_deleted
GROUP BY
  s."StockId", s."StockCode", s.sell_order_item_id, s."Type",
  s."QtyRepertory", s."QtyRepertoryAvailable"
HAVING s."QtyRepertoryAvailable"
     <> COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0)
ORDER BY diff_avail DESC, s."StockCode";

-- -----------------------------------------------------------------------------
-- 三、PREVIEW：修复前将写入的新值（建议 BEGIN; 跑完后 ROLLBACK; 先看不改库）
-- -----------------------------------------------------------------------------
-- BEGIN;

SELECT
  s."StockCode",
  s."QtyOccupy" AS old_occupy,
  x.qty_occupy AS new_occupy,
  s."QtySales" AS old_sales,
  x.qty_sales AS new_sales,
  s."QtyRepertory" AS old_repertory,
  x.qty_repertory AS new_repertory,
  s."QtyRepertoryAvailable" AS old_avail,
  x.qty_avail AS new_avail
FROM public.stock s
JOIN (
  SELECT
    s2."StockId",
    COALESCE(SUM(si."QtyOccupy") FILTER (WHERE NOT si.is_deleted), 0) AS qty_occupy,
    COALESCE(SUM(si."QtySales") FILTER (WHERE NOT si.is_deleted), 0) AS qty_sales,
    COALESCE(SUM(si."QtyRepertory") FILTER (WHERE NOT si.is_deleted), 0) AS qty_repertory,
    COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS qty_avail
  FROM public.stock s2
  LEFT JOIN public.stock_item si ON si."StockAggregateId" = s2."StockId"
  WHERE NOT s2.is_deleted
  GROUP BY s2."StockId"
) x ON x."StockId" = s."StockId"
WHERE NOT s.is_deleted
  AND (
    s."QtyOccupy" IS DISTINCT FROM x.qty_occupy
    OR s."QtySales" IS DISTINCT FROM x.qty_sales
    OR s."QtyRepertory" IS DISTINCT FROM x.qty_repertory
    OR s."QtyRepertoryAvailable" IS DISTINCT FROM x.qty_avail
  )
ORDER BY (s."QtyRepertoryAvailable" - x.qty_avail) DESC, s."StockCode";

-- ROLLBACK;

-- -----------------------------------------------------------------------------
-- 四、修复：按明细层重算全部未删汇总桶（与 RecalculateStockAggregateTotalsAsync 同口径）
-- -----------------------------------------------------------------------------
-- BEGIN;

UPDATE public.stock s
SET
  "QtyOccupy" = x.qty_occupy,
  "QtySales" = x.qty_sales,
  "QtyRepertory" = x.qty_repertory,
  "QtyRepertoryAvailable" = x.qty_avail,
  "ModifyTime" = NOW()
FROM (
  SELECT
    s2."StockId",
    COALESCE(SUM(si."QtyOccupy") FILTER (WHERE NOT si.is_deleted), 0) AS qty_occupy,
    COALESCE(SUM(si."QtySales") FILTER (WHERE NOT si.is_deleted), 0) AS qty_sales,
    COALESCE(SUM(si."QtyRepertory") FILTER (WHERE NOT si.is_deleted), 0) AS qty_repertory,
    COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS qty_avail
  FROM public.stock s2
  LEFT JOIN public.stock_item si ON si."StockAggregateId" = s2."StockId"
  WHERE NOT s2.is_deleted
  GROUP BY s2."StockId"
) x
WHERE s."StockId" = x."StockId"
  AND NOT s.is_deleted
  AND (
    s."QtyOccupy" IS DISTINCT FROM x.qty_occupy
    OR s."QtySales" IS DISTINCT FROM x.qty_sales
    OR s."QtyRepertory" IS DISTINCT FROM x.qty_repertory
    OR s."QtyRepertoryAvailable" IS DISTINCT FROM x.qty_avail
  );

-- COMMIT;

-- -----------------------------------------------------------------------------
-- 五、修复后验证：应返回 0 行
-- -----------------------------------------------------------------------------
SELECT
  s."StockCode",
  s."QtyRepertoryAvailable" AS bucket_avail,
  COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS detail_avail,
  s."QtyRepertoryAvailable"
    - COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0) AS diff_avail
FROM public.stock s
LEFT JOIN public.stock_item si ON si."StockAggregateId" = s."StockId"
WHERE NOT s.is_deleted
GROUP BY s."StockId", s."StockCode", s."QtyRepertoryAvailable"
HAVING s."QtyRepertoryAvailable"
     <> COALESCE(SUM(si."QtyRepertoryAvailable") FILTER (WHERE NOT si.is_deleted), 0)
ORDER BY diff_avail DESC;

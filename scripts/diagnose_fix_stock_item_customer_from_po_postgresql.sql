-- =============================================================================
-- 客单采购入库后 stock_item 客户/业务员为空 — 诊断、个案核对、全量统计与修复
--
-- 业务根因（代码 InventoryCenterService.CreateStockItemForInboundLineAsync）：
--   过账时仅当 stock 分桶上的 sell_order_item_id（来自 stock_in_item_extend）非空时，
--   才从 sellorder 写入 CustomerId/CustomerName、SalespersonId/SalespersonName。
--   若入库扩展表未带销行，但 purchaseorderitem.sell_order_item_id 已有值，仍会漏写客户。
--
-- 使用顺序：先跑「一、个案」→「二、全量统计」→ 确认后跑「四、修复」（建议事务内 PREVIEW）
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 一、个案：PO0021N / STI0022B / STK0020V-1（按实际单号改 WHERE）
-- -----------------------------------------------------------------------------
WITH target AS (
    SELECT si."StockItemId", si.stock_item_code, si."StockInId", si."StockInItemId",
           si.purchase_order_item_id, si.purchase_order_item_code, si.sell_order_item_id AS si_so_id,
           si."CustomerId", si."CustomerName", si."SalespersonId", si."SalespersonName",
           si."Type" AS stock_item_type
    FROM public.stock_item si
    WHERE si.is_deleted = false
      AND (
          si.stock_item_code = 'STK0020V-1'
          OR si.purchase_order_item_code = 'PO0021N-1'
      )
)
SELECT
    t.stock_item_code,
    t.purchase_order_item_code,
    sin."StockInCode" AS stock_in_code,
    sin."StockInDate",
    t.si_so_id AS stock_item_sell_line_id,
    ext.sell_order_item_id AS extend_sell_line_id,
    poi.sell_order_item_id AS po_item_sell_line_id,
    soi.sell_order_item_code,
    so.sell_order_code,
    so.customer_id AS order_customer_id,
    so.customer_name AS order_customer_name,
    so.sales_user_id,
    so.sales_user_name,
    t."CustomerId" AS stock_item_customer_id,
    t."CustomerName" AS stock_item_customer_name,
    pr.bill_code AS pr_code,
    pr.sell_order_id AS pr_sell_order_id
FROM target t
JOIN public.stock_in sin ON sin."StockInId" = t."StockInId" AND sin.is_deleted = false
LEFT JOIN public.stock_in_item_extend ext
    ON ext."StockInItemId" = t."StockInItemId" AND COALESCE(ext.is_deleted, false) = false
LEFT JOIN public.purchaseorderitem poi
    ON poi."PurchaseOrderItemId" = COALESCE(NULLIF(TRIM(t.purchase_order_item_id), ''), ext.purchase_order_item_id)
LEFT JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
LEFT JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
LEFT JOIN public.purchaseorder po ON po."PurchaseOrderId" = poi.purchase_order_id
LEFT JOIN public.purchaserequisition pr ON pr.sell_order_item_id = poi.sell_order_item_id
ORDER BY t.stock_item_code;

-- -----------------------------------------------------------------------------
-- 二、全系统：应带客户却为空的在库明细（可修复候选）
-- 条件：采购明细已关联销售行 + 销售订单有客户 + stock_item 客户名为空
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS affected_stock_item_rows
FROM public.stock_item si
INNER JOIN public.purchaseorderitem poi
    ON poi."PurchaseOrderItemId" = si.purchase_order_item_id
   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
INNER JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
WHERE si.is_deleted = false
  AND NULLIF(TRIM(so.customer_id), '') IS NOT NULL
  AND (si."CustomerName" IS NULL OR TRIM(si."CustomerName") = '');

-- 明细列表（前 200 条，便于抽查）
SELECT
    si.stock_item_code,
    si.purchase_order_item_code,
    sin."StockInCode",
    sin."StockInDate",
    so.sell_order_code,
    so.customer_name,
    so.sales_user_name,
    si.sell_order_item_id AS si_so_id,
    ext.sell_order_item_id AS extend_so_id,
    poi.sell_order_item_id AS po_so_id
FROM public.stock_item si
INNER JOIN public.stock_in sin ON sin."StockInId" = si."StockInId"
INNER JOIN public.purchaseorderitem poi
    ON poi."PurchaseOrderItemId" = si.purchase_order_item_id
INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
INNER JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
LEFT JOIN public.stock_in_item_extend ext ON ext."StockInItemId" = si."StockInItemId"
WHERE si.is_deleted = false
  AND NULLIF(TRIM(so.customer_id), '') IS NOT NULL
  AND (si."CustomerName" IS NULL OR TRIM(si."CustomerName") = '')
ORDER BY sin."StockInDate" DESC NULLS LAST, si.stock_item_code
LIMIT 200;

-- 扩展表缺销行但 PO 行有销行（过账前即应补齐的数据缺口）
SELECT COUNT(*) AS extend_missing_so_but_po_has_so
FROM public.stock_in_item_extend ext
INNER JOIN public.purchaseorderitem poi
    ON poi."PurchaseOrderItemId" = ext.purchase_order_item_id
   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
WHERE COALESCE(ext.is_deleted, false) = false
  AND NULLIF(TRIM(ext.sell_order_item_id), '') IS NULL;

-- -----------------------------------------------------------------------------
-- 三、PREVIEW：修复后将写入的值（与四、UPDATE 同一 JOIN 逻辑）
-- -----------------------------------------------------------------------------
SELECT
    si."StockItemId",
    si.stock_item_code,
    si."CustomerName" AS old_customer_name,
    so.customer_name AS new_customer_name,
    si."SalespersonName" AS old_sales_name,
    so.sales_user_name AS new_sales_name,
    COALESCE(NULLIF(TRIM(si.sell_order_item_id), ''), poi.sell_order_item_id) AS new_sell_line_id
FROM public.stock_item si
INNER JOIN public.purchaseorderitem poi
    ON poi."PurchaseOrderItemId" = si.purchase_order_item_id
   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
INNER JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
WHERE si.is_deleted = false
  AND NULLIF(TRIM(so.customer_id), '') IS NOT NULL
  AND (si."CustomerName" IS NULL OR TRIM(si."CustomerName") = '')
ORDER BY si.stock_item_code
LIMIT 500;

-- -----------------------------------------------------------------------------
-- 四、修复（建议在事务中执行：BEGIN; … COMMIT; 或 ROLLBACK;）
-- 4a) 补齐入库扩展表上的销行（若为空且 PO 行有值）
-- -----------------------------------------------------------------------------
-- UPDATE public.stock_in_item_extend ext
-- SET
--     sell_order_item_id = poi.sell_order_item_id,
--     sell_order_item_code = soi.sell_order_item_code,
--     "ModifyTime" = NOW() AT TIME ZONE 'UTC'
-- FROM public.purchaseorderitem poi
-- INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
-- WHERE ext.purchase_order_item_id = poi."PurchaseOrderItemId"
--   AND COALESCE(ext.is_deleted, false) = false
--   AND NULLIF(TRIM(ext.sell_order_item_id), '') IS NULL
--   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL;

-- 4b) 回填 stock_item 客户/业务员/销行（主修复）
-- -----------------------------------------------------------------------------
-- UPDATE public.stock_item si
-- SET
--     sell_order_item_id = COALESCE(NULLIF(TRIM(si.sell_order_item_id), ''), poi.sell_order_item_id),
--     sell_order_item_code = COALESCE(NULLIF(TRIM(si.sell_order_item_code), ''), soi.sell_order_item_code),
--     "CustomerId" = so.customer_id,
--     "CustomerName" = so.customer_name,
--     "SalespersonId" = so.sales_user_id,
--     "SalespersonName" = so.sales_user_name,
--     "ModifyTime" = NOW() AT TIME ZONE 'UTC'
-- FROM public.purchaseorderitem poi
-- INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
-- INNER JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
-- WHERE si.purchase_order_item_id = poi."PurchaseOrderItemId"
--   AND si.is_deleted = false
--   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
--   AND NULLIF(TRIM(so.customer_id), '') IS NOT NULL
--   AND (si."CustomerName" IS NULL OR TRIM(si."CustomerName") = '');

-- 4c) 同步 stock 分桶上的 sell_order_item_id（可选，与明细一致）
-- -----------------------------------------------------------------------------
-- UPDATE public.stock s
-- SET
--     sell_order_item_id = sub.so_id,
--     sell_order_item_code = sub.so_code,
--     "ModifyTime" = NOW() AT TIME ZONE 'UTC'
-- FROM (
--     SELECT DISTINCT ON (si."StockAggregateId")
--         si."StockAggregateId",
--         poi.sell_order_item_id AS so_id,
--         soi.sell_order_item_code AS so_code
--     FROM public.stock_item si
--     INNER JOIN public.purchaseorderitem poi ON poi."PurchaseOrderItemId" = si.purchase_order_item_id
--     INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
--     WHERE si.is_deleted = false
--       AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
--       AND (si."CustomerName" IS NOT NULL AND TRIM(si."CustomerName") <> '')
--     ORDER BY si."StockAggregateId", si."CreateTime" DESC
-- ) sub
-- WHERE s."StockId" = sub."StockAggregateId"
--   AND NULLIF(TRIM(s.sell_order_item_id), '') IS NULL;

-- -----------------------------------------------------------------------------
-- 五、修复后复检（应返回 0）
-- -----------------------------------------------------------------------------
-- SELECT COUNT(*) AS remaining_bad_rows
-- FROM public.stock_item si
-- INNER JOIN public.purchaseorderitem poi ON poi."PurchaseOrderItemId" = si.purchase_order_item_id
-- INNER JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
-- INNER JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
-- WHERE si.is_deleted = false
--   AND NULLIF(TRIM(poi.sell_order_item_id), '') IS NOT NULL
--   AND NULLIF(TRIM(so.customer_id), '') IS NOT NULL
--   AND (si."CustomerName" IS NULL OR TRIM(si."CustomerName") = '');

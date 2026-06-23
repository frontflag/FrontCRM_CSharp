-- 刷新 purchaserequisition.purchase_user_id（采购员）
-- 口径与 PurchaseRequisitionPurchaserResolver / CreateAsync 一致：
--   1) 销售明细关联报价单 quote.purchase_user_id
--   2) 报价单关联需求明细 rfqitem.assigned_purchaser_user_id_1
--   3) rfqitem.assigned_purchaser_user_id_2
--
-- 建议：先跑「预览」确认影响行，再跑 UPDATE。

-- ---------------------------------------------------------------------------
-- 预览：将写入的采购员（仅展示能解析到的行）
-- ---------------------------------------------------------------------------
SELECT
    pr."PurchaseRequisitionId" AS purchase_requisition_id,
    pr.bill_code,
    pr.purchase_user_id AS purchase_user_id_before,
    src.resolved_purchase_user_id AS purchase_user_id_after,
    so."SellOrderId" AS sell_order_id,
    so.sell_order_code,
    q."QuoteId" AS quote_id,
    q.quote_code
FROM purchaserequisition pr
LEFT JOIN sellorder so
    ON so."SellOrderId" = pr.sell_order_id
LEFT JOIN sellorderitem soi
    ON soi."SellOrderItemId" = pr.sell_order_item_id
LEFT JOIN quote q
    ON q."QuoteId" = soi.quote_id
LEFT JOIN rfqitem ri
    ON ri.item_id = q.rfq_item_id
CROSS JOIN LATERAL (
    SELECT NULLIF(TRIM(
        COALESCE(
            NULLIF(TRIM(q.purchase_user_id), ''),
            NULLIF(TRIM(ri.assigned_purchaser_user_id_1), ''),
            NULLIF(TRIM(ri.assigned_purchaser_user_id_2), '')
        )
    ), '') AS resolved_purchase_user_id
) src
WHERE COALESCE(pr.is_deleted, false) = false
  AND src.resolved_purchase_user_id IS NOT NULL
  AND (
        pr.purchase_user_id IS DISTINCT FROM src.resolved_purchase_user_id
      )
ORDER BY pr.bill_code;

-- ---------------------------------------------------------------------------
-- 无法解析采购员的行（可选排查）
-- ---------------------------------------------------------------------------
SELECT
    pr."PurchaseRequisitionId" AS purchase_requisition_id,
    pr.bill_code,
    pr.purchase_user_id AS purchase_user_id_before,
    pr.sell_order_item_id,
    soi.quote_id
FROM purchaserequisition pr
LEFT JOIN sellorderitem soi
    ON soi."SellOrderItemId" = pr.sell_order_item_id
WHERE COALESCE(pr.is_deleted, false) = false
  AND (
        soi.quote_id IS NULL
        OR NOT EXISTS (
            SELECT 1
            FROM quote q
            LEFT JOIN rfqitem ri ON ri.item_id = q.rfq_item_id
            WHERE q."QuoteId" = soi.quote_id
              AND NULLIF(TRIM(
                      COALESCE(
                          NULLIF(TRIM(q.purchase_user_id), ''),
                          NULLIF(TRIM(ri.assigned_purchaser_user_id_1), ''),
                          NULLIF(TRIM(ri.assigned_purchaser_user_id_2), '')
                      )
                  ), '') IS NOT NULL
        )
      )
ORDER BY pr.bill_code;

-- ---------------------------------------------------------------------------
-- 执行回填（仅更新能解析且与现值不同的行）
-- ---------------------------------------------------------------------------
UPDATE purchaserequisition pr
SET purchase_user_id = src.resolved_purchase_user_id
FROM (
    SELECT
        pr2."PurchaseRequisitionId" AS pr_id,
        NULLIF(TRIM(
            COALESCE(
                NULLIF(TRIM(q.purchase_user_id), ''),
                NULLIF(TRIM(ri.assigned_purchaser_user_id_1), ''),
                NULLIF(TRIM(ri.assigned_purchaser_user_id_2), '')
            )
        ), '') AS resolved_purchase_user_id
    FROM purchaserequisition pr2
    LEFT JOIN sellorderitem soi
        ON soi."SellOrderItemId" = pr2.sell_order_item_id
    LEFT JOIN quote q
        ON q."QuoteId" = soi.quote_id
    LEFT JOIN rfqitem ri
        ON ri.item_id = q.rfq_item_id
    WHERE COALESCE(pr2.is_deleted, false) = false
) src
WHERE pr."PurchaseRequisitionId" = src.pr_id
  AND src.resolved_purchase_user_id IS NOT NULL
  AND pr.purchase_user_id IS DISTINCT FROM src.resolved_purchase_user_id;

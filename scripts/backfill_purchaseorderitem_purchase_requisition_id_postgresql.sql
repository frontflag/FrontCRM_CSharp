-- 回填 purchaseorderitem.purchase_requisition_id（历史数据）
-- 0) 必须先跑：scripts/ensure_purchaseorderitem_purchase_requisition_id_postgresql.sql
--    （或 dotnet ef database update；未加列时 UPDATE 会报 column does not exist）
-- 1) 按需手动指定 PO 行 → PR（已知来源时优先）
-- 2) 或按销售行 FIFO 自动归属未关联 PO 行（与 RecalculateAsync 兜底一致）
-- 3) 最后触发 PR 状态重算（脚本末尾 UPDATE，或重启 API 后保存任意 PO）
--
-- 注意：purchaseorderitem 主表无 is_deleted；有效 PO 行用 status NOT IN (-1,-2) 过滤。
--       purchaserequisition 若尚无 is_deleted 列，脚本内已不依赖该列。

-- ========== 诊断：同销售行多 PR 与 PO 关联 ==========
-- SELECT pr.bill_code, pr."PurchaseRequisitionId", pr.qty, pr.status,
--        poi.purchase_order_item_code, poi.qty, poi.purchase_requisition_id
-- FROM purchaserequisition pr
-- LEFT JOIN purchaseorderitem poi
--   ON poi.sell_order_item_id = pr.sell_order_item_id
--  AND poi.status NOT IN (-1, -2)
-- WHERE pr.sell_order_item_id = '<sell_order_item_id>'
-- ORDER BY pr.create_time, pr."PurchaseRequisitionId", poi.purchase_order_item_code;

-- ========== 示例：手动绑定 PO0005Z-1 → 实际生成它的 PR ==========
-- UPDATE purchaseorderitem poi
-- SET purchase_requisition_id = pr."PurchaseRequisitionId"
-- FROM purchaserequisition pr
-- WHERE poi.purchase_order_item_code = 'PO0005Z-1'
--   AND pr.bill_code = 'POR0001Y'
--   AND poi.purchase_requisition_id IS NULL;

-- ========== FIFO 自动回填（仅 purchase_requisition_id IS NULL 的 PO 行）==========
WITH pr_ordered AS (
    SELECT
        pr."PurchaseRequisitionId" AS pr_id,
        pr.sell_order_item_id,
        pr.qty AS pr_qty,
        pr.create_time,
        ROW_NUMBER() OVER (
            PARTITION BY pr.sell_order_item_id
            ORDER BY pr.create_time, pr."PurchaseRequisitionId"
        ) AS pr_seq
    FROM purchaserequisition pr
),
po_unlinked AS (
    SELECT
        poi."PurchaseOrderItemId" AS poi_id,
        poi.sell_order_item_id,
        poi.qty AS poi_qty,
        poi.purchase_order_item_code,
        ROW_NUMBER() OVER (
            PARTITION BY poi.sell_order_item_id
            ORDER BY poi.purchase_order_item_code
        ) AS poi_seq
    FROM purchaseorderitem poi
    LEFT JOIN purchaseorderitemextend ext
      ON ext."PurchaseOrderItemId" = poi."PurchaseOrderItemId"
    WHERE poi.purchase_requisition_id IS NULL
      AND poi.sell_order_item_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
      AND (ext."PurchaseOrderItemId" IS NULL OR NOT ext.is_deleted)
),
paired AS (
    SELECT
        u.poi_id,
        p.pr_id
    FROM po_unlinked u
    JOIN pr_ordered p
      ON p.sell_order_item_id = u.sell_order_item_id
     AND p.pr_seq = u.poi_seq
)
UPDATE purchaseorderitem poi
SET purchase_requisition_id = paired.pr_id
FROM paired
WHERE poi."PurchaseOrderItemId" = paired.poi_id
  AND poi.purchase_requisition_id IS NULL;

-- ========== 按 FIFO 逻辑修正 PR.status（0/1/2）==========
WITH pr_base AS (
    SELECT
        pr."PurchaseRequisitionId" AS pr_id,
        pr.sell_order_item_id,
        pr.qty AS pr_qty,
        pr.status AS old_status,
        pr.create_time
    FROM purchaserequisition pr
    WHERE pr.status <> 3
),
explicit AS (
    SELECT
        poi.purchase_requisition_id AS pr_id,
        SUM(poi.qty) AS explicit_qty
    FROM purchaseorderitem poi
    LEFT JOIN purchaseorderitemextend ext
      ON ext."PurchaseOrderItemId" = poi."PurchaseOrderItemId"
    WHERE poi.purchase_requisition_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
      AND (ext."PurchaseOrderItemId" IS NULL OR NOT ext.is_deleted)
    GROUP BY poi.purchase_requisition_id
),
unlinked_by_so AS (
    SELECT
        poi.sell_order_item_id,
        SUM(poi.qty) AS unlinked_qty
    FROM purchaseorderitem poi
    LEFT JOIN purchaseorderitemextend ext
      ON ext."PurchaseOrderItemId" = poi."PurchaseOrderItemId"
    WHERE poi.purchase_requisition_id IS NULL
      AND poi.sell_order_item_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
      AND (ext."PurchaseOrderItemId" IS NULL OR NOT ext.is_deleted)
    GROUP BY poi.sell_order_item_id
),
fifo AS (
    SELECT
        b.pr_id,
        b.pr_qty,
        COALESCE(e.explicit_qty, 0) AS explicit_qty,
        COALESCE(u.unlinked_qty, 0) AS unlinked_qty,
        SUM(b.pr_qty) OVER (
            PARTITION BY b.sell_order_item_id
            ORDER BY b.create_time, b.pr_id
            ROWS UNBOUNDED PRECEDING
        ) AS cum_pr_qty
    FROM pr_base b
    LEFT JOIN explicit e ON e.pr_id = b.pr_id
    LEFT JOIN unlinked_by_so u ON u.sell_order_item_id = b.sell_order_item_id
),
linked AS (
    SELECT
        pr_id,
        pr_qty,
        explicit_qty
            + GREATEST(
                0,
                LEAST(
                    pr_qty,
                    unlinked_qty
                        - (cum_pr_qty - pr_qty)
                )
              ) AS linked_qty
    FROM fifo
),
next_status AS (
    SELECT
        pr_id,
        CASE
            WHEN linked_qty <= 0 THEN 0
            WHEN linked_qty < pr_qty THEN 1
            ELSE 2
        END AS new_status
    FROM linked
)
UPDATE purchaserequisition pr
SET status = ns.new_status
FROM next_status ns
WHERE pr."PurchaseRequisitionId" = ns.pr_id
  AND pr.status <> ns.new_status
  AND pr.status <> 3;

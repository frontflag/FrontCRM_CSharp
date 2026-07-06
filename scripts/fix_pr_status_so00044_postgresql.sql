-- =============================================================================
-- 诊断 + 修复：同销行多 PR 状态仍为「全部完成」
-- 典型场景：SO00044 下 POR0001Y / POR0001Z，仅 PO0005Z-1 应对应 POR0001Y
--
-- 用法：在 DBeaver 中【分段执行】——先跑「1. 诊断」，看清数据后再跑 2、3
-- =============================================================================

-- ---------- 1. 诊断（销售订单号 SO00044）----------
SELECT
    so.sell_order_code,
    pr.bill_code,
    pr."PurchaseRequisitionId" AS pr_id,
    pr.qty AS pr_qty,
    pr.status AS pr_status,
    pr."CreateTime" AS pr_create_time,
    poi.purchase_order_item_code,
    poi.qty AS po_qty,
    poi.purchase_requisition_id AS po_linked_pr_id,
    CASE
        WHEN poi.purchase_requisition_id IS NULL THEN 'PO 未绑定 PR'
        WHEN poi.purchase_requisition_id = pr."PurchaseRequisitionId" THEN 'PO 已绑定本 PR'
        ELSE 'PO 绑定到其它 PR'
    END AS link_hint
FROM public.purchaserequisition pr
JOIN public.sellorder so ON so."SellOrderId" = pr.sell_order_id
LEFT JOIN public.purchaseorderitem poi
  ON poi.sell_order_item_id = pr.sell_order_item_id
 AND poi.status NOT IN (-1, -2)
WHERE so.sell_order_code ILIKE 'SO00044'
ORDER BY pr."CreateTime", pr.bill_code, poi.purchase_order_item_code;

-- 若上一查询无 PO 行，单独查 PO（按明细号）：
-- SELECT purchase_order_item_code, qty, status, sell_order_item_id, purchase_requisition_id
-- FROM public.purchaseorderitem
-- WHERE purchase_order_item_code ILIKE 'PO0005Z%';

-- ---------- 2. 强制绑定 PO0005Z-1 → POR0001Y（允许覆盖已有错误绑定）----------
UPDATE public.purchaseorderitem poi
SET purchase_requisition_id = pr."PurchaseRequisitionId"
FROM public.purchaserequisition pr
WHERE poi.purchase_order_item_code = 'PO0005Z-1'
  AND pr.bill_code = 'POR0001Y';

-- 执行后应显示 Updated Rows: 1；若为 0，请核对诊断结果中的实际单号

-- ---------- 3. 按 purchase_requisition_id 重算 PR.status（与线上一致）----------
WITH pr_rows AS (
    SELECT
        pr."PurchaseRequisitionId" AS pr_id,
        pr.qty AS pr_qty,
        pr.status AS old_status,
        pr.sell_order_item_id,
        pr."CreateTime" AS create_time
    FROM public.purchaserequisition pr
    WHERE pr.status <> 3
),
explicit AS (
    SELECT
        poi.purchase_requisition_id AS pr_id,
        SUM(poi.qty) AS explicit_qty
    FROM public.purchaseorderitem poi
    WHERE poi.purchase_requisition_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
    GROUP BY poi.purchase_requisition_id
),
unlinked_by_so AS (
    SELECT
        poi.sell_order_item_id,
        SUM(poi.qty) AS unlinked_qty
    FROM public.purchaseorderitem poi
    WHERE poi.purchase_requisition_id IS NULL
      AND poi.sell_order_item_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
    GROUP BY poi.sell_order_item_id
),
fifo AS (
    SELECT
        b.pr_id,
        b.pr_qty,
        b.old_status,
        COALESCE(e.explicit_qty, 0) AS explicit_qty,
        COALESCE(u.unlinked_qty, 0) AS unlinked_qty,
        SUM(b.pr_qty) OVER (
            PARTITION BY b.sell_order_item_id
            ORDER BY b.create_time, b.pr_id
            ROWS UNBOUNDED PRECEDING
        ) AS cum_pr_qty
    FROM pr_rows b
    LEFT JOIN explicit e ON e.pr_id = b.pr_id
    LEFT JOIN unlinked_by_so u ON u.sell_order_item_id = b.sell_order_item_id
),
calc AS (
    SELECT
        pr_id,
        pr_qty,
        old_status,
        explicit_qty
            + GREATEST(
                0,
                LEAST(
                    pr_qty,
                    unlinked_qty - (cum_pr_qty - pr_qty)
                )
              ) AS linked_qty
    FROM fifo
),
next_status AS (
    SELECT
        pr_id,
        old_status,
        linked_qty,
        CASE
            WHEN linked_qty <= 0 THEN 0::smallint
            WHEN linked_qty < pr_qty THEN 1::smallint
            ELSE 2::smallint
        END AS new_status
    FROM calc
)
UPDATE public.purchaserequisition pr
SET status = ns.new_status
FROM next_status ns
WHERE pr."PurchaseRequisitionId" = ns.pr_id
  AND pr.status <> ns.new_status;

-- ---------- 4. 验证 SO00044 ----------
SELECT
    so.sell_order_code,
    pr.bill_code,
    pr.qty,
    pr.status,
    CASE pr.status
        WHEN 0 THEN '新建'
        WHEN 1 THEN '部分完成'
        WHEN 2 THEN '全部完成'
        WHEN 3 THEN '已取消'
        ELSE pr.status::text
    END AS status_label
FROM public.purchaserequisition pr
JOIN public.sellorder so ON so."SellOrderId" = pr.sell_order_id
WHERE so.sell_order_code ILIKE 'SO00044'
ORDER BY pr."CreateTime", pr.bill_code;

-- =============================================================================
-- 为有 sell_order_item_id 但未绑定 PR 的采购订单明细，挂上 purchase_requisition_id
--
-- 规则（与 PurchaseRequisitionPoLinkHelper 线上一致）：
--   A) 同销行仅 1 条 PR → 该销行所有待绑 PO 全部挂到这条 PR
--   B) 同销行仅 1 条待绑 PO、多条 PR → 按 PR CreateTime FIFO 挂到第一条 PR
--   C) 同销行多条 PR + 多条待绑 PO → 按 PO 明细号顺序，逐行挂到仍有剩余数量的 PR（FIFO）
--
-- 用法（DBeaver 分段执行）：
--   0) 若列不存在，先跑 scripts/ensure_purchaseorderitem_purchase_requisition_id_postgresql.sql
--   1) 诊断（改销售单号，或注释 WHERE 查全库未绑行）
--   2) 执行绑定 A → B → C（可只跑绑定段）
--   3) 验证 + 重算 PR.status
-- =============================================================================

-- ---------- 1. 诊断：未绑定 PR 的 PO 行（可按销售单过滤）----------
SELECT
    poi.purchase_order_item_code,
    poi.qty,
    poi.status,
    poi.sell_order_item_id,
    poi.purchase_requisition_id,
    pr.bill_code AS linked_pr_bill_code,
    pr.status AS linked_pr_status,
    so.sell_order_code,
    soi.sell_order_item_code,
    CASE
        WHEN poi.purchase_requisition_id IS NULL THEN '未绑定 PR'
        WHEN pr."PurchaseRequisitionId" IS NOT NULL THEN '已绑定 PR'
        ELSE 'PR 外键无效'
    END AS bind_state
FROM public.purchaseorderitem poi
LEFT JOIN public.purchaserequisition pr
  ON pr."PurchaseRequisitionId" = poi.purchase_requisition_id
LEFT JOIN public.sellorderitem soi
  ON soi."SellOrderItemId" = poi.sell_order_item_id
LEFT JOIN public.sellorder so
  ON so."SellOrderId" = soi.sell_order_id
WHERE poi.purchase_requisition_id IS NULL
  AND poi.sell_order_item_id IS NOT NULL
  AND poi.status NOT IN (-1, -2)
  -- AND so.sell_order_code = 'SO00044'   -- ← 限定销售单时取消注释
ORDER BY so.sell_order_code NULLS LAST, poi.purchase_order_item_code;

-- 同销行 PR / PO 对照（看清该挂哪条 PR）
SELECT
    so.sell_order_code,
    soi.sell_order_item_code,
    soi."SellOrderItemId" AS sell_order_item_id,
    pr.bill_code AS pr_bill_code,
    pr."PurchaseRequisitionId" AS pr_id,
    pr.qty AS pr_qty,
    pr."CreateTime" AS pr_create_time,
    poi.purchase_order_item_code,
    poi.qty AS po_qty,
    poi.purchase_requisition_id,
    CASE
        WHEN poi."PurchaseOrderItemId" IS NULL THEN '无 PO'
        WHEN poi.purchase_requisition_id IS NULL THEN 'PO 未绑 PR'
        WHEN poi.purchase_requisition_id = pr."PurchaseRequisitionId" THEN 'PO 已绑本 PR'
        ELSE 'PO 绑到其它 PR'
    END AS link_hint
FROM public.sellorder so
JOIN public.sellorderitem soi ON soi.sell_order_id = so."SellOrderId"
LEFT JOIN public.purchaserequisition pr ON pr.sell_order_item_id = soi."SellOrderItemId"
LEFT JOIN public.purchaseorderitem poi
  ON poi.sell_order_item_id = soi."SellOrderItemId"
 AND poi.status NOT IN (-1, -2)
WHERE 1 = 1
  -- AND so.sell_order_code = 'SO00044'   -- ← 限定销售单时取消注释
  AND (
      poi.purchase_requisition_id IS NULL
      OR pr."PurchaseRequisitionId" IS NOT NULL
  )
ORDER BY so.sell_order_code, soi.sell_order_item_code, pr."CreateTime", pr.bill_code, poi.purchase_order_item_code;

-- ---------- 2. 绑定 A：同销行仅 1 条 PR → 该销行所有待绑 PO 挂到这条 PR ----------
WITH scope_sell_lines AS (
    SELECT soi."SellOrderItemId" AS sell_order_item_id
    FROM public.sellorder so
    JOIN public.sellorderitem soi ON soi.sell_order_id = so."SellOrderId"
    WHERE 1 = 1
      -- AND so.sell_order_code = 'SO00044'   -- ← 限定销售单时取消注释
),
single_pr AS (
    SELECT
        pr.sell_order_item_id,
        MIN(pr."PurchaseRequisitionId") AS pr_id
    FROM public.purchaserequisition pr
    JOIN scope_sell_lines s ON s.sell_order_item_id = pr.sell_order_item_id
    GROUP BY pr.sell_order_item_id
    HAVING COUNT(*) = 1
)
UPDATE public.purchaseorderitem poi
SET purchase_requisition_id = sp.pr_id
FROM single_pr sp
WHERE poi.sell_order_item_id = sp.sell_order_item_id
  AND poi.purchase_requisition_id IS NULL
  AND poi.status NOT IN (-1, -2);

-- ---------- 3. 绑定 B：同销行仅 1 条待绑 PO、多条 PR → FIFO 第一条 PR ----------
WITH scope_sell_lines AS (
    SELECT soi."SellOrderItemId" AS sell_order_item_id
    FROM public.sellorder so
    JOIN public.sellorderitem soi ON soi.sell_order_id = so."SellOrderId"
    WHERE 1 = 1
      -- AND so.sell_order_code = 'SO00044'
),
single_poi AS (
    SELECT poi.sell_order_item_id
    FROM public.purchaseorderitem poi
    JOIN scope_sell_lines s ON s.sell_order_item_id = poi.sell_order_item_id
    WHERE poi.purchase_requisition_id IS NULL
      AND poi.status NOT IN (-1, -2)
    GROUP BY poi.sell_order_item_id
    HAVING COUNT(*) = 1
),
first_pr AS (
    SELECT DISTINCT ON (pr.sell_order_item_id)
        pr.sell_order_item_id,
        pr."PurchaseRequisitionId" AS pr_id
    FROM public.purchaserequisition pr
    JOIN single_poi sp ON sp.sell_order_item_id = pr.sell_order_item_id
    ORDER BY pr.sell_order_item_id, pr."CreateTime", pr."PurchaseRequisitionId"
)
UPDATE public.purchaseorderitem poi
SET purchase_requisition_id = fp.pr_id
FROM first_pr fp
WHERE poi.sell_order_item_id = fp.sell_order_item_id
  AND poi.purchase_requisition_id IS NULL
  AND poi.status NOT IN (-1, -2);

-- ---------- 4. 绑定 C：同销行多条 PR + 多条待绑 PO → 按明细号 + PR 剩余数量 FIFO ----------
DO $bind$
DECLARE
    r_line record;
    r_po record;
    r_pr record;
    pr_need numeric(18, 4);
    pr_remaining jsonb;
BEGIN
    FOR r_line IN
        SELECT DISTINCT poi.sell_order_item_id
        FROM public.purchaseorderitem poi
        LEFT JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
        LEFT JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
        WHERE poi.purchase_requisition_id IS NULL
          AND poi.sell_order_item_id IS NOT NULL
          AND poi.status NOT IN (-1, -2)
          -- AND so.sell_order_code = 'SO00044'
    LOOP
        pr_remaining := '{}'::jsonb;

        FOR r_pr IN
            SELECT pr."PurchaseRequisitionId" AS pr_id, pr.qty
            FROM public.purchaserequisition pr
            WHERE pr.sell_order_item_id = r_line.sell_order_item_id
            ORDER BY pr."CreateTime", pr."PurchaseRequisitionId"
        LOOP
            pr_remaining := pr_remaining || jsonb_build_object(r_pr.pr_id, r_pr.qty);
        END LOOP;

        IF pr_remaining = '{}'::jsonb THEN
            CONTINUE;
        END IF;

        FOR r_po IN
            SELECT poi."PurchaseOrderItemId" AS poi_id, poi.qty
            FROM public.purchaseorderitem poi
            WHERE poi.sell_order_item_id = r_line.sell_order_item_id
              AND poi.purchase_requisition_id IS NULL
              AND poi.status NOT IN (-1, -2)
            ORDER BY poi.purchase_order_item_code
        LOOP
            FOR r_pr IN
                SELECT pr."PurchaseRequisitionId" AS pr_id, pr.qty
                FROM public.purchaserequisition pr
                WHERE pr.sell_order_item_id = r_line.sell_order_item_id
                ORDER BY pr."CreateTime", pr."PurchaseRequisitionId"
            LOOP
                pr_need := COALESCE((pr_remaining ->> r_pr.pr_id)::numeric, 0);
                IF pr_need <= 0 THEN
                    CONTINUE;
                END IF;

                UPDATE public.purchaseorderitem
                SET purchase_requisition_id = r_pr.pr_id
                WHERE "PurchaseOrderItemId" = r_po.poi_id
                  AND purchase_requisition_id IS NULL;

                pr_remaining := jsonb_set(
                    pr_remaining,
                    ARRAY[r_pr.pr_id],
                    to_jsonb(pr_need - r_po.qty),
                    true
                );
                EXIT;
            END LOOP;
        END LOOP;
    END LOOP;
END
$bind$;

-- ---------- 5. 验证：仍未绑定的 PO 行 ----------
SELECT
    so.sell_order_code,
    soi.sell_order_item_code,
    poi.purchase_order_item_code,
    poi.qty,
    poi.sell_order_item_id,
    poi.purchase_requisition_id
FROM public.purchaseorderitem poi
JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
WHERE poi.purchase_requisition_id IS NULL
  AND poi.status NOT IN (-1, -2)
  -- AND so.sell_order_code = 'SO00044'
ORDER BY so.sell_order_code, poi.purchase_order_item_code;

-- ---------- 6. 重算 PR.status（绑定后必跑；全库 PR，幂等）----------
WITH pr_rows AS (
    SELECT
        pr."PurchaseRequisitionId" AS pr_id,
        pr.qty AS pr_qty,
        pr.sell_order_item_id,
        pr."CreateTime" AS create_time
    FROM public.purchaserequisition pr
    WHERE pr.status <> 3
),
explicit AS (
    SELECT poi.purchase_requisition_id AS pr_id, SUM(poi.qty) AS explicit_qty
    FROM public.purchaseorderitem poi
    WHERE poi.purchase_requisition_id IS NOT NULL
      AND poi.status NOT IN (-1, -2)
    GROUP BY poi.purchase_requisition_id
),
unlinked_by_so AS (
    SELECT poi.sell_order_item_id, SUM(poi.qty) AS unlinked_qty
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
        explicit_qty + GREATEST(0, LEAST(pr_qty, unlinked_qty - (cum_pr_qty - pr_qty))) AS linked_qty
    FROM fifo
)
UPDATE public.purchaserequisition pr
SET status = CASE
    WHEN c.linked_qty <= 0 THEN 0
    WHEN c.linked_qty < c.pr_qty THEN 1
    ELSE 2
END
FROM calc c
WHERE pr."PurchaseRequisitionId" = c.pr_id
  AND pr.status <> CASE
    WHEN c.linked_qty <= 0 THEN 0
    WHEN c.linked_qty < c.pr_qty THEN 1
    ELSE 2
  END;

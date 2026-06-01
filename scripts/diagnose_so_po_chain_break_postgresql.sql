-- =============================================================================
-- SO / PR / PO / 入库 / stock_item 链条断点诊断（PostgreSQL）
-- 现象：库存已有 PO0021N-1 且带客户，但销行 SO0020L-3 扩展仍为「待采购」
-- 根因多为：purchaseorderitem.sell_order_item_id 与 SO0020L-3 的主键不一致
-- =============================================================================

-- 1) 销售单 SO0020L 全部明细行（看主键 GUID 与行号）
SELECT
    soi."SellOrderItemId",
    soi.sell_order_item_code,
    soi.pn,
    soi.brand,
    soi.qty,
    soi.purchased_qty,
    ext."PurchaseProgressStatus",
    ext."StockInProgressStatus",
    ext."QtyAlreadyPurchased"
FROM public.sellorderitem soi
JOIN public.sellorder so ON so."SellOrderId" = soi.sell_order_id
LEFT JOIN public.sellorderitemextend ext ON ext."SellOrderItemId" = soi."SellOrderItemId"
WHERE so.sell_order_code = 'SO0020L'
ORDER BY soi.sell_order_item_code;

-- 2) 采购申请 POR001ZK 绑定的销行
SELECT
    pr.bill_code,
    pr.sell_order_item_id AS pr_sell_line_id,
    soi.sell_order_item_code AS pr_so_line_code
FROM public.purchaserequisition pr
LEFT JOIN public.sellorderitem soi ON soi."SellOrderItemId" = pr.sell_order_item_id
WHERE pr.bill_code = 'POR001ZK';

-- 3) 采购单 PO0021N 明细实际绑定的销行（关键）
SELECT
    po.purchase_order_code,
    po.status AS po_header_status,
    poi.purchase_order_item_code,
    poi.sell_order_item_id AS po_sell_line_id,
    soi.sell_order_item_code AS po_so_line_code,
    poi.qty
FROM public.purchaseorder po
JOIN public.purchaseorderitem poi ON poi.purchase_order_id = po."PurchaseOrderId"
LEFT JOIN public.sellorderitem soi ON soi."SellOrderItemId" = poi.sell_order_item_id
WHERE po.purchase_order_code = 'PO0021N';

-- 4) 对比：SO0020L-3 的 Id 是否等于 PO 上的 sell_order_item_id
SELECT
    soi3."SellOrderItemId" AS so_line_3_id,
    soi3.sell_order_item_code,
    poi.sell_order_item_id AS po_linked_id,
    CASE
        WHEN poi.sell_order_item_id IS NULL THEN 'PO销行为空(备货或未关联)'
        WHEN poi.sell_order_item_id = soi3."SellOrderItemId" THEN '链条一致'
        ELSE '链条断裂：PO绑在其它销行'
    END AS chain_status,
    soi_po.sell_order_item_code AS po_actual_so_line_code
FROM public.sellorderitem soi3
JOIN public.sellorder so ON so."SellOrderId" = soi3.sell_order_id
LEFT JOIN public.purchaseorder po ON po.purchase_order_code = 'PO0021N'
LEFT JOIN public.purchaseorderitem poi ON poi.purchase_order_id = po."PurchaseOrderId"
LEFT JOIN public.sellorderitem soi_po ON soi_po."SellOrderItemId" = poi.sell_order_item_id
WHERE so.sell_order_code = 'SO0020L'
  AND soi3.sell_order_item_code = 'SO0020L-3';

-- 5) 库存层（已入库）绑定的销行
SELECT
    si.stock_item_code,
    si.purchase_order_item_code,
    si.sell_order_item_id AS stock_item_sell_line_id,
    soi.sell_order_item_code AS stock_item_so_line_code
FROM public.stock_item si
LEFT JOIN public.sellorderitem soi ON soi."SellOrderItemId" = si.sell_order_item_id
WHERE si.stock_item_code = 'STK0020V-1';

-- 待报关 / 境外可用库存 自查（列名与库表一致）
-- customs_pendlist: create_time（非 CreateTime）
-- stock_item: "StockItemId", "RegionType", "Type", "QtyRepertoryAvailable"（PascalCase）
-- warehouseinfo: 仓库表（非 warehouse / warehouse_info）；主键 "Id"，名称 "WarehouseName"
-- 待报关 status: 1=Open（非 0）

-- ---------------------------------------------------------------------------
-- 1) 待报关记录（待处理 Open = 1）
-- ---------------------------------------------------------------------------
SELECT
    p.id,
    p.sell_order_item_id,
    p.qty,
    p.overseas_warehouse_id,
    p.status,
    p.create_time
FROM customs_pendlist p
WHERE COALESCE(p.is_deleted, false) = false
  AND p.status = 1
ORDER BY p.create_time DESC;

-- ---------------------------------------------------------------------------
-- 2) 生成报关出库通知时统计的「境外客单可用」（与 EnsureOverseasStockCoversQtyAsync 一致）
--    替换 <销售明细ID>
-- ---------------------------------------------------------------------------
SELECT
    si."StockItemId",
    si.sell_order_item_id,
    si."RegionType",
    si."Type" AS stock_type,
    si."WarehouseId",
    si."QtyRepertory",
    si."QtyRepertoryAvailable"
FROM stock_item si
WHERE si.sell_order_item_id = '<销售明细ID>'
  AND si."RegionType" = 20
  AND si."QtyRepertoryAvailable" > 0
  AND COALESCE(si.is_deleted, false) = false;

SELECT COALESCE(SUM(si."QtyRepertoryAvailable"), 0) AS overseas_avail_customer_only
FROM stock_item si
WHERE si.sell_order_item_id = '<销售明细ID>'
  AND si."RegionType" = 20
  AND si."QtyRepertoryAvailable" > 0
  AND COALESCE(si.is_deleted, false) = false;

-- ---------------------------------------------------------------------------
-- 3) 同 PN+品牌 备货境外（销售订单库存 Tab 会显示，生成报关出库通知不算）
--    替换 <PN>、<品牌>
-- ---------------------------------------------------------------------------
SELECT
    si."StockItemId",
    si."Type",
    si."RegionType",
    si.purchase_pn,
    si.purchase_brand,
    si."QtyRepertoryAvailable"
FROM stock_item si
WHERE si."Type" = 2
  AND si."RegionType" = 20
  AND si."QtyRepertoryAvailable" > 0
  AND COALESCE(si.is_deleted, false) = false
  AND UPPER(TRIM(si.purchase_pn)) = UPPER(TRIM('<PN>'))
  AND UPPER(TRIM(si.purchase_brand)) = UPPER(TRIM('<品牌>'));

-- ---------------------------------------------------------------------------
-- 4) 按 PN 查境外可用库存（含仓库名）
--    替换 <PN>
-- ---------------------------------------------------------------------------
SELECT
    si."StockItemId",
    si.sell_order_item_id,
    si.sell_order_item_code,
    si."RegionType",
    si."Type",
    si."QtyRepertory",
    si."QtyRepertoryAvailable",
    w."WarehouseName"
FROM stock_item si
LEFT JOIN warehouseinfo w ON w."Id" = si."WarehouseId"
WHERE UPPER(TRIM(si.purchase_pn)) = UPPER(TRIM('<PN>'))
  AND COALESCE(si.is_deleted, false) = false
  AND si."QtyRepertoryAvailable" > 0
ORDER BY si."RegionType", si."Type";

-- ---------------------------------------------------------------------------
-- 5) 待报关 + 境外客单可用 + 备货池（一次看清 UI 与 API 差异）
-- ---------------------------------------------------------------------------
SELECT
    p.id AS pendlist_id,
    p.sell_order_item_id,
    p.qty AS pendlist_qty,
    soi.pn,
    soi.brand,
    COALESCE(cust.overseas_avail, 0) AS overseas_avail_customer_only,
    COALESCE(pool.overseas_stocking_avail, 0) AS overseas_stocking_pool_avail
FROM customs_pendlist p
JOIN sellorderitem soi ON soi."SellOrderItemId" = p.sell_order_item_id
LEFT JOIN LATERAL (
    SELECT SUM(si."QtyRepertoryAvailable") AS overseas_avail
    FROM stock_item si
    WHERE si.sell_order_item_id = p.sell_order_item_id
      AND si."RegionType" = 20
      AND si."QtyRepertoryAvailable" > 0
      AND COALESCE(si.is_deleted, false) = false
) cust ON true
LEFT JOIN LATERAL (
    SELECT SUM(si."QtyRepertoryAvailable") AS overseas_stocking_avail
    FROM stock_item si
    WHERE si."Type" = 2
      AND si."RegionType" = 20
      AND si."QtyRepertoryAvailable" > 0
      AND COALESCE(si.is_deleted, false) = false
      AND UPPER(TRIM(si.purchase_pn)) = UPPER(TRIM(soi.pn))
      AND UPPER(TRIM(si.purchase_brand)) = UPPER(TRIM(soi.brand))
) pool ON true
WHERE COALESCE(p.is_deleted, false) = false
  AND p.status = 1
ORDER BY p.create_time DESC;

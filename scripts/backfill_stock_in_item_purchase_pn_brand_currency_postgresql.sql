-- =============================================================================
-- 回填 public.stock_in_item：purchase_pn、purchase_brand、currency
-- 数据来源：public.purchaseorderitem（pn / brand / currency）
-- 关联口径（与后端 StockIn 逻辑一致）：入库明细 MaterialId = 采购明细主键
-- 可选兜底：stock_in_item.purchase_order_item_id、stock_in_item_extend.purchase_order_item_id
--
-- 执行前请先 PREVIEW；建议在事务中执行并核对受影响行数。
-- =============================================================================

-- ----- 预览：按 MaterialId 能关联到的采购行（仅看将补哪些行） -----
-- SELECT
--   sii."ItemId",
--   sii.stock_in_item_code,
--   sii."MaterialId" AS material_as_po_item_id,
--   sii.purchase_pn,
--   sii.purchase_brand,
--   sii.currency,
--   poi.pn AS poi_pn,
--   poi.brand AS poi_brand,
--   poi.currency AS poi_currency
-- FROM public.stock_in_item sii
-- INNER JOIN public.purchaseorderitem poi
--   ON poi."PurchaseOrderItemId" = NULLIF(btrim(sii."MaterialId"), '')
-- WHERE (
--     sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = ''
--     OR sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = ''
--     OR sii.currency IS NULL
--   )
-- LIMIT 200;

BEGIN;

-- 1) 主路径：MaterialId = purchaseorderitem.PurchaseOrderItemId
--    仅填空：已有非空值的列不覆盖
UPDATE public.stock_in_item sii
SET
  purchase_pn = CASE
    WHEN sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = '' THEN NULLIF(btrim(poi.pn), '')
    ELSE sii.purchase_pn
  END,
  purchase_brand = CASE
    WHEN sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = '' THEN NULLIF(btrim(poi.brand), '')
    ELSE sii.purchase_brand
  END,
  currency = CASE
    WHEN sii.currency IS NULL THEN poi.currency
    ELSE sii.currency
  END
FROM public.purchaseorderitem poi
WHERE NULLIF(btrim(sii."MaterialId"), '') = poi."PurchaseOrderItemId"
  AND (
    sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = ''
    OR sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = ''
    OR sii.currency IS NULL
  );

-- 2) 兜底 A：主表已有 purchase_order_item_id，但与 MaterialId 不一致或未命中上一步时
UPDATE public.stock_in_item sii
SET
  purchase_pn = CASE
    WHEN sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = '' THEN NULLIF(btrim(poi.pn), '')
    ELSE sii.purchase_pn
  END,
  purchase_brand = CASE
    WHEN sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = '' THEN NULLIF(btrim(poi.brand), '')
    ELSE sii.purchase_brand
  END,
  currency = CASE
    WHEN sii.currency IS NULL THEN poi.currency
    ELSE sii.currency
  END
FROM public.purchaseorderitem poi
WHERE NULLIF(btrim(sii.purchase_order_item_id), '') = poi."PurchaseOrderItemId"
  AND (
    sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = ''
    OR sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = ''
    OR sii.currency IS NULL
  );

-- 3) 兜底 B：扩展表 stock_in_item_extend.purchase_order_item_id（与主键 StockInItemId = 明细 ItemId）
UPDATE public.stock_in_item sii
SET
  purchase_pn = CASE
    WHEN sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = '' THEN NULLIF(btrim(poi.pn), '')
    ELSE sii.purchase_pn
  END,
  purchase_brand = CASE
    WHEN sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = '' THEN NULLIF(btrim(poi.brand), '')
    ELSE sii.purchase_brand
  END,
  currency = CASE
    WHEN sii.currency IS NULL THEN poi.currency
    ELSE sii.currency
  END
FROM public.stock_in_item_extend ext
INNER JOIN public.purchaseorderitem poi
  ON poi."PurchaseOrderItemId" = NULLIF(btrim(ext.purchase_order_item_id), '')
WHERE ext."StockInItemId" = sii."ItemId"
  AND NULLIF(btrim(ext.purchase_order_item_id), '') IS NOT NULL
  AND (
    sii.purchase_pn IS NULL OR btrim(sii.purchase_pn) = ''
    OR sii.purchase_brand IS NULL OR btrim(sii.purchase_brand) = ''
    OR sii.currency IS NULL
  );

COMMIT;

-- 币别说明（与系统一致）：1=RMB  2=USD  3=EUR  4=HKD

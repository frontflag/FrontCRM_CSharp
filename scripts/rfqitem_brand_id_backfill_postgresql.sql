-- rfqitem.brand_id 回填：按 biz_brand 匹配 rfqitem.brand 文本
-- 前置：已执行 rfq_item_brand_id_postgresql.sql（存在 brand_id 列）
-- 匹配规则：TRIM + 忽略大小写；同名多条品牌时优先已审核(2)，再取最小 id

-- ---------- 1. 回填前检查 ----------
-- SELECT COUNT(*) FROM public.rfqitem
-- WHERE brand_id IS NULL AND COALESCE(TRIM(brand), '') <> '';

-- ---------- 2. 主回填：standard_brand ----------
BEGIN;

WITH brand_pick AS (
  SELECT DISTINCT ON (LOWER(TRIM(standard_brand)))
    id,
    LOWER(TRIM(standard_brand)) AS norm_key
  FROM public.biz_brand
  WHERE is_deleted = false
    AND standard_brand IS NOT NULL
    AND TRIM(standard_brand) <> ''
  ORDER BY
    LOWER(TRIM(standard_brand)),
    CASE WHEN audit_status = 2 THEN 0 ELSE 1 END,
    id
)
UPDATE public.rfqitem AS i
SET brand_id = b.id
FROM brand_pick AS b
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
  AND LOWER(TRIM(i.brand)) = b.norm_key;

COMMIT;

-- ---------- 3.（可选）仍未匹配：按 brand_e_name ----------
BEGIN;

WITH brand_pick_en AS (
  SELECT DISTINCT ON (LOWER(TRIM(brand_e_name)))
    id,
    LOWER(TRIM(brand_e_name)) AS norm_key
  FROM public.biz_brand
  WHERE is_deleted = false
    AND brand_e_name IS NOT NULL
    AND TRIM(brand_e_name) <> ''
  ORDER BY
    LOWER(TRIM(brand_e_name)),
    CASE WHEN audit_status = 2 THEN 0 ELSE 1 END,
    id
)
UPDATE public.rfqitem AS i
SET brand_id = b.id
FROM brand_pick_en AS b
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
  AND LOWER(TRIM(i.brand)) = b.norm_key;

COMMIT;

-- ---------- 4.（可选）仍未匹配：按 brand_c_name ----------
BEGIN;

WITH brand_pick_cn AS (
  SELECT DISTINCT ON (LOWER(TRIM(brand_c_name)))
    id,
    LOWER(TRIM(brand_c_name)) AS norm_key
  FROM public.biz_brand
  WHERE is_deleted = false
    AND brand_c_name IS NOT NULL
    AND TRIM(brand_c_name) <> ''
  ORDER BY
    LOWER(TRIM(brand_c_name)),
    CASE WHEN audit_status = 2 THEN 0 ELSE 1 END,
    id
)
UPDATE public.rfqitem AS i
SET brand_id = b.id
FROM brand_pick_cn AS b
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
  AND LOWER(TRIM(i.brand)) = b.norm_key;

COMMIT;

-- ---------- 5. 核对 ----------
-- SELECT
--   COUNT(*) FILTER (WHERE brand_id IS NOT NULL) AS has_brand_id,
--   COUNT(*) FILTER (WHERE brand_id IS NULL AND COALESCE(TRIM(brand), '') <> '') AS still_missing,
--   COUNT(*) AS total
-- FROM public.rfqitem;

-- ---------- 6. 仍无法匹配：自动建档待审核并回填 ----------
-- 见 scripts/rfqitem_brand_id_backfill_create_pending_postgresql.sql

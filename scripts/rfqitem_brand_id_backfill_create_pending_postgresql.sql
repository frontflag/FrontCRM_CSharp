-- rfqitem 无法匹配 biz_brand 的 brand 文本：
--   1) 在 biz_brand 插入待审核记录（audit_status = 1）
--   2) 按 standard_brand 回填 rfqitem.brand_id
--
-- 前置：
--   - rfq_item_brand_id_postgresql.sql（brand_id 列）
--   - 建议先执行 rfqitem_brand_id_backfill_postgresql.sql 中 2~4 步匹配已有品牌
--
-- 说明：英文名/中文名/标准名均用 rfqitem.brand 文本（截断至列长），remark 标记来源。

-- ---------- 预览：仍将建档的品牌文本 ----------
-- SELECT DISTINCT TRIM(i.brand) AS brand_text, COUNT(*) AS line_count
-- FROM public.rfqitem i
-- WHERE i.brand_id IS NULL
--   AND COALESCE(TRIM(i.brand), '') <> ''
--   AND NOT EXISTS (
--     SELECT 1
--     FROM public.biz_brand b
--     WHERE b.is_deleted = false
--       AND (
--         LOWER(TRIM(COALESCE(b.standard_brand, ''))) = LOWER(TRIM(i.brand))
--         OR LOWER(TRIM(COALESCE(b.brand_e_name, ''))) = LOWER(TRIM(i.brand))
--         OR LOWER(TRIM(COALESCE(b.brand_c_name, ''))) = LOWER(TRIM(i.brand))
--       )
--   )
-- ORDER BY line_count DESC, brand_text;

BEGIN;

-- 1) 为仍未匹配的品牌文本创建待审核主数据（每个忽略大小写的 brand 一条）
WITH unmatched AS (
  SELECT DISTINCT ON (LOWER(TRIM(i.brand)))
    TRIM(i.brand) AS brand_text
  FROM public.rfqitem i
  WHERE i.brand_id IS NULL
    AND COALESCE(TRIM(i.brand), '') <> ''
    AND NOT EXISTS (
      SELECT 1
      FROM public.biz_brand b
      WHERE b.is_deleted = false
        AND (
          LOWER(TRIM(COALESCE(b.standard_brand, ''))) = LOWER(TRIM(i.brand))
          OR LOWER(TRIM(COALESCE(b.brand_e_name, ''))) = LOWER(TRIM(i.brand))
          OR LOWER(TRIM(COALESCE(b.brand_c_name, ''))) = LOWER(TRIM(i.brand))
        )
    )
  ORDER BY LOWER(TRIM(i.brand))
)
INSERT INTO public.biz_brand (
  brand_e_name,
  brand_c_name,
  standard_brand,
  remark,
  create_time,
  audit_status,
  is_deleted
)
SELECT
  LEFT(u.brand_text, 200),
  LEFT(u.brand_text, 200),
  LEFT(u.brand_text, 300),
  'RFQ历史明细 brand 自动建档（待审核）',
  NOW(),
  1,
  false
FROM unmatched u;

-- 2) 按 standard_brand 回填 brand_id（含刚插入的待审核品牌）
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

-- ---------- 核对 ----------
-- SELECT
--   COUNT(*) FILTER (WHERE brand_id IS NOT NULL) AS has_brand_id,
--   COUNT(*) FILTER (WHERE brand_id IS NULL AND COALESCE(TRIM(brand), '') <> '') AS still_missing,
--   COUNT(*) AS total
-- FROM public.rfqitem;
--
-- SELECT COUNT(*) AS pending_from_rfq_import
-- FROM public.biz_brand
-- WHERE audit_status = 1
--   AND remark = 'RFQ历史明细 brand 自动建档（待审核）';

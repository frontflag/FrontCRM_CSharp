-- 处理 rfqitem 仍缺 brand_id 的明细（brand 有文本但 brand_id 为空）
-- 步骤：先诊断 → 再尝试宽匹配已有品牌 → 仍缺则建档待审核 → 最后回填
--
-- 核对列含义：
--   has_brand_id   = 已有 brand_id（已处理）
--   still_missing  = brand 非空但 brand_id 仍为空（待处理）

-- ========== A. 诊断：仍缺 brand_id 的明细 ==========
SELECT
  i.item_id,
  i.rfq_id,
  TRIM(i.brand) AS brand_text,
  LENGTH(TRIM(i.brand)) AS brand_len,
  i.brand_id
FROM public.rfqitem i
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
ORDER BY brand_text, i.item_id;

-- 按品牌文本汇总
SELECT
  TRIM(i.brand) AS brand_text,
  COUNT(*) AS line_count
FROM public.rfqitem i
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
GROUP BY TRIM(i.brand)
ORDER BY line_count DESC, brand_text;

-- 库中是否已有同名（standard / 英文 / 中文 / alias）
SELECT
  TRIM(i.brand) AS rfq_brand_text,
  b.id,
  b.standard_brand,
  b.brand_e_name,
  b.brand_c_name,
  b.alias,
  b.is_deleted,
  b.audit_status
FROM public.rfqitem i
JOIN public.biz_brand b
  ON (
    LOWER(TRIM(COALESCE(b.standard_brand, ''))) = LOWER(TRIM(i.brand))
    OR LOWER(TRIM(COALESCE(b.brand_e_name, ''))) = LOWER(TRIM(i.brand))
    OR LOWER(TRIM(COALESCE(b.brand_c_name, ''))) = LOWER(TRIM(i.brand))
    OR LOWER(TRIM(COALESCE(b.alias, ''))) = LOWER(TRIM(i.brand))
  )
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> ''
ORDER BY rfq_brand_text, b.is_deleted, b.id;

-- 是否因 biz_brand 已软删且仅 standard_brand 同名（上一条无结果时可看此项）
SELECT
  TRIM(i.brand) AS brand_text,
  b.id,
  b.standard_brand,
  b.is_deleted,
  b.audit_status
FROM public.rfqitem i
JOIN public.biz_brand b
  ON LOWER(TRIM(COALESCE(b.standard_brand, ''))) = LOWER(TRIM(i.brand))
WHERE i.brand_id IS NULL
  AND COALESCE(TRIM(i.brand), '') <> '';

-- ========== B. 宽匹配：standard / 英文名 / 中文名（优先未删、已审核）==========
BEGIN;

WITH pick AS (
  SELECT
    i.item_id,
    (
      SELECT b2.id
      FROM public.biz_brand b2
      WHERE (
        LOWER(TRIM(COALESCE(b2.standard_brand, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b2.brand_e_name, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b2.brand_c_name, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b2.alias, ''))) = LOWER(TRIM(i.brand))
      )
      ORDER BY
        CASE WHEN b2.is_deleted THEN 1 ELSE 0 END,
        CASE WHEN b2.audit_status = 2 THEN 0 ELSE 1 END,
        b2.id
      LIMIT 1
    ) AS brand_id
  FROM public.rfqitem i
  WHERE i.brand_id IS NULL
    AND COALESCE(TRIM(i.brand), '') <> ''
)
UPDATE public.rfqitem AS i
SET brand_id = p.brand_id
FROM pick AS p
WHERE i.item_id = p.item_id
  AND p.brand_id IS NOT NULL;

COMMIT;

-- ========== C. 仍为 unmatched 的文本：建档待审核 + 回填 ==========
BEGIN;

WITH unmatched AS (
  SELECT DISTINCT ON (LOWER(TRIM(i.brand)))
    TRIM(i.brand) AS brand_text
  FROM public.rfqitem i
  WHERE i.brand_id IS NULL
    AND COALESCE(TRIM(i.brand), '') <> ''
    AND NOT EXISTS (
      SELECT 1
      FROM public.biz_brand b
      WHERE (
        LOWER(TRIM(COALESCE(b.standard_brand, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b.brand_e_name, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b.brand_c_name, ''))) = LOWER(TRIM(i.brand))
        OR LOWER(TRIM(COALESCE(b.alias, ''))) = LOWER(TRIM(i.brand))
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

-- ========== D. 最终核对（still_missing 应为 0）==========
SELECT
  COUNT(*) FILTER (WHERE brand_id IS NOT NULL) AS has_brand_id,
  COUNT(*) FILTER (WHERE brand_id IS NULL AND COALESCE(TRIM(brand), '') <> '') AS still_missing,
  COUNT(*) AS total
FROM public.rfqitem;

-- biz_brand：按 (brand_e_name, brand_c_name, standard_brand) 去重
-- 规则：三字段完全相同视为重复；每组只保留 id 最小的一行（最早插入）

-- ========== 1) 去重前：查看重复组 ==========
SELECT
    brand_e_name,
    brand_c_name,
    standard_brand,
    count(*) AS dup_count,
    min(id) AS keep_id,
    array_agg(id ORDER BY id) AS all_ids
FROM public.biz_brand
GROUP BY brand_e_name, brand_c_name, standard_brand
HAVING count(*) > 1
ORDER BY dup_count DESC, keep_id;

-- ========== 2) 去重前：将删除的行数 ==========
SELECT count(*) AS rows_to_delete
FROM public.biz_brand a
WHERE EXISTS (
    SELECT 1
    FROM public.biz_brand b
    WHERE b.id < a.id
      AND a.brand_e_name IS NOT DISTINCT FROM b.brand_e_name
      AND a.brand_c_name IS NOT DISTINCT FROM b.brand_c_name
      AND a.standard_brand IS NOT DISTINCT FROM b.standard_brand
);

-- ========== 3) 执行去重（保留每组 id 最小行） ==========
DELETE FROM public.biz_brand a
USING public.biz_brand b
WHERE a.id > b.id
  AND a.brand_e_name IS NOT DISTINCT FROM b.brand_e_name
  AND a.brand_c_name IS NOT DISTINCT FROM b.brand_c_name
  AND a.standard_brand IS NOT DISTINCT FROM b.standard_brand;

-- ========== 4) 去重后验证（应无结果） ==========
SELECT
    brand_e_name,
    brand_c_name,
    standard_brand,
    count(*) AS dup_count
FROM public.biz_brand
GROUP BY brand_e_name, brand_c_name, standard_brand
HAVING count(*) > 1;

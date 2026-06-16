-- biz_brand 导入并去重后：恢复业务唯一性（按需执行）
-- 建议先去重，例如：
--   DELETE FROM public.biz_brand a
--   USING public.biz_brand b
--   WHERE a.id > b.id
--     AND lower(btrim(coalesce(a.standard_brand, ''))) = lower(btrim(coalesce(b.standard_brand, '')))
--     AND btrim(coalesce(a.standard_brand, '')) <> '';

-- 标准品牌名唯一（非空时）
CREATE UNIQUE INDEX IF NOT EXISTS "UX_biz_brand_standard_brand"
    ON public.biz_brand (standard_brand)
    WHERE standard_brand IS NOT NULL AND btrim(standard_brand) <> '';

-- 若需「英文名 + 国家代码」组合唯一，取消下行注释：
-- CREATE UNIQUE INDEX IF NOT EXISTS "UX_biz_brand_e_name_country_code"
--     ON public.biz_brand (brand_e_name, country_code)
--     WHERE brand_e_name IS NOT NULL AND btrim(brand_e_name) <> '';

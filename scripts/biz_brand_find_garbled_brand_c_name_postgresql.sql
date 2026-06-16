-- biz_brand：查找 brand_c_name 含乱码的记录
--
-- 截图类问题：转码失败产生 Unicode 替换符 U+FFFD（界面常显示为 ）
-- 另含：UTF-8 误读为 Latin-1 的残留字符、不可见控制字符
--
-- 用法：在 psql / DBeaver 中执行「查询 1」；需要只看替换符时用「查询 3」

-- ========== 查询 1：所有疑似乱码记录（推荐） ==========
SELECT
    id,
    brand_e_name,
    brand_c_name,
    standard_brand,
    alias,
    country_code,
    country
FROM public.biz_brand
WHERE brand_c_name IS NOT NULL
  AND btrim(brand_c_name) <> ''
  AND (
        -- 替换符（对应 n鼠旺 这类）
        position(U&'\FFFD' IN brand_c_name) > 0
        -- 常见 Mojibake 残留
     OR brand_c_name ~ '[ÃÂÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ¤]'
        -- 控制字符（不含 Tab/LF/CR）
     OR brand_c_name ~ '[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]'
  )
ORDER BY id;

-- ========== 查询 2：统计条数 ==========
SELECT count(*) AS garbled_row_count
FROM public.biz_brand
WHERE brand_c_name IS NOT NULL
  AND btrim(brand_c_name) <> ''
  AND (
        position(U&'\FFFD' IN brand_c_name) > 0
     OR brand_c_name ~ '[ÃÂÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ¤]'
     OR brand_c_name ~ '[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]'
  );

-- ========== 查询 3：仅 Unicode 替换符 U+FFFD（最精准） ==========
SELECT
    id,
    brand_e_name,
    brand_c_name,
    standard_brand
FROM public.biz_brand
WHERE brand_c_name IS NOT NULL
  AND position(U&'\FFFD' IN brand_c_name) > 0
ORDER BY id;

-- ========== 查询 4：导出待人工核对（含乱码原因标签） ==========
SELECT
    id,
    brand_e_name,
    brand_c_name,
    standard_brand,
    CASE
        WHEN position(U&'\FFFD' IN brand_c_name) > 0 THEN 'replacement_char'
        WHEN brand_c_name ~ '[ÃÂÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ¤]' THEN 'mojibake_latin'
        WHEN brand_c_name ~ '[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]' THEN 'control_char'
        ELSE 'other'
    END AS garbled_reason
FROM public.biz_brand
WHERE brand_c_name IS NOT NULL
  AND btrim(brand_c_name) <> ''
  AND (
        position(U&'\FFFD' IN brand_c_name) > 0
     OR brand_c_name ~ '[ÃÂÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ¤]'
     OR brand_c_name ~ '[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]'
  )
ORDER BY id;

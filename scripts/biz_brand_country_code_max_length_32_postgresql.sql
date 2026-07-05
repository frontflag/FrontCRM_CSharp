-- biz_brand.country_code：varchar(10) → varchar(32)（可重复执行）
ALTER TABLE IF EXISTS public.biz_brand
  ALTER COLUMN country_code TYPE character varying(32);

COMMENT ON COLUMN public.biz_brand.country_code IS '国家/地区代码（CountryCode）';

-- biz_brand 增加「备注」字段
ALTER TABLE public.biz_brand
  ADD COLUMN IF NOT EXISTS remark character varying(500) NULL;

COMMENT ON COLUMN public.biz_brand.remark IS '备注';

-- 验证
-- SELECT id, brand_e_name, brand_c_name, remark FROM public.biz_brand ORDER BY id LIMIT 10;

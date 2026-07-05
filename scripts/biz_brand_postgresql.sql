-- 品牌主数据表 biz_brand（新建库 + 已有旧表升级，均可重复执行）
-- 审核状态 audit_status：1=待审核，2=已审核

-- 1) 表不存在时：按完整结构创建
CREATE TABLE IF NOT EXISTS public.biz_brand (
    id BIGSERIAL PRIMARY KEY,
    brand_e_name character varying(200) NULL,
    brand_c_name character varying(200) NULL,
    standard_brand character varying(300) NULL,
    alias character varying(500) NULL,
    country_code character varying(32) NULL,
    country character varying(100) NULL
);

-- 2) 表已存在（旧版缺列）时：补齐后续增加的字段
ALTER TABLE public.biz_brand
  ADD COLUMN IF NOT EXISTS remark character varying(500) NULL,
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS create_time timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS audit_status smallint NULL,
  ADD COLUMN IF NOT EXISTS audit_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS audit_time timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL;

COMMENT ON TABLE public.biz_brand IS '品牌主数据';
COMMENT ON COLUMN public.biz_brand.id IS '自增主键';
COMMENT ON COLUMN public.biz_brand.brand_e_name IS '品牌英文名（BrandEName）';
COMMENT ON COLUMN public.biz_brand.brand_c_name IS '品牌中文名（BrandCName）';
COMMENT ON COLUMN public.biz_brand.standard_brand IS '标准品牌名（StandardBrand）';
COMMENT ON COLUMN public.biz_brand.alias IS '别名（Alias）';
COMMENT ON COLUMN public.biz_brand.country_code IS '国家/地区代码（CountryCode）';
COMMENT ON COLUMN public.biz_brand.country IS '国家/地区名称（Country）';
COMMENT ON COLUMN public.biz_brand.remark IS '备注';
COMMENT ON COLUMN public.biz_brand.create_by_user_id IS '创建人用户ID（关联 user.UserId）';
COMMENT ON COLUMN public.biz_brand.create_time IS '创建日期';
COMMENT ON COLUMN public.biz_brand.audit_status IS '审核状态：1待审核，2已审核';
COMMENT ON COLUMN public.biz_brand.audit_by_user_id IS '审核人用户ID（关联 user.UserId）';
COMMENT ON COLUMN public.biz_brand.audit_time IS '审核日期';
COMMENT ON COLUMN public.biz_brand.is_deleted IS '是否已删除（软删除）';
COMMENT ON COLUMN public.biz_brand.deleted_at IS '删除时间';
COMMENT ON COLUMN public.biz_brand.deleted_by_user_id IS '删除操作人用户ID（关联 user.UserId）';

CREATE INDEX IF NOT EXISTS "IX_biz_brand_brand_e_name"
    ON public.biz_brand (brand_e_name);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_standard_brand"
    ON public.biz_brand (standard_brand);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_country_code"
    ON public.biz_brand (country_code);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_audit_status"
    ON public.biz_brand (audit_status);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_create_time"
    ON public.biz_brand (create_time);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_is_deleted"
    ON public.biz_brand (is_deleted);

-- 批量导入流程：
--   1) 若库中已有唯一约束：执行 biz_brand_disable_unique_for_import_postgresql.sql
--   2) 导入 Excel/CSV 数据
--   3) 去重后执行 biz_brand_enable_unique_after_import_postgresql.sql

-- 验证
-- SELECT column_name, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'biz_brand'
-- ORDER BY ordinal_position;
-- SELECT * FROM public.biz_brand ORDER BY id LIMIT 10;

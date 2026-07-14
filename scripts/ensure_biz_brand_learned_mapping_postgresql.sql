-- 品牌导入学习映射表（全公司共享：导入原文 source_key → biz_brand.id）
-- 冲突策略：source_key 唯一，后写覆盖 brand_id

CREATE TABLE IF NOT EXISTS public.biz_brand_learned_mapping (
    id BIGSERIAL PRIMARY KEY,
    source_text VARCHAR(500) NOT NULL,
    source_key VARCHAR(500) NOT NULL,
    brand_id BIGINT NOT NULL,
    hit_count INTEGER NOT NULL DEFAULT 1,
    last_used_by_user_id VARCHAR(36) NULL,
    create_by_user_id VARCHAR(36) NULL,
    create_time TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    update_time TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_biz_brand_learned_mapping_source_key
    ON public.biz_brand_learned_mapping (source_key);

CREATE INDEX IF NOT EXISTS ix_biz_brand_learned_mapping_brand_id
    ON public.biz_brand_learned_mapping (brand_id);

COMMENT ON TABLE public.biz_brand_learned_mapping IS '品牌导入学习映射（用户选手品牌时写入，全公司共享）';
COMMENT ON COLUMN public.biz_brand_learned_mapping.source_text IS '导入原文（展示用）';
COMMENT ON COLUMN public.biz_brand_learned_mapping.source_key IS '归一化键（查重，英文大小写不敏感）';
COMMENT ON COLUMN public.biz_brand_learned_mapping.brand_id IS '标准品牌 ID（biz_brand.id）';
COMMENT ON COLUMN public.biz_brand_learned_mapping.hit_count IS '命中/学习次数';

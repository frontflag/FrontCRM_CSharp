-- 复制 biz_brand 表结构 + 数据 → biz_brand_BK（备份表）
-- 若备份表已存在会先删除再重建

DROP TABLE IF EXISTS public."biz_brand_BK";

CREATE TABLE public."biz_brand_BK" (
    LIKE public.biz_brand INCLUDING ALL
);

INSERT INTO public."biz_brand_BK"
SELECT * FROM public.biz_brand;

COMMENT ON TABLE public."biz_brand_BK" IS 'biz_brand 备份表（结构+数据快照）';

-- 验证
-- SELECT count(*) AS src_cnt FROM public.biz_brand;
-- SELECT count(*) AS bk_cnt  FROM public."biz_brand_BK";
-- SELECT * FROM public."biz_brand_BK" ORDER BY id LIMIT 10;

-- ==========================================
-- 创建 debug 表（幂等）
-- 默认：public.debug（与腾讯云 Navicat 中「数据库 FrontCRM → 模式 public」一致）
-- ==========================================

CREATE TABLE IF NOT EXISTS public.debug (
    "Name" VARCHAR(32) PRIMARY KEY,
    "Value" VARCHAR(128) NOT NULL
);

INSERT INTO public.debug ("Name", "Value")
VALUES
    ('db_version', '1.0.0'),
    ('deploy_time', CURRENT_TIMESTAMP::TEXT),
    ('server_status', 'running')
ON CONFLICT ("Name") DO UPDATE SET
    "Value" = EXCLUDED."Value";

SELECT 'public.debug 表就绪' AS result;
SELECT * FROM public.debug;

-- 若你在库里单独建了 PostgreSQL「模式」（非数据库名），例如 my_app，且 .env 中设置了
--   DB_DEBUG_TABLE_SCHEMA=my_app
-- 则改为：
--   CREATE SCHEMA IF NOT EXISTS my_app;
--   CREATE TABLE IF NOT EXISTS my_app.debug ( ... 同上列 ... );

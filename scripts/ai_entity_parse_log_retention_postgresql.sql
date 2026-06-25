-- 清理超过保留期的 ai_entity_parse_log（默认 180 天，可通过 :keep_days 覆盖）
-- 用法：psql ... -v keep_days=180 -f scripts/ai_entity_parse_log_retention_postgresql.sql

DELETE FROM public.ai_entity_parse_log
WHERE created_at < NOW() - make_interval(days => COALESCE(NULLIF(:'keep_days', '')::int, 180));

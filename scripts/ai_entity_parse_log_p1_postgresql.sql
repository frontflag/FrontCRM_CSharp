-- P1：ai_entity_parse_log 增加 saved 追踪字段
-- 依赖：scripts/ai_entity_parse_log_postgresql.sql

ALTER TABLE IF EXISTS public.ai_entity_parse_log
  ADD COLUMN IF NOT EXISTS saved_biz_id character varying(64) NULL,
  ADD COLUMN IF NOT EXISTS saved_at timestamp with time zone NULL;

CREATE INDEX IF NOT EXISTS "IX_ai_entity_parse_log_saved_created"
  ON public.ai_entity_parse_log (saved_at DESC NULLS LAST)
  WHERE saved_at IS NOT NULL;

COMMENT ON COLUMN public.ai_entity_parse_log.saved_biz_id IS '用户保存成功后写入的业务实体 id';
COMMENT ON COLUMN public.ai_entity_parse_log.saved_at IS '保存成功时间（outcome=saved）';
COMMENT ON COLUMN public.ai_entity_parse_log.outcome IS 'parsed | confirmed | saved | failed';

-- 可选：全局保留天数（默认 180）；管理员亦可通过 API 手动 purge
INSERT INTO public.ai_global_config (config_key, config_value, description, modify_time)
VALUES (
  'entity_parse_log_retention_days',
  '180',
  'AI entity.parse 质量日志保留天数（purge 脚本/API 参考）',
  NOW()
)
ON CONFLICT (config_key) DO NOTHING;

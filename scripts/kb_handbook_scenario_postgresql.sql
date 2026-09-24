-- 增量：知识库问答场景、向量厂商、权限与距离阈值
-- DBeaver-safe：提示词占位符用 CHR 拼接，文件中不写双花括号字面量

INSERT INTO public.ai_provider (
    id, code, name, base_url, api_key_env, default_model, timeout_seconds, is_enabled
)
VALUES (
    'a1000001-0000-4000-8000-0000000000b1',
    'siliconflow',
    'SiliconFlow',
    'https://api.siliconflow.cn/v1',
    'AI_EMBEDDING_API_KEY',
    'BAAI/bge-m3',
    120,
    true
)
ON CONFLICT (code) DO UPDATE
SET base_url = EXCLUDED.base_url,
    api_key_env = EXCLUDED.api_key_env,
    default_model = EXCLUDED.default_model,
    is_enabled = true;

INSERT INTO public.kb_embedding_profile (
    id, provider_code, model, dimension, is_enabled
)
VALUES (
    'b1000001-0000-4000-8000-0000000000b1',
    'siliconflow',
    'BAAI/bge-m3',
    1024,
    true
)
ON CONFLICT (id) DO UPDATE
SET provider_code = EXCLUDED.provider_code,
    model = EXCLUDED.model,
    dimension = EXCLUDED.dimension,
    is_enabled = true,
    is_deleted = false;

INSERT INTO public.ai_global_config (config_key, config_value, description)
VALUES
    ('kb_handbook_max_top_distance', '0.45', '培训问答：最近一块距离超过该值则判定教材未覆盖'),
    ('kb_handbook_max_chunk_distance', '0.55', '培训问答：超过该距离的块不送入对话模型')
ON CONFLICT (config_key) DO NOTHING;

INSERT INTO public.ai_prompt_template (
    id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active
)
VALUES (
    'a2000001-0000-4000-8000-0000000000b1',
    'knowledge.handbook.qa',
    1,
    '你是电子元器件分销行业新人培训教材助教。只根据给定片段回答。数字、公式、阈值和话术必须与片段一致，不要补充公司制度或系统操作。片段不够时 covered 为 false，answer 为空字符串。只返回 JSON 对象，键为 covered 和 answer。',
    convert_from(decode('e997aee9a298efbc9a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'question' || CHR(125) || CHR(125)
        || convert_from(decode('0ae69599e69d90e78987e6aeb5efbc9a0a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'context' || CHR(125) || CHR(125)
        || convert_from(decode('0a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'compliance_hint' || CHR(125) || CHR(125),
    'json',
    'covered: boolean; answer: string',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search
)
VALUES (
    'a3000001-0000-4000-8000-0000000000b1',
    'knowledge.handbook.qa',
    '培训教材问答',
    '按教材切块检索后回答，不编造教材外的公司制度',
    'moonshot',
    'kimi-k2.6',
    'a2000001-0000-4000-8000-0000000000b1',
    604800,
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids'),
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids', 'context', 'compliance_hint'),
    2048,
    0.20,
    'biz.ai.kb.qa',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000d1', 'biz.ai.kb.qa', 'AI-培训教材问答', 'api', 'kb', 'ask', 1, NOW()),
    ('30000000-0000-4000-8000-0000000000d2', 'biz.ai.kb.admin', 'AI-培训教材管理', 'api', 'kb', 'admin', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('biz.ai.kb.qa', 'biz.ai.kb.admin')
  AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

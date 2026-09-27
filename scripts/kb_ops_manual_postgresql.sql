-- 系统操作手册问答：场景、提示词、提问权限。
-- 文档编码 ops.manual 由上传接口在首次导入时写入 kb_document，本脚本不插文档正文。
-- 导入仍用既有权限 biz.ai.kb.admin。执行后把「系统操作手册.md」上传到
-- POST /api/v1/kb/documents/ops.manual/versions ，待入库完成后再启用该版本。

INSERT INTO public.ai_prompt_template (
    id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active
)
VALUES (
    'a2000001-0000-4000-8000-0000000000b2',
    'knowledge.ops.qa',
    1,
    '你是 FrontCRM 系统操作手册助教。只根据给定片段回答操作步骤、业务流程和计算公式。数字和公式必须与片段一致，不要补充片段里没有的步骤或数字。片段不够时 covered 为 false，answer 为空字符串。只返回 JSON 对象，键为 covered 和 answer。',
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
    'a3000001-0000-4000-8000-0000000000b2',
    'knowledge.ops.qa',
    '系统操作手册问答',
    '按操作手册切块检索后回答，不编造手册外的步骤和公式',
    'moonshot',
    'kimi-k2.6',
    'a2000001-0000-4000-8000-0000000000b2',
    604800,
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids'),
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids', 'context', 'compliance_hint'),
    2048,
    0.20,
    'biz.ai.ops.qa',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000d3', 'biz.ai.ops.qa', 'AI-系统操作手册问答', 'api', 'kb', 'ops-ask', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.ops.qa'
  AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

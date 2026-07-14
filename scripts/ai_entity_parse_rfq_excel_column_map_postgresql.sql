-- 增量：entity.parse.rfq_excel_column_map（RFQ Excel 表头 AI 列映射，权限沿用 biz.ai.entity.parse.rfq）
-- DBeaver-safe：user_prompt 经 hex + CHR 拼接占位符

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-0000000000e1',
    'entity.parse.rfq_excel_column_map',
    1,
    '你是 CRM RFQ Excel 表头列映射助手。根据用户提供的 Excel 表头文本列表，将每一列映射到目标字段键名。仅输出合法 JSON（禁止 markdown）。键名 snake_case。field 必须是 target_fields 列表中的值之一，或 null 表示忽略该列。每个 field 最多映射一列；若多列疑似同一字段，选最匹配的一列，其余填 null。header_row_index 为 0-based 表头行索引（用户已指定时原样返回，否则输出 0）。禁止输出行数据或编造表头。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e8a1a8e5a4b4e58897e698a0e5b084efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082',
            'hex'
        ),
        'UTF8'
    )
    || CHR(10) || '表头列表：' || CHR(123) || CHR(123) || 'headers' || CHR(125) || CHR(125)
    || CHR(10) || '目标字段：' || CHR(123) || CHR(123) || 'target_fields' || CHR(125) || CHR(125),
    'json',
    '{"header_row_index":0,"columns":[{"col_index":0,"field":"customer_mpn","confidence":0.95},{"col_index":1,"field":"mpn","confidence":0.98},{"col_index":4,"field":"quantity","confidence":0.99}]}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-0000000000e1',
    'entity.parse.rfq_excel_column_map',
    'AI 映射 RFQ Excel 表头列',
    '根据 Excel 表头将列映射到 RFQ 明细字段（仅映列，不解析行数据）',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-0000000000e1',
    3600,
    jsonb_build_array(convert_from(decode('68656164657273', 'hex'), 'UTF8')),
    jsonb_build_array(
        convert_from(decode('68656164657273', 'hex'), 'UTF8'),
        convert_from(decode('7461726765745f6669656c6473', 'hex'), 'UTF8')
    ),
    512,
    0.10,
    'biz.ai.entity.parse.rfq',
    20,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

-- 与 entity.parse.rfq 使用相同 LLM 厂商/模型（若已配置 moonshot 等）
UPDATE public.ai_scenario dest
SET provider_code = src.provider_code,
    model = src.model,
    modify_time = (now() AT TIME ZONE 'utc')
FROM public.ai_scenario src
WHERE dest.code = 'entity.parse.rfq_excel_column_map'
  AND src.code = 'entity.parse.rfq'
  AND NOT dest.is_deleted
  AND NOT src.is_deleted;

-- 已有库：修正 JSON 结构示例，避免 LLM 输出 customer_mpn|null 等非法 field
UPDATE public.ai_prompt_template
SET json_schema_hint = '{"header_row_index":0,"columns":[{"col_index":0,"field":"customer_mpn","confidence":0.95},{"col_index":1,"field":"mpn","confidence":0.98},{"col_index":4,"field":"quantity","confidence":0.99}]}',
    system_prompt = system_prompt || E'\n\n【输出示例】仅输出如下结构，columns 须覆盖每个非空表头列索引（0-based）：{"header_row_index":0,"columns":[{"col_index":0,"field":"customer_mpn","confidence":0.95}]}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.rfq_excel_column_map' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.rfq_excel_column_map';

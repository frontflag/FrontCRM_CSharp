-- 增量：entity.parse.rfq_excel_brand_map（RFQ Excel 导入品牌 AI 映射，权限沿用 biz.ai.entity.parse.rfq）
-- DBeaver-safe：user_prompt 经 hex + CHR 拼接占位符

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-0000000000e2',
    'entity.parse.rfq_excel_brand_map',
    1,
    '你是 CRM RFQ Excel 导入品牌映射助手。根据用户提供的 Excel「供应品牌」原文列表，将每条原文映射到系统标准品牌名（standard_brand）。仅输出合法 JSON（禁止 markdown）。键名 snake_case。mappings 为数组；每项含 source_text（与输入完全一致）、standard_brand（官方标准品牌名，无法确定时省略该项）、confidence（0~1）。禁止编造不存在的品牌；不确定则不要输出该 source_text。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e5be85e698a0e5b084e59381e7898ce58e9fe69687e58897e8a1a8efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082',
            'hex'
        ),
        'UTF8'
    )
    || CHR(10) || '待映射品牌原文：' || CHR(123) || CHR(123) || 'source_texts' || CHR(125) || CHR(125),
    'json',
    '{"mappings":[{"source_text":"TI(德州仪器)","standard_brand":"Texas Instruments","confidence":0.92},{"source_text":"ST(意法半导体)","standard_brand":"STMicroelectronics","confidence":0.9}]}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-0000000000e2',
    'entity.parse.rfq_excel_brand_map',
    'AI 映射 RFQ Excel 导入品牌',
    '将 Excel 供应品牌原文映射到系统标准品牌名（仅补漏，不替代规则与学习映射）',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-0000000000e2',
    3600,
    jsonb_build_array(convert_from(decode('736f757263655f7465787473', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('736f757263655f7465787473', 'hex'), 'UTF8')),
    512,
    0.10,
    'biz.ai.entity.parse.rfq',
    20,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

UPDATE public.ai_scenario dest
SET provider_code = src.provider_code,
    model = src.model,
    modify_time = (now() AT TIME ZONE 'utc')
FROM public.ai_scenario src
WHERE dest.code = 'entity.parse.rfq_excel_brand_map'
  AND src.code = 'entity.parse.rfq'
  AND NOT dest.is_deleted
  AND NOT src.is_deleted;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.rfq_excel_brand_map';

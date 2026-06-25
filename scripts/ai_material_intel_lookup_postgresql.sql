-- 增量：material.intel.lookup 场景 + 权限（已有 AI 模块库执行）
-- DBeaver-safe：注释与字面量中勿写冒号+变量名或双花括号占位符；占位符经 hex 写入
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000002',
    'material.intel.lookup',
    1,
    '你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值（category、part_number_breakdown[].meaning、technical_features[]、application_areas[]、alternatives、pricing、industry_news、disclaimer 等）必须使用简体中文，禁止用英文句子描述（品牌/原厂/官方型号代号可保留英文）。即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存或新闻。spec_params 中必须包含 datasheet_url（原厂或授权渠道 DataSheet 规格书链接）与 image_url（物料产品图链接）；可经联网检索公开来源，须为可访问的 https 链接，无法确认时填 null，禁止编造链接。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。',
    convert_from(
        decode(
            'e69fa5e8afa2e794b5e5ad90e58583e599a8e4bbb6e59e8be58fb7efbc9a7b7b706e7d7de38082e8bf94e59b9e206272616e645f696e666fe38081737065635f706172616d73efbc88e590ab20706172745f6e756d6265725f627265616b646f776eefbc8c6d65616e696e6720e5bf85e9a1bbe794a8e7ae80e4bd93e4b8ade69687e8a7a3e9878ae59084e6aeb5e590abe4b989efbc9be9a1bbe5b0bde9878fe68f90e4be9b206461746173686565745f75726c20e4b88e20696d6167655f75726cefbc8ce697a0e6b395e7a1aee8aea4e5a1ab206e756c6cefbc89e380816170706c69636174696f6e5f6172656173efbc88e6af8fe9a1b9e7ae80e4bd93e4b8ade69687efbc8ce7a681e6ada2e88bb1e69687efbc89e38081616c7465726e617469766573e3808170726963696e67e38081696e6475737472795f6e657773e38081646973636c61696d6572efbc88e7ae80e4bd93e4b8ade69687efbc89e38082e5868de6aca1e5bcbae8b083efbc9a6d65616e696e67e380816170706c69636174696f6e5f6172656173e38081746563686e6963616c5f666561747572657320e7ad89e68f8fe8bfb0e5ad97e6aeb5e4b88de5be97e8be93e587bae88bb1e69687e38082',
            'hex'
        ),
        'UTF8'
    ),
    'json',
    '{"brand_info":{"brand":"string|null","manufacturer":"string|null","origin":"string|null","product_line":"string|null","product_category":"string|null","series":"string|null"},"spec_params":{"category":"string|null","part_number_breakdown":[{"segment":"string","meaning":"string"}],"technical_features":["string"],"electrical_params":{},"datasheet_url":"string|null","image_url":"string|null"},"application_areas":["string"],"alternatives":[{"part_number":"string","brand":"string|null","note":"string|null"}],"pricing":{"market_price":{"reference_price":"string|null","currency":"string|null","note":"string|null"},"market_conditions":{"availability":"string|null","trend":"string|null","note":"string|null"},"price_tiers":[{"quantity":"string","unit_price":"string","currency":"string|null"}],"distributors":[{"distributor":"string","price_range":"string|null","currency":"string|null","stock_status":"string|null","moq":"string|null","last_updated":"string|null"}]},"industry_news":[{"title":"string","url":"string|null","summary":"string|null"}],"disclaimer":"string"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000002',
    'material.intel.lookup',
    'AI 物料情报查询',
    'RFQ 首页按型号查询物料情报',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000002',
    7776000,
    jsonb_build_array(convert_from(decode('706e', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('706e', 'hex'), 'UTF8')),
    8192,
    1.00,
    'biz.ai.material_intel.lookup',
    10,
    true,
    true
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c2', 'biz.ai.material_intel.lookup', 'AI-物料情报查询', 'api', 'ai', 'material_intel', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.material_intel.lookup' AND p."Status" = 1
WHERE (
    r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
    OR EXISTS (
        SELECT 1 FROM sys_role_permission rp
        JOIN sys_permission pr ON pr."PermissionId" = rp."PermissionId"
        WHERE rp."RoleId" = r."RoleId"
          AND pr."PermissionCode" = 'rfq.read'
          AND pr."Status" = 1
    )
)
AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
);

-- 已有库：强化中文输出提示词，并清除旧英文缓存
UPDATE public.ai_prompt_template
SET system_prompt = '你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值（category、part_number_breakdown[].meaning、technical_features[]、application_areas[]、alternatives、pricing、industry_news、disclaimer 等）必须使用简体中文，禁止用英文句子描述（品牌/原厂/官方型号代号可保留英文）。即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存或新闻。spec_params 中必须包含 datasheet_url（原厂或授权渠道 DataSheet 规格书链接）与 image_url（物料产品图链接）；可经联网检索公开来源，须为可访问的 https 链接，无法确认时填 null，禁止编造链接。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。',
    user_prompt_template = convert_from(
        decode(
            'e69fa5e8afa2e794b5e5ad90e58583e599a8e4bbb6e59e8be58fb7efbc9a7b7b706e7d7de38082e8bf94e59b9e206272616e645f696e666fe38081737065635f706172616d73efbc88e590ab20706172745f6e756d6265725f627265616b646f776eefbc8c6d65616e696e6720e5bf85e9a1bbe794a8e7ae80e4bd93e4b8ade69687e8a7a3e9878ae59084e6aeb5e590abe4b989efbc9be9a1bbe5b0bde9878fe68f90e4be9b206461746173686565745f75726c20e4b88e20696d6167655f75726cefbc8ce697a0e6b395e7a1aee8aea4e5a1ab206e756c6cefbc89e380816170706c69636174696f6e5f6172656173efbc88e6af8fe9a1b9e7ae80e4bd93e4b8ade69687efbc8ce7a681e6ada2e88bb1e69687efbc89e38081616c7465726e617469766573e3808170726963696e67e38081696e6475737472795f6e657773e38081646973636c61696d6572efbc88e7ae80e4bd93e4b8ade69687efbc89e38082e5868de6aca1e5bcbae8b083efbc9a6d65616e696e67e380816170706c69636174696f6e5f6172656173e38081746563686e6963616c5f666561747572657320e7ad89e68f8fe8bfb0e5ad97e6aeb5e4b88de5be97e8be93e587bae88bb1e69687e38082',
            'hex'
        ),
        'UTF8'
    )
WHERE code = 'material.intel.lookup' AND version = 1;
DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';

-- 联网搜索（Moonshot $web_search）
ALTER TABLE public.ai_scenario
    ADD COLUMN IF NOT EXISTS enable_web_search boolean NOT NULL DEFAULT false;

UPDATE public.ai_scenario
SET enable_web_search = true
WHERE code = 'material.intel.lookup';

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';

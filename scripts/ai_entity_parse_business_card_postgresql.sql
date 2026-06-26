-- 增量：entity.parse.customer_business_card / entity.parse.vendor_business_card（AI 名片解析建单）
-- 依赖：ai_module、ai_entity_parse、ai_entity_parse_log
-- 注意：009/00a/0c9 已用于 vendor_address，本脚本使用 00b/00c/0cb/0cc

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-00000000000b',
    'entity.parse.customer_business_card',
    1,
    '你是 CRM 客户名片解析助手。从用户提供的名片图片中提取客户主档、联系人、地址信息，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造客户编码、系统 ID。用户可能提供 1 张合图，或分别提供正面、反面共 2 张图片；若为 2 张，第一张为正面、第二张为反面，请合并两面全部信息（反面常见 slogan、宣传语、地址、电话、邮箱、二维码说明等）。customer 对象为客户主档；contact 为名片上的联系人；address 为公司地址（无地址则 address 各字段均为 null 或省略 address 对象）。customer.customer_short_name 须自动生成简称：优先使用名片上的简称/品牌名；若无则从 customer_name 去掉「有限公司/股份有限公司/集团/公司」等后缀；仍无则填 customer_name。customer.industry 须从名片上的行业标签、业务关键词、产品方向等推断（如半导体、新能源、智能芯片、IoT 等，多个用顿号连接）；customer.company_info 须根据 slogan、宣传语、业务标签、产品/服务描述等生成 1～3 句公司简介式描述（将写入系统「公司介绍」字段），无相关内容则 null；勿将 slogan 摘要写入 customer.remarks。contact.gender 为整数 0=保密 1=男 2=女，名片无法判断时默认 1。customer_type 为整数（1 终端 2 贸易商 3 代理商）；customer_level 为 D/C/B/BPO/VIP/VPO；contact.c_name 中文名；contact.e_name 英文名；contact.is_default 默认 true；address.address_type 默认 Office；province/city/district 为中国省市区，须推断标准后缀。',
    '请解析附件名片图片（可为 1 张或正反面 2 张），提取客户主档、联系人与地址信息，严格按约定 JSON 输出。',
    'json',
    '{"customer":{"customer_name":"string|null","customer_short_name":"string|null","english_official_name":"string|null","customer_type":"number|null","customer_level":"string|null","industry":"string|null","country":"string|null","province":"string|null","city":"string|null","district":"string|null","address":"string|null","unified_social_credit_code":"string|null","credit_limit":"number|null","payment_terms":"number|null","currency":"number|null","tax_rate":"number|null","invoice_type":"number|null","company_info":"string|null","remarks":"string|null"},"contact":{"c_name":"string|null","e_name":"string|null","gender":"number|null","department":"string|null","position":"string|null","mobile_phone":"string|null","phone":"string|null","email":"string|null","fax":"string|null","social_account":"string|null","is_default":"boolean|null","is_decision_maker":"boolean|null","remarks":"string|null"},"address":{"address_type":"string|null","country":"string|null","province":"string|null","city":"string|null","district":"string|null","street_address":"string|null","company_name":"string|null","zip_code":"string|null","contact_person":"string|null","contact_phone":"string|null","is_default":"boolean|null"}}',
    true
)
ON CONFLICT (code, version) DO UPDATE SET
    system_prompt = EXCLUDED.system_prompt,
    user_prompt_template = EXCLUDED.user_prompt_template,
    output_format = EXCLUDED.output_format,
    json_schema_hint = EXCLUDED.json_schema_hint,
    is_active = EXCLUDED.is_active,
    modify_time = (now() AT TIME ZONE 'utc');

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-00000000000c',
    'entity.parse.vendor_business_card',
    1,
    '你是 CRM 供应商名片解析助手。从用户提供的名片图片中提取供应商主档、联系人、地址信息，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造编码、系统 ID。用户可能提供 1 张合图，或分别提供正面、反面共 2 张图片；若为 2 张，第一张为正面、第二张为反面，请合并两面全部信息。vendor.nick_name 须自动生成简称：优先使用名片简称/品牌名；若无则从 official_name 去掉「有限公司/股份有限公司/集团/公司」等后缀；仍无则填 official_name。vendor.industry 须从名片上的行业标签、业务关键词、产品方向等推断（如半导体、新能源、智能芯片、IoT 等，多个用顿号连接）；vendor.company_info 须根据 slogan、宣传语、业务标签、产品/服务描述等生成 1～3 句公司简介式描述（将写入系统「公司简介」字段），无相关内容则 null；勿将 slogan 摘要写入 vendor.remark。contact.gender 为整数 0=保密 1=男 2=女，名片无法判断时默认 1。vendor 对象为供应商主档；contact 为名片上的联系人；address 为公司地址（无地址则省略 address 或字段为 null）。vendor.level 1-13；vendor.credit 1-10；contact.c_name 中文名；contact.e_name 英文名；contact.is_main 默认 true；address.address_type 默认 1（办公地址）。',
    '请解析附件名片图片（可为 1 张或正反面 2 张），提取供应商主档、联系人与地址信息，严格按约定 JSON 输出。',
    'json',
    '{"vendor":{"official_name":"string|null","english_official_name":"string|null","nick_name":"string|null","industry":"string|null","level":"number|null","credit":"number|null","office_address":"string|null","website":"string|null","trade_currency":"number|null","payment_method":"string|null","payment_days":"number|null","credit_code":"string|null","company_info":"string|null","remark":"string|null"},"contact":{"c_name":"string|null","e_name":"string|null","gender":"number|null","title":"string|null","department":"string|null","mobile":"string|null","tel":"string|null","email":"string|null","is_main":"boolean|null","remark":"string|null"},"address":{"address_type":"number|null","country":"string|null","province":"string|null","city":"string|null","area":"string|null","address":"string|null","contact_name":"string|null","contact_phone":"string|null","is_default":"boolean|null","remark":"string|null"}}',
    true
)
ON CONFLICT (code, version) DO UPDATE SET
    system_prompt = EXCLUDED.system_prompt,
    user_prompt_template = EXCLUDED.user_prompt_template,
    output_format = EXCLUDED.output_format,
    json_schema_hint = EXCLUDED.json_schema_hint,
    is_active = EXCLUDED.is_active,
    modify_time = (now() AT TIME ZONE 'utc');

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-00000000000b',
    'entity.parse.customer_business_card',
    'AI 名片创建客户',
    '从名片图片（可双面）解析客户主档、联系人与地址',
    'mock',
    'mock',
    (SELECT id FROM public.ai_prompt_template WHERE code = 'entity.parse.customer_business_card' AND version = 1),
    0,
    jsonb_build_array(
        convert_from(decode('696d6167655f626173653634', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6261736536345f32', 'hex'), 'UTF8')
    ),
    jsonb_build_array(
        convert_from(decode('696d6167655f626173653634', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6d696d65', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6261736536345f32', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6d696d655f32', 'hex'), 'UTF8')
    ),
    4096,
    0.20,
    'biz.ai.entity.parse.customer_business_card',
    10,
    true,
    false
)
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    prompt_template_id = EXCLUDED.prompt_template_id,
    cache_ttl_seconds = EXCLUDED.cache_ttl_seconds,
    cache_key_fields = EXCLUDED.cache_key_fields,
    allowed_input_fields = EXCLUDED.allowed_input_fields,
    max_tokens = EXCLUDED.max_tokens,
    temperature = EXCLUDED.temperature,
    permission_code = EXCLUDED.permission_code,
    rate_limit_per_user_per_min = EXCLUDED.rate_limit_per_user_per_min,
    is_enabled = EXCLUDED.is_enabled,
    enable_web_search = EXCLUDED.enable_web_search,
    modify_time = (now() AT TIME ZONE 'utc');

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-00000000000c',
    'entity.parse.vendor_business_card',
    'AI 名片创建供应商',
    '从名片图片（可双面）解析供应商主档、联系人与地址',
    'mock',
    'mock',
    (SELECT id FROM public.ai_prompt_template WHERE code = 'entity.parse.vendor_business_card' AND version = 1),
    0,
    jsonb_build_array(
        convert_from(decode('696d6167655f626173653634', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6261736536345f32', 'hex'), 'UTF8')
    ),
    jsonb_build_array(
        convert_from(decode('696d6167655f626173653634', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6d696d65', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6261736536345f32', 'hex'), 'UTF8'),
        convert_from(decode('696d6167655f6d696d655f32', 'hex'), 'UTF8')
    ),
    4096,
    0.20,
    'biz.ai.entity.parse.vendor_business_card',
    10,
    true,
    false
)
ON CONFLICT (code) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    prompt_template_id = EXCLUDED.prompt_template_id,
    cache_ttl_seconds = EXCLUDED.cache_ttl_seconds,
    cache_key_fields = EXCLUDED.cache_key_fields,
    allowed_input_fields = EXCLUDED.allowed_input_fields,
    max_tokens = EXCLUDED.max_tokens,
    temperature = EXCLUDED.temperature,
    permission_code = EXCLUDED.permission_code,
    rate_limit_per_user_per_min = EXCLUDED.rate_limit_per_user_per_min,
    is_enabled = EXCLUDED.is_enabled,
    enable_web_search = EXCLUDED.enable_web_search,
    modify_time = (now() AT TIME ZONE 'utc');

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000cb', 'biz.ai.entity.parse.customer_business_card', 'AI-名片创建客户', 'api', 'ai', 'entity_parse_customer_business_card', 1, NOW()),
    ('30000000-0000-4000-8000-0000000000cc', 'biz.ai.entity.parse.vendor_business_card', 'AI-名片创建供应商', 'api', 'ai', 'entity_parse_vendor_business_card', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.customer_business_card' AND p."Status" = 1
WHERE (
    r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
    OR EXISTS (
        SELECT 1 FROM sys_role_permission rp
        JOIN sys_permission pr ON pr."PermissionId" = rp."PermissionId"
        WHERE rp."RoleId" = r."RoleId"
          AND pr."PermissionCode" = 'customer.write'
          AND pr."Status" = 1
    )
)
AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.vendor_business_card' AND p."Status" = 1
WHERE (
    r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
    OR EXISTS (
        SELECT 1 FROM sys_role_permission rp
        JOIN sys_permission pr ON pr."PermissionId" = rp."PermissionId"
        WHERE rp."RoleId" = r."RoleId"
          AND pr."PermissionCode" = 'vendor.write'
          AND pr."Status" = 1
    )
)
AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
);

-- 生产环境：切换为 Moonshot 视觉模型（按需执行）
-- UPDATE public.ai_scenario SET provider_code = 'moonshot', model = 'moonshot-v1-8k-vision-preview' WHERE code = 'entity.parse.customer_business_card';
-- UPDATE public.ai_scenario SET provider_code = 'moonshot', model = 'moonshot-v1-8k-vision-preview' WHERE code = 'entity.parse.vendor_business_card';

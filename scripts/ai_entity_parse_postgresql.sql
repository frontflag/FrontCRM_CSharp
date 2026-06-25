-- 增量：entity.parse.customer / entity.parse.rfq / entity.parse.vendor / entity.parse.customer_contact / entity.parse.vendor_contact / entity.parse.customer_address / entity.parse.vendor_address（AI 文本解析建单）
-- DBeaver-safe：注释与字面量中勿写双花括号占位符；user_prompt 经 hex + CHR 拼接 raw_text 占位符

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000003',
    'entity.parse.customer',
    1,
    '你是 CRM 客户主档解析助手。从用户提供的非结构化文本中提取客户主档字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造客户编码、系统 ID 或联系人信息。不要输出 contacts 或任何联系人数组。customer_type 为整数（1 终端 2 贸易商 3 代理商，未知 null）；customer_level 为 D/C/B/BPO/VIP/VPO 之一；currency 为 1=RMB 2=USD 3=EUR 4=HKD；invoice_type 为 0 无需开票 1 专票 2 普票。province、city、district 为中国省/市/区；若文本或公司名含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省，杭州→浙江省）；直辖市 province 与 city 均为直辖市全称（如北京市）；名称尽量带标准「省」「市」「区」后缀。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e5aea2e688b7e4b8bbe6a1a3e4bfa1e681afefbc88e4b88de590abe88194e7b3bbe4babaefbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"customer_name":"string|null","customer_short_name":"string|null","english_official_name":"string|null","customer_type":"number|null","customer_level":"string|null","industry":"string|null","country":"string|null","province":"string|null","city":"string|null","district":"string|null","address":"string|null","unified_social_credit_code":"string|null","credit_limit":"number|null","payment_terms":"number|null","currency":"number|null","tax_rate":"number|null","invoice_type":"number|null","remarks":"string|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000004',
    'entity.parse.rfq',
    1,
    '你是 CRM RFQ 需求解析助手。从文本中提取需求主档与物料明细 items 数组，仅输出合法 JSON（禁止 markdown）。键名 snake_case。未出现填 null，禁止编造 ID。customer_name 仅为公司名称文本。items 为物料明细数组，可含 1 条或多条；即使仅一条也必须输出 items 数组；每条明细对应独立型号及数量、目标价、备注等。每条 item.target_price 为目标价数值；item.price_currency 为目标价币别整数：1=RMB/CNY/人民币，2=USD/美元/$，3=EUR/欧元，4=HKD/港币/港元；须从目标价同行或邻近的币种符号、ISO 代码、中文币种名解析，例如 USD 0.15 或 $0.15 填 price_currency=2；未标注币种默认 1。item.remark 填该行物料备注（包装/交付/品质/环保等补充说明，保留原文）；顶层 remark 填整单通用备注；不得将型号、数量、价格误填入备注；禁止使用顶层单对象 item。rfq_type 1 询价 2 招标；target_type 1 现货 2 期货；quote_method 1 邮件 2 系统；assign_method 1 手动 2 自动；importance 1-3 表示重要程度星级。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f962052465120e99c80e6b782e4b8bbe6a1a3e4b88ee9a696e69da1e789a9e69699e6988ee7bb86efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"customer_name":"string|null","contact_email":"string|null","industry":"string|null","product":"string|null","rfq_type":"number|null","target_type":"number|null","quote_method":"number|null","assign_method":"number|null","importance":"number|null","project_background":"string|null","competitor":"string|null","remark":"string|null","items":[{"customer_mpn":"string|null","customer_brand":"string|null","mpn":"string|null","brand":"string|null","target_price":"number|null","price_currency":"number|null","quantity":"number|null","production_date":"string|null","expiry_date":"string|null","min_package_qty":"number|null","moq":"number|null","alternatives":"string|null","remark":"string|null"}]}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000003',
    'entity.parse.customer',
    'AI 解析文本创建客户',
    '从非结构化文本解析客户主档字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000003',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    4096,
    0.20,
    'biz.ai.entity.parse.customer',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000004',
    'entity.parse.rfq',
    'AI 解析文本创建需求',
    '从非结构化文本解析 RFQ 主档与物料明细（支持多行）',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000004',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    4096,
    0.20,
    'biz.ai.entity.parse.rfq',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c3', 'biz.ai.entity.parse.customer', 'AI-解析创建客户', 'api', 'ai', 'entity_parse_customer', 1, NOW()),
    ('30000000-0000-4000-8000-0000000000c4', 'biz.ai.entity.parse.rfq', 'AI-解析创建需求', 'api', 'ai', 'entity_parse_rfq', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

-- 拥有 customer.write 的角色授予客户 AI 解析
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.customer' AND p."Status" = 1
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

-- 拥有 rfq.create 的角色授予 RFQ AI 解析
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.rfq' AND p."Status" = 1
WHERE (
    r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
    OR EXISTS (
        SELECT 1 FROM sys_role_permission rp
        JOIN sys_permission pr ON pr."PermissionId" = rp."PermissionId"
        WHERE rp."RoleId" = r."RoleId"
          AND pr."PermissionCode" = 'rfq.create'
          AND pr."Status" = 1
    )
)
AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
);

-- 已有库：同步 user_prompt（CHR 拼接占位符，避免脚本内双花括号字面量）
UPDATE public.ai_prompt_template
SET user_prompt_template =
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e5aea2e688b7e4b8bbe6a1a3e4bfa1e681afefbc88e4b88de590abe88194e7b3bbe4babaefbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.customer' AND version = 1;

-- 已有库：强化客户解析省/市/区推断（含 province 反查）
UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 客户主档解析助手。从用户提供的非结构化文本中提取客户主档字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造客户编码、系统 ID 或联系人信息。不要输出 contacts 或任何联系人数组。customer_type 为整数（1 终端 2 贸易商 3 代理商，未知 null）；customer_level 为 D/C/B/BPO/VIP/VPO 之一；currency 为 1=RMB 2=USD 3=EUR 4=HKD；invoice_type 为 0 无需开票 1 专票 2 普票。province、city、district 为中国省/市/区；若文本或公司名含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省，杭州→浙江省）；直辖市 province 与 city 均为直辖市全称（如北京市）；名称尽量带标准「省」「市」「区」后缀。',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.customer' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.customer';

UPDATE public.ai_prompt_template
SET user_prompt_template =
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f962052465120e99c80e6b782e4b8bbe6a1a3e4b88ee9a696e69da1e789a9e69699e6988ee7bb86efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.rfq' AND version = 1;

-- 已有库：强化 RFQ 多行 items 解析说明
UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM RFQ 需求解析助手。从文本中提取需求主档与物料明细 items 数组，仅输出合法 JSON（禁止 markdown）。键名 snake_case。未出现填 null，禁止编造 ID。customer_name 仅为公司名称文本。items 为物料明细数组，可含 1 条或多条；即使仅一条也必须输出 items 数组；每条明细对应独立型号及数量、目标价、备注等。每条 item.target_price 为目标价数值；item.price_currency 为目标价币别整数：1=RMB/CNY/人民币，2=USD/美元/$，3=EUR/欧元，4=HKD/港币/港元；须从目标价同行或邻近的币种符号、ISO 代码、中文币种名解析，例如 USD 0.15 或 $0.15 填 price_currency=2；未标注币种默认 1。item.remark 填该行物料备注（包装/交付/品质/环保等补充说明，保留原文）；顶层 remark 填整单通用备注；不得将型号、数量、价格误填入备注；禁止使用顶层单对象 item。rfq_type 1 询价 2 招标；target_type 1 现货 2 期货；quote_method 1 邮件 2 系统；assign_method 1 手动 2 自动；importance 1-3 表示重要程度星级。',
    json_schema_hint = '{"customer_name":"string|null","contact_email":"string|null","industry":"string|null","product":"string|null","rfq_type":"number|null","target_type":"number|null","quote_method":"number|null","assign_method":"number|null","importance":"number|null","project_background":"string|null","competitor":"string|null","remark":"string|null","items":[{"customer_mpn":"string|null","customer_brand":"string|null","mpn":"string|null","brand":"string|null","target_price":"number|null","price_currency":"number|null","quantity":"number|null","production_date":"string|null","expiry_date":"string|null","min_package_qty":"number|null","moq":"number|null","alternatives":"string|null","remark":"string|null"}]}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.rfq' AND version = 1;

UPDATE public.ai_scenario
SET description = '从非结构化文本解析 RFQ 主档与物料明细（支持多行）'
WHERE code = 'entity.parse.rfq';

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.rfq';

-- entity.parse.vendor：AI 解析文本创建供应商
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000005',
    'entity.parse.vendor',
    1,
    '你是 CRM 供应商主档解析助手。从用户提供的非结构化文本中提取供应商主档字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造供应商编码、系统 ID 或联系人信息。不要输出 contacts 或任何联系人数组。official_name 为供应商全称。level 为整数 1-13；credit 为身份整数 1-10；trade_currency 为 1=RMB 2=USD 3=EUR 4=HKD；payment_method 为付款方式文本；payment_days 为账期天数整数。',
    convert_from(
        decode(
            'e8afb7e6a0b9e68daee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e4be9be5ba94e59586e4b8bbe6a1a3e4bfa1e681afefbc88e4b88de590abe88194e7b3bbe4babaefbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"official_name":"string|null","english_official_name":"string|null","nick_name":"string|null","industry":"string|null","level":"number|null","credit":"number|null","office_address":"string|null","website":"string|null","trade_currency":"number|null","payment_method":"string|null","payment_days":"number|null","credit_code":"string|null","company_info":"string|null","remark":"string|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000005',
    'entity.parse.vendor',
    'AI 解析文本创建供应商',
    '从非结构化文本解析供应商主档字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000005',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    4096,
    0.20,
    'biz.ai.entity.parse.vendor',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c5', 'biz.ai.entity.parse.vendor', 'AI-解析创建供应商', 'api', 'ai', 'entity_parse_vendor', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.vendor' AND p."Status" = 1
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

-- 已有库：同步 vendor 解析 prompt / 权限
UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 供应商主档解析助手。从用户提供的非结构化文本中提取供应商主档字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造供应商编码、系统 ID 或联系人信息。不要输出 contacts 或任何联系人数组。official_name 为供应商全称。level 为整数 1-13；credit 为身份整数 1-10；trade_currency 为 1=RMB 2=USD 3=EUR 4=HKD；payment_method 为付款方式文本；payment_days 为账期天数整数。',
    user_prompt_template =
        convert_from(
            decode(
                'e8afb7e6a0b9e68daee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e4be9be5ba94e59586e4b8bbe6a1a3e4bfa1e681afefbc88e4b88de590abe88194e7b3bbe4babaefbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
                'hex'
            ),
            'UTF8'
        )
        || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    json_schema_hint = '{"official_name":"string|null","english_official_name":"string|null","nick_name":"string|null","industry":"string|null","level":"number|null","credit":"number|null","office_address":"string|null","website":"string|null","trade_currency":"number|null","payment_method":"string|null","payment_days":"number|null","credit_code":"string|null","company_info":"string|null","remark":"string|null"}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.vendor' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.vendor';

-- entity.parse.customer_contact：AI 解析文本创建客户联系人
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000006',
    'entity.parse.customer_contact',
    1,
    '你是 CRM 客户联系人解析助手。从用户提供的非结构化文本（名片、邮件签名、聊天记录等）中提取单个联系人字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造联系人 ID 或客户主档信息。不要输出客户公司名称、地址等主档字段。contact_name 为联系人姓名。gender 为整数 0=保密/未知 1=男 2=女；is_default 为是否默认联系人布尔；is_decision_maker 为是否决策人布尔；social_account 为 QQ/微信等社交账号。',
    convert_from(
        decode(
            'e8afb7e6a0b9e68daee99d9ee7bb93e69e84e58c96e69687e69cace68f90e58f96e5aea2e688b7e88194e7b3bbe4babae4bfa1e681afefbc88e4b88de590abe5aea2e688b7e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"contact_name":"string|null","gender":"number|null","department":"string|null","position":"string|null","mobile_phone":"string|null","phone":"string|null","email":"string|null","fax":"string|null","social_account":"string|null","is_default":"boolean|null","is_decision_maker":"boolean|null","remarks":"string|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000006',
    'entity.parse.customer_contact',
    'AI 解析文本创建客户联系人',
    '从非结构化文本解析客户联系人字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000006',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    2048,
    0.20,
    'biz.ai.entity.parse.customer_contact',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c6', 'biz.ai.entity.parse.customer_contact', 'AI-解析创建客户联系人', 'api', 'ai', 'entity_parse_customer_contact', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.customer_contact' AND p."Status" = 1
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

UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 客户联系人解析助手。从用户提供的非结构化文本（名片、邮件签名、聊天记录等）中提取单个联系人字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造联系人 ID 或客户主档信息。不要输出客户公司名称、地址等主档字段。contact_name 为联系人姓名。gender 为整数 0=保密/未知 1=男 2=女；is_default 为是否默认联系人布尔；is_decision_maker 为是否决策人布尔；social_account 为 QQ/微信等社交账号。',
    user_prompt_template =
        convert_from(
            decode(
                'e8afb7e6a0b9e68daee99d9ee7bb93e69e84e58c96e69687e69cace68f90e58f96e5aea2e688b7e88194e7b3bbe4babae4bfa1e681afefbc88e4b88de590abe5aea2e688b7e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
                'hex'
            ),
            'UTF8'
        )
        || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    json_schema_hint = '{"contact_name":"string|null","gender":"number|null","department":"string|null","position":"string|null","mobile_phone":"string|null","phone":"string|null","email":"string|null","fax":"string|null","social_account":"string|null","is_default":"boolean|null","is_decision_maker":"boolean|null","remarks":"string|null"}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.customer_contact' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.customer_contact';

-- entity.parse.vendor_contact：AI 解析文本创建供应商联系人
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000007',
    'entity.parse.vendor_contact',
    1,
    '你是 CRM 供应商联系人解析助手。从用户提供的非结构化文本（名片、邮件签名、聊天记录等）中提取单个联系人字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造联系人 ID 或供应商主档信息。不要输出供应商公司名称、地址等主档字段。c_name 为中文姓名；e_name 为英文姓名；title 为职位；mobile 为手机；tel 为座机；is_main 为是否主联系人布尔。',
    convert_from(
        decode(
            'e8afb7e6a0b9e68daee99d9ee7bb93e69e84e58c96e69687e69cace68f90e58f96e4be9be5ba94e59586e88194e7b3bbe4babae4bfa1e681afefbc88e4b88de590abe4be9be5ba94e59586e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"c_name":"string|null","e_name":"string|null","title":"string|null","department":"string|null","mobile":"string|null","tel":"string|null","email":"string|null","is_main":"boolean|null","remark":"string|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000007',
    'entity.parse.vendor_contact',
    'AI 解析文本创建供应商联系人',
    '从非结构化文本解析供应商联系人字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000007',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    2048,
    0.20,
    'biz.ai.entity.parse.vendor_contact',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c7', 'biz.ai.entity.parse.vendor_contact', 'AI-解析创建供应商联系人', 'api', 'ai', 'entity_parse_vendor_contact', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.vendor_contact' AND p."Status" = 1
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

UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 供应商联系人解析助手。从用户提供的非结构化文本（名片、邮件签名、聊天记录等）中提取单个联系人字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造联系人 ID 或供应商主档信息。不要输出供应商公司名称、地址等主档字段。c_name 为中文姓名；e_name 为英文姓名；title 为职位；mobile 为手机；tel 为座机；is_main 为是否主联系人布尔。',
    user_prompt_template =
        convert_from(
            decode(
                'e8afb7e6a0b9e68daee99d9ee7bb93e69e84e58c96e69687e69cace68f90e58f96e4be9be5ba94e59586e88194e7b3bbe4babae4bfa1e681afefbc88e4b88de590abe4be9be5ba94e59586e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
                'hex'
            ),
            'UTF8'
        )
        || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    json_schema_hint = '{"c_name":"string|null","e_name":"string|null","title":"string|null","department":"string|null","mobile":"string|null","tel":"string|null","email":"string|null","is_main":"boolean|null","remark":"string|null"}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.vendor_contact' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.vendor_contact';

-- entity.parse.customer_address：AI 解析文本创建客户地址（单条）
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000008',
    'entity.parse.customer_address',
    1,
    '你是 CRM 客户地址解析助手。从用户提供的非结构化文本（送货单、收货地址、名片地址、邮件签名等）中提取单条地址字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造地址 ID 或客户主档信息。不要输出客户主档除地址相关外的其他字段。address_type 为 Office/Billing/Shipping/Registered 之一（办公/账单/收货/注册，未知默认 Office）。country 为国家中文名；港澳台一律 country=中国，province 填香港特别行政区/澳门特别行政区/台湾省（或与文本一致的省级名称）。中国大陆 province/city/district 为中国省/市/区；若文本含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省/深圳市/福田区）；直辖市 province 与 city 均为直辖市全称。海外地址 province 填州/省，zip_code 填邮编。street_address 为街道门牌等详细地址。company_name 为地址上的公司名称（若有）。contact_person、contact_phone 为地址联系人及电话。is_default 为是否默认地址布尔。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e58d95e69da1e5aea2e688b7e59cb0e59d80e4bfa1e681afefbc88e4b88de58c85e590abe5aea2e688b7e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"address_type":"string|null","country":"string|null","province":"string|null","city":"string|null","district":"string|null","street_address":"string|null","company_name":"string|null","zip_code":"string|null","contact_person":"string|null","contact_phone":"string|null","is_default":"boolean|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000008',
    'entity.parse.customer_address',
    'AI 解析文本创建客户地址',
    '从非结构化文本解析单条客户地址字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000008',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    2048,
    0.20,
    'biz.ai.entity.parse.customer_address',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c8', 'biz.ai.entity.parse.customer_address', 'AI-解析创建客户地址', 'api', 'ai', 'entity_parse_customer_address', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.customer_address' AND p."Status" = 1
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

UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 客户地址解析助手。从用户提供的非结构化文本（送货单、收货地址、名片地址、邮件签名等）中提取单条地址字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造地址 ID 或客户主档信息。不要输出客户主档除地址相关外的其他字段。address_type 为 Office/Billing/Shipping/Registered 之一（办公/账单/收货/注册，未知默认 Office）。country 为国家中文名；港澳台一律 country=中国，province 填香港特别行政区/澳门特别行政区/台湾省（或与文本一致的省级名称）。中国大陆 province/city/district 为中国省/市/区；若文本含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省/深圳市/福田区）；直辖市 province 与 city 均为直辖市全称。海外地址 province 填州/省，zip_code 填邮编。street_address 为街道门牌等详细地址。company_name 为地址上的公司名称（若有）。contact_person、contact_phone 为地址联系人及电话。is_default 为是否默认地址布尔。',
    user_prompt_template =
        convert_from(
            decode(
                'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e58d95e69da1e5aea2e688b7e59cb0e59d80e4bfa1e681afefbc88e4b88de58c85e590abe5aea2e688b7e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
                'hex'
            ),
            'UTF8'
        )
        || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    json_schema_hint = '{"address_type":"string|null","country":"string|null","province":"string|null","city":"string|null","district":"string|null","street_address":"string|null","company_name":"string|null","zip_code":"string|null","contact_person":"string|null","contact_phone":"string|null","is_default":"boolean|null"}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.customer_address' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.customer_address';

-- entity.parse.vendor_address：AI 解析文本创建供应商地址（单条）
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000009',
    'entity.parse.vendor_address',
    1,
    '你是 CRM 供应商地址解析助手。从用户提供的非结构化文本（送货单、收货地址、名片地址、邮件签名等）中提取单条地址字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造地址 ID 或供应商主档信息。不要输出供应商主档除地址相关外的其他字段。address_type 为整数：1=收货地址 2=账单地址（未知默认 1）；也接受 Shipping/Billing 或中文「收货」「账单」。country 为国家中文名；港澳台一律 country=中国，province 填香港特别行政区/澳门特别行政区/台湾省（或与文本一致的省级名称）。中国大陆 province/city/area 为中国省/市/区；若文本含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省/深圳市/福田区）；直辖市 province 与 city 均为直辖市全称。海外地址 province 填州/省。address 或 street_address 为街道门牌等详细地址。contact_name、contact_phone 为地址联系人及电话。is_default 为是否默认地址布尔。remark 为备注。',
    convert_from(
        decode(
            'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e58d95e69da1e4be9be5ba94e59586e59cb0e59d80e4bfa1e681afefbc88e4b88de58c85e590abe4be9be5ba94e59586e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
            'hex'
        ),
        'UTF8'
    )
    || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    'json',
    '{"address_type":"number|string|null","country":"string|null","province":"string|null","city":"string|null","area":"string|null","street_address":"string|null","address":"string|null","contact_name":"string|null","contact_phone":"string|null","is_default":"boolean|null","remark":"string|null"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
VALUES (
    'a3000001-0000-4000-8000-000000000009',
    'entity.parse.vendor_address',
    'AI 解析文本创建供应商地址',
    '从非结构化文本解析单条供应商地址字段',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000009',
    0,
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    jsonb_build_array(convert_from(decode('7261775f74657874', 'hex'), 'UTF8')),
    2048,
    0.20,
    'biz.ai.entity.parse.vendor_address',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c9', 'biz.ai.entity.parse.vendor_address', 'AI-解析创建供应商地址', 'api', 'ai', 'entity_parse_vendor_address', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.entity.parse.vendor_address' AND p."Status" = 1
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

UPDATE public.ai_prompt_template
SET system_prompt = '你是 CRM 供应商地址解析助手。从用户提供的非结构化文本（送货单、收货地址、名片地址、邮件签名等）中提取单条地址字段，仅输出合法 JSON（禁止 markdown 代码块）。JSON 键名必须 snake_case。文中未出现的字段填 null，禁止编造地址 ID 或供应商主档信息。不要输出供应商主档除地址相关外的其他字段。address_type 为整数：1=收货地址 2=账单地址（未知默认 1）；也接受 Shipping/Billing 或中文「收货」「账单」。country 为国家中文名；港澳台一律 country=中国，province 填香港特别行政区/澳门特别行政区/台湾省（或与文本一致的省级名称）。中国大陆 province/city/area 为中国省/市/区；若文本含城市/区名但未写省，须根据中国行政区划常识推断 province（如深圳/福田→广东省/深圳市/福田区）；直辖市 province 与 city 均为直辖市全称。海外地址 province 填州/省。address 或 street_address 为街道门牌等详细地址。contact_name、contact_phone 为地址联系人及电话。is_default 为是否默认地址布尔。remark 为备注。',
    user_prompt_template =
        convert_from(
            decode(
                'e8afb7e4bb8ee4bba5e4b88be99d9ee7bb93e69e84e58c96e69687e69cace4b8ade68f90e58f96e58d95e69da1e4be9be5ba94e59586e59cb0e59d80e4bfa1e681afefbc88e4b88de58c85e590abe4be9be5ba94e59586e4b8bbe6a1a3efbc89efbc8ce4b8a5e6a0bce68c89e7baa6e5ae9a204a534f4e20e8bf94e59b9eefbc8ce7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97e38082e58e9fe69687efbc9a',
                'hex'
            ),
            'UTF8'
        )
        || CHR(123) || CHR(123) || 'raw_text' || CHR(125) || CHR(125),
    json_schema_hint = '{"address_type":"number|string|null","country":"string|null","province":"string|null","city":"string|null","area":"string|null","street_address":"string|null","address":"string|null","contact_name":"string|null","contact_phone":"string|null","is_default":"boolean|null","remark":"string|null"}',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'entity.parse.vendor_address' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'entity.parse.vendor_address';

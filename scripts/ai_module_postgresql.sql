-- AI module PostgreSQL bootstrap
-- DBeaver-safe：见 document/PRD/规范/业务规范/PostgreSQL增量脚本编写规范.md
-- 脚本内勿出现双花括号占位符字面量（含注释）

-- ========== tables ==========

CREATE TABLE IF NOT EXISTS public.ai_provider (
    id                  character varying(36)  NOT NULL,
    code                character varying(64)  NOT NULL,
    name                character varying(100) NOT NULL,
    base_url            character varying(500) NOT NULL DEFAULT '',
    api_key_env         character varying(128) NULL,
    default_model       character varying(100) NOT NULL DEFAULT '',
    timeout_seconds     integer                NOT NULL DEFAULT 120,
    is_enabled          boolean                NOT NULL DEFAULT true,
    extra_headers       jsonb                  NULL,
    create_time         timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time         timestamp with time zone NULL,
    is_deleted          boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_ai_provider" PRIMARY KEY (id),
    CONSTRAINT "UX_ai_provider_code" UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS public.ai_prompt_template (
    id                      character varying(36)  NOT NULL,
    code                    character varying(100) NOT NULL,
    version                 integer                NOT NULL DEFAULT 1,
    system_prompt           text                   NOT NULL DEFAULT '',
    user_prompt_template    text                   NOT NULL DEFAULT '',
    output_format           character varying(20)  NOT NULL DEFAULT 'json',
    json_schema_hint        text                   NULL,
    is_active               boolean                NOT NULL DEFAULT true,
    create_time             timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time             timestamp with time zone NULL,
    is_deleted              boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_ai_prompt_template" PRIMARY KEY (id),
    CONSTRAINT "UX_ai_prompt_template_code_ver" UNIQUE (code, version)
);

CREATE TABLE IF NOT EXISTS public.ai_scenario (
    id                          character varying(36)  NOT NULL,
    code                        character varying(100) NOT NULL,
    name                        character varying(200) NOT NULL,
    description                 text                   NULL,
    provider_code               character varying(64)  NOT NULL,
    model                       character varying(100) NOT NULL,
    prompt_template_id          character varying(36)  NOT NULL,
    cache_ttl_seconds           integer                NOT NULL DEFAULT 0,
    cache_key_fields            jsonb                  NOT NULL DEFAULT '[]'::jsonb,
    allowed_input_fields        jsonb                  NOT NULL DEFAULT '[]'::jsonb,
    max_tokens                  integer                NOT NULL DEFAULT 2048,
    temperature                 numeric(4,2)           NOT NULL DEFAULT 0.30,
    permission_code             character varying(100) NOT NULL DEFAULT '',
    rate_limit_per_user_per_min integer                NOT NULL DEFAULT 10,
    is_enabled                  boolean                NOT NULL DEFAULT true,
    enable_web_search           boolean                NOT NULL DEFAULT false,
    create_time                 timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time                 timestamp with time zone NULL,
    is_deleted                  boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_ai_scenario" PRIMARY KEY (id),
    CONSTRAINT "UX_ai_scenario_code" UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS public.ai_global_config (
    config_key      character varying(64)  NOT NULL,
    config_value    character varying(500) NOT NULL DEFAULT '',
    description     character varying(500) NULL,
    modify_time     timestamp with time zone NULL,
    CONSTRAINT "PK_ai_global_config" PRIMARY KEY (config_key)
);

CREATE TABLE IF NOT EXISTS public.ai_invocation_cache (
    id                  character varying(36)  NOT NULL,
    cache_key           character varying(64)  NOT NULL,
    scenario_code       character varying(100) NOT NULL,
    request_fingerprint jsonb                  NOT NULL,
    response_content    text                   NOT NULL,
    response_json       jsonb                  NULL,
    provider_code       character varying(64)  NOT NULL,
    model               character varying(100) NOT NULL,
    template_version    integer                NOT NULL DEFAULT 1,
    hit_count           integer                NOT NULL DEFAULT 0,
    created_at          timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    expires_at          timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ai_invocation_cache" PRIMARY KEY (id),
    CONSTRAINT "UX_ai_invocation_cache_key" UNIQUE (cache_key)
);

CREATE INDEX IF NOT EXISTS "IX_ai_invocation_cache_expires"
    ON public.ai_invocation_cache (expires_at);

CREATE TABLE IF NOT EXISTS public.ai_invocation_log (
    id                  character varying(36)  NOT NULL,
    scenario_code       character varying(100) NOT NULL,
    provider_code       character varying(64)  NOT NULL,
    model               character varying(100) NOT NULL,
    template_version    integer                NOT NULL DEFAULT 1,
    user_id             character varying(36)  NULL,
    biz_type            character varying(64)  NULL,
    biz_id              character varying(64)  NULL,
    request_fingerprint jsonb                  NOT NULL,
    prompt_hash         character varying(64)  NOT NULL DEFAULT '',
    prompt_preview      character varying(200) NULL,
    status              character varying(20)  NOT NULL,
    from_cache          boolean                NOT NULL DEFAULT false,
    latency_ms          integer                NOT NULL DEFAULT 0,
    error_message       character varying(1000) NULL,
    prompt_tokens       integer                NULL,
    completion_tokens   integer                NULL,
    total_tokens        integer                NULL,
    created_at          timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT "PK_ai_invocation_log" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS "IX_ai_invocation_log_scenario_created"
    ON public.ai_invocation_log (scenario_code, created_at DESC);

CREATE INDEX IF NOT EXISTS "IX_ai_invocation_log_user_created"
    ON public.ai_invocation_log (user_id, created_at DESC);

-- ========== seed data ==========

INSERT INTO public.ai_global_config (config_key, config_value, description)
VALUES
    ('daily_quota_limit', '5000', '全站 AI 调用日配额（不含纯缓存命中）'),
    ('prompt_preview_enabled', 'true', '是否在日志中记录 prompt 前 200 字预览'),
    ('prompt_preview_max_chars', '200', 'prompt 预览最大字符数')
ON CONFLICT (config_key) DO NOTHING;

INSERT INTO public.ai_provider (id, code, name, base_url, api_key_env, default_model, timeout_seconds, is_enabled)
VALUES
    ('a1000001-0000-4000-8000-000000000001', 'mock', 'Mock（开发/测试）', '', NULL, 'mock', 30, true),
    ('a1000001-0000-4000-8000-000000000002', 'moonshot', 'Kimi (Moonshot)', 'https://api.moonshot.ai/v1', 'AI_MOONSHOT_API_KEY', 'kimi-k2.5', 120, true)
ON CONFLICT (code) DO NOTHING;

-- user_prompt_template via hex (DBeaver-safe)
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000001',
    'material.spec.lookup',
    1,
    '你是电子元器件规格助手。根据用户提供的 PN 与品牌，整理公开可查的规格参数。只输出 JSON，不要 markdown 代码块。无法确认的字段填 null，不要编造。输出字段：package, voltage, temperature_range, description, confidence(low|medium|high), disclaimer。',
    convert_from(
        decode(
            'e8afb7e69fa5e8afa2e789a9e69699e8a784e6a0bcefbc9a504e3d7b7b706e7d7defbc8ce59381e7898c3d7b7b6272616e647d7de38082',
            'hex'
        ),
        'UTF8'
    ),
    'json',
    convert_from(
        decode(
            '7b227061636b616765223a22737472696e677c6e756c6c222c' ||
            '22766f6c74616765223a22737472696e677c6e756c6c222c' ||
            '2274656d70657261747572655f72616e6765223a22737472696e677c6e756c6c222c' ||
            '226465736372697074696f6e223a22737472696e677c6e756c6c222c' ||
            '22636f6e666964656e6365223a22737472696e67222c' ||
            '22646973636c61696d6572223a22737472696e67227d',
            'hex'
        ),
        'UTF8'
    ),
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled
)
VALUES (
    'a3000001-0000-4000-8000-000000000001',
    'material.spec.lookup',
    'AI 查询物料规格',
    '根据 PN+品牌查询规格参数（Debug/业务入口）',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000001',
    604800,
    jsonb_build_array(
        convert_from(decode('706e', 'hex'), 'UTF8'),
        convert_from(decode('6272616e64', 'hex'), 'UTF8')
    ),
    jsonb_build_array(
        convert_from(decode('706e', 'hex'), 'UTF8'),
        convert_from(decode('6272616e64', 'hex'), 'UTF8')
    ),
    2048,
    1.00,
    'biz.ai.material_spec.lookup',
    10,
    true
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000c0', 'biz.ai.admin', 'AI-管理配置', 'api', 'ai', 'admin', 1, NOW()),
    ('30000000-0000-4000-8000-0000000000c1', 'biz.ai.material_spec.lookup', 'AI-查询物料规格', 'api', 'ai', 'material_spec', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('biz.ai.admin', 'biz.ai.material_spec.lookup')
  AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- ========== material.intel.lookup (RFQ AI 物料情报) ==========

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
    '{"brand_info":{"brand":"string|null","manufacturer":"string|null","origin":"string|null"},"spec_params":{"category":"string|null","part_number_breakdown":[{"segment":"string","meaning":"string"}],"technical_features":["string"],"electrical_params":{},"datasheet_url":"string|null","image_url":"string|null"},"application_areas":["string"],"alternatives":["string"],"pricing":{"market_price":{},"market_conditions":{}},"industry_news":["string"],"disclaimer":"string"}',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search
)
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

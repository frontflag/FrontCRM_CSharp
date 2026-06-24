using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>AI 通用模块：厂商、场景、模板、缓存、调用日志。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260803180000_AiModuleSchema")]
    public partial class AiModuleSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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

                INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
                VALUES (
                    'a2000001-0000-4000-8000-000000000001',
                    'material.spec.lookup',
                    1,
                    '你是电子元器件规格助手。根据用户提供的 PN 与品牌，整理公开可查的规格参数。只输出 JSON，不要 markdown 代码块。无法确认的字段填 null，不要编造。输出字段：package, voltage, temperature_range, description, confidence(low|medium|high), disclaimer。',
                    $$请查询物料规格：PN={{pn}}，品牌={{brand}}。$$,
                    'json',
                    $$ {"package":"string|null","voltage":"string|null","temperature_range":"string|null","description":"string|null","confidence":"string","disclaimer":"string"} $$,
                    true
                )
                ON CONFLICT (code, version) DO NOTHING;

                INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
                    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
                    permission_code, rate_limit_per_user_per_min, is_enabled)
                VALUES (
                    'a3000001-0000-4000-8000-000000000001',
                    'material.spec.lookup',
                    'AI 查询物料规格',
                    '根据 PN+品牌查询规格参数（Debug/业务入口）',
                    'mock',
                    'mock',
                    'a2000001-0000-4000-8000-000000000001',
                    604800,
                    jsonb_build_array('pn', 'brand'),
                    jsonb_build_array('pn', 'brand'),
                    2048,
                    1.00,
                    'biz.ai.material_spec.lookup',
                    10,
                    true
                )
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO sys_permission (""PermissionId"", ""PermissionCode"", ""PermissionName"", ""PermissionType"", ""Resource"", ""Action"", ""Status"", ""CreateTime"")
                VALUES
                    ('30000000-0000-4000-8000-0000000000c0', 'biz.ai.admin', 'AI-管理配置', 'api', 'ai', 'admin', 1, NOW()),
                    ('30000000-0000-4000-8000-0000000000c1', 'biz.ai.material_spec.lookup', 'AI-查询物料规格', 'api', 'ai', 'material_spec', 1, NOW())
                ON CONFLICT (""PermissionCode"") DO NOTHING;

                INSERT INTO sys_role_permission (""RolePermissionId"", ""RoleId"", ""PermissionId"", ""CreateTime"")
                SELECT gen_random_uuid()::text, r.""RoleId"", p.""PermissionId"", NOW()
                FROM sys_role r
                JOIN sys_permission p ON p.""PermissionCode"" IN ('biz.ai.admin', 'biz.ai.material_spec.lookup')
                  AND p.""Status"" = 1
                WHERE r.""RoleCode"" IN ('SYS_ADMIN', 'biz_all')
                  AND NOT EXISTS (
                    SELECT 1 FROM sys_role_permission x
                    WHERE x.""RoleId"" = r.""RoleId"" AND x.""PermissionId"" = p.""PermissionId""
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM sys_role_permission rp
                USING sys_permission p
                WHERE rp.""PermissionId"" = p.""PermissionId""
                  AND p.""PermissionCode"" IN ('biz.ai.admin', 'biz.ai.material_spec.lookup');

                DELETE FROM sys_permission
                WHERE ""PermissionCode"" IN ('biz.ai.admin', 'biz.ai.material_spec.lookup');

                DROP TABLE IF EXISTS public.ai_invocation_log;
                DROP TABLE IF EXISTS public.ai_invocation_cache;
                DROP TABLE IF EXISTS public.ai_global_config;
                DROP TABLE IF EXISTS public.ai_scenario;
                DROP TABLE IF EXISTS public.ai_prompt_template;
                DROP TABLE IF EXISTS public.ai_provider;
                """);
        }
    }
}

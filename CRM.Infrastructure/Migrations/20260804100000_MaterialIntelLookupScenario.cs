using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804100000_MaterialIntelLookupScenario")]
public partial class MaterialIntelLookupScenario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
            VALUES (
                'a2000001-0000-4000-8000-000000000002',
                'material.intel.lookup',
                1,
                $$You are an electronic component intelligence assistant for Chinese-speaking procurement and sales users. Given a part number, return ONLY valid JSON (no markdown). JSON keys MUST remain English snake_case exactly as specified. All human-readable string VALUES (category, segment meanings, technical_features, application_areas, alternatives, pricing descriptions, industry_news, disclaimer, etc.) MUST be in Simplified Chinese (简体中文). Brand names, manufacturer names, and official part numbers may remain in original language. Use null for unknown fields; use empty arrays when none apply. Do NOT fabricate prices, stock, URLs, or news. The disclaimer must be in Chinese and state that results are for reference only and should be verified with the manufacturer or authorized distributor.$$
                ,
                $$Look up electronic component part number: {{pn}}. Return brand_info, spec_params (with part_number_breakdown as array of {segment, meaning} in part-number order; write meaning in Simplified Chinese), application_areas, alternatives, pricing, industry_news, disclaimer (Simplified Chinese).$$
                ,
                'json',
                $$ {"brand_info":{"brand":"string|null","manufacturer":"string|null","origin":"string|null"},"spec_params":{"category":"string|null","part_number_breakdown":[{"segment":"string","meaning":"string"}],"technical_features":["string"],"electrical_params":{},"datasheet_url":"string|null","image_url":"string|null"},"application_areas":["string"],"alternatives":["string"],"pricing":{"market_price":{},"market_conditions":{}},"industry_news":["string"],"disclaimer":"string"} $$,
                true
            )
            ON CONFLICT (code, version) DO NOTHING;

            INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
                cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
                permission_code, rate_limit_per_user_per_min, is_enabled)
            VALUES (
                'a3000001-0000-4000-8000-000000000002',
                'material.intel.lookup',
                'AI 物料情报查询',
                'RFQ 首页按 PN 查询物料情报（品牌/规格/应用/替代/价格/新闻）',
                'mock',
                'mock',
                'a2000001-0000-4000-8000-000000000002',
                7776000,
                jsonb_build_array('pn'),
                jsonb_build_array('pn'),
                8192,
                1.00,
                'biz.ai.material_intel.lookup',
                10,
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
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM sys_role_permission rp
            USING sys_permission p
            WHERE rp."PermissionId" = p."PermissionId" AND p."PermissionCode" = 'biz.ai.material_intel.lookup';

            DELETE FROM sys_permission WHERE "PermissionCode" = 'biz.ai.material_intel.lookup';

            DELETE FROM public.ai_scenario WHERE code = 'material.intel.lookup';
            DELETE FROM public.ai_prompt_template WHERE code = 'material.intel.lookup' AND version = 1;
            """);
    }
}

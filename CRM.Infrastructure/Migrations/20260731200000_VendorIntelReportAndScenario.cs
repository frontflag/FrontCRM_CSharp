using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731200000_VendorIntelReportAndScenario")]
public partial class VendorIntelReportAndScenario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.vendor_intel_report (
                id varchar(36) NOT NULL PRIMARY KEY,
                vendor_id varchar(36) NULL,
                company_name varchar(256) NOT NULL,
                credit_code varchar(64) NULL,
                query_fingerprint varchar(64) NOT NULL,
                scenario_code varchar(32) NOT NULL,
                report_json jsonb NOT NULL DEFAULT '{}'::jsonb,
                schema_version varchar(16) NOT NULL DEFAULT '1.0',
                source varchar(16) NOT NULL DEFAULT 'live',
                invocation_log_id varchar(36) NULL,
                is_latest boolean NOT NULL DEFAULT true,
                created_by varchar(36) NULL,
                created_at timestamptz NOT NULL DEFAULT timezone('utc', now())
            );
            CREATE INDEX IF NOT EXISTS ix_vendor_intel_report_vendor_created
                ON public.vendor_intel_report (vendor_id, created_at DESC);
            CREATE INDEX IF NOT EXISTS ix_vendor_intel_report_fingerprint_latest
                ON public.vendor_intel_report (query_fingerprint, is_latest);

            INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
            VALUES (
                'a2000001-0000-4000-8000-00000000000d',
                'vendor.intel.lookup',
                1,
                $$你是面向中国采购与供应链人员的供应商情报调查助手。根据企业名称等信息，仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case。所有描述性字符串值必须使用简体中文。未知用 null，无数据用空数组；禁止编造司法风险数量、行政处罚、联系方式、股东持股比例。必须输出 meta（schema_version 填 1.1）、query、sections（固定 13 章）、relations、disclaimer。$$
                || $$ sections 必须包含且 id 固定为：registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment。每章含 id、title、summary、confidence（high|medium|low）、content、sources。ai_assessment 须从采购与供应链视角给出合作建议。relations.section_order 为上述 13 个 id 顺序数组。$$,
                $$请调查以下供应商的公开信息：企业名称 {{company_name}}；统一社会信用代码 {{credit_code}}；地区 {{region}}。返回 13 章供应商情报 JSON（registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment）。$$,
                'json',
                $$ {"meta":{"schema_version":"1.1","company_name_primary":"string","credit_code":"string|null","overall_confidence":"string"},"query":{"company_name":"string","credit_code":"string|null","region":"string|null","intent":"full"},"sections":[{"id":"registry|ownership|business|scale|certifications|timeline|contacts|compliance_risks|market_risks|procurement_signals|opportunities|key_people|ai_assessment","title":"string","summary":"string","confidence":"high|medium|low","content":{},"sources":[]}],"relations":{"section_order":["registry","ownership","business","scale","certifications","timeline","contacts","compliance_risks","market_risks","procurement_signals","opportunities","key_people","ai_assessment"],"for_risk_control":["registry","ownership","compliance_risks","market_risks"],"for_procurement_followup":["procurement_signals","certifications","timeline","key_people","ai_assessment"]},"disclaimer":"string"} $$,
                true
            )
            ON CONFLICT (code, version) DO NOTHING;

            INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
                cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
                permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
            VALUES (
                'a3000001-0000-4000-8000-00000000000d',
                'vendor.intel.lookup',
                'AI 供应商情报调查',
                '按企业名称调查公开供应商情报（13章结构化报告）',
                'mock',
                'mock',
                'a2000001-0000-4000-8000-00000000000d',
                7776000,
                jsonb_build_array('company_name', 'credit_code'),
                jsonb_build_array('company_name', 'credit_code', 'region', 'vendor_id'),
                8192,
                1.00,
                'biz.ai.vendor_intel.lookup',
                10,
                true,
                true
            )
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
            VALUES
                ('30000000-0000-4000-8000-0000000000cd', 'biz.ai.vendor_intel.lookup', 'AI-供应商情报调查', 'api', 'ai', 'vendor_intel', 1, NOW())
            ON CONFLICT ("PermissionCode") DO NOTHING;

            INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
            SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
            FROM sys_role r
            JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.vendor_intel.lookup' AND p."Status" = 1
            WHERE (
                r."RoleCode" IN ('SYS_ADMIN', 'biz_all', 'purchase_buyer', 'pur_manager', 'pur_staff', 'PURCHASER')
            )
            AND NOT EXISTS (
                SELECT 1 FROM sys_role_permission x
                WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
            );
            """);

        migrationBuilder.Sql(
            """
            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'vendor.intel.lookup';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM sys_role_permission WHERE "PermissionId" IN (
                SELECT "PermissionId" FROM sys_permission WHERE "PermissionCode" = 'biz.ai.vendor_intel.lookup'
            );
            DELETE FROM sys_permission WHERE "PermissionCode" = 'biz.ai.vendor_intel.lookup';
            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'vendor.intel.lookup';
            DELETE FROM public.ai_scenario WHERE code = 'vendor.intel.lookup';
            DELETE FROM public.ai_prompt_template WHERE code = 'vendor.intel.lookup';
            DROP TABLE IF EXISTS public.vendor_intel_report;
            """);
    }
}

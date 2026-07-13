using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260714120000_CustomerIntelReportAndScenario")]
public partial class CustomerIntelReportAndScenario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.customer_intel_report (
                id varchar(36) NOT NULL PRIMARY KEY,
                customer_id varchar(36) NULL,
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
            CREATE INDEX IF NOT EXISTS ix_customer_intel_report_customer_created
                ON public.customer_intel_report (customer_id, created_at DESC);
            CREATE INDEX IF NOT EXISTS ix_customer_intel_report_fingerprint_latest
                ON public.customer_intel_report (query_fingerprint, is_latest);

            INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
            VALUES (
                'a2000001-0000-4000-8000-00000000000a',
                'customer.intel.lookup',
                1,
                $$你是面向中国销售与风控人员的客户情报调查助手。根据企业名称等信息，仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case。所有描述性字符串值必须使用简体中文。未知用 null，无数据用空数组；禁止编造司法风险数量、行政处罚、联系方式。必须输出 meta、query、sections（8章：registry、business、scale、compliance_risks、opportunities、contacts、timeline、ai_assessment）、relations、disclaimer。$$,
                $$请调查以下企业的公开信息：企业名称 {{company_name}}；统一社会信用代码 {{credit_code}}；地区 {{region}}。返回客户情报 JSON。$$,
                'json',
                $$ {"meta":{"schema_version":"1.0","company_name_primary":"string","credit_code":"string|null","overall_confidence":"string"},"sections":[{"id":"registry|business|scale|compliance_risks|opportunities|contacts|timeline|ai_assessment","title":"string","summary":"string","confidence":"string","content":{},"sources":[]}],"disclaimer":"string"} $$,
                true
            )
            ON CONFLICT (code, version) DO NOTHING;

            INSERT INTO public.ai_scenario (id, code, name, description, provider_code, model, prompt_template_id,
                cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
                permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search)
            VALUES (
                'a3000001-0000-4000-8000-00000000000a',
                'customer.intel.lookup',
                'AI 客户情报调查',
                '按企业名称调查公开客户情报（8章结构化报告）',
                'mock',
                'mock',
                'a2000001-0000-4000-8000-00000000000a',
                7776000,
                jsonb_build_array('company_name', 'credit_code'),
                jsonb_build_array('company_name', 'credit_code', 'region', 'customer_id'),
                8192,
                1.00,
                'biz.ai.customer_intel.lookup',
                10,
                true,
                true
            )
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
            VALUES
                ('30000000-0000-4000-8000-0000000000ca', 'biz.ai.customer_intel.lookup', 'AI-客户情报调查', 'api', 'ai', 'customer_intel', 1, NOW())
            ON CONFLICT ("PermissionCode") DO NOTHING;

            INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
            SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
            FROM sys_role r
            JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.customer_intel.lookup' AND p."Status" = 1
            WHERE (
                r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
                OR EXISTS (
                    SELECT 1 FROM sys_role_permission rp
                    JOIN sys_permission pr ON pr."PermissionId" = rp."PermissionId"
                    WHERE rp."RoleId" = r."RoleId"
                      AND pr."PermissionCode" = 'customer.read'
                      AND pr."Status" = 1
                )
            )
            AND NOT EXISTS (
                SELECT 1 FROM sys_role_permission x
                WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
            );
            """);

        migrationBuilder.Sql(
            """
            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.intel.lookup';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM sys_role_permission WHERE "PermissionId" IN (
                SELECT "PermissionId" FROM sys_permission WHERE "PermissionCode" = 'biz.ai.customer_intel.lookup'
            );
            DELETE FROM sys_permission WHERE "PermissionCode" = 'biz.ai.customer_intel.lookup';
            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.intel.lookup';
            DELETE FROM public.ai_scenario WHERE code = 'customer.intel.lookup';
            DELETE FROM public.ai_prompt_template WHERE code = 'customer.intel.lookup';
            DROP TABLE IF EXISTS public.customer_intel_report;
            """);
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260714200000_CustomerIntelLookupSchemaV2")]
public partial class CustomerIntelLookupSchemaV2 : Migration
{
    private const string SchemaV2Hint =
        """{"meta":{"schema_version":"1.1","company_name_primary":"string","credit_code":"string|null","overall_confidence":"string"},"query":{"company_name":"string","credit_code":"string|null","region":"string|null","intent":"full"},"sections":[{"id":"registry|ownership|business|scale|certifications|timeline|contacts|compliance_risks|market_risks|procurement_signals|opportunities|key_people|ai_assessment","title":"string","summary":"string","confidence":"high|medium|low","content":{},"sources":[]}],"relations":{"section_order":["registry","ownership","business","scale","certifications","timeline","contacts","compliance_risks","market_risks","procurement_signals","opportunities","key_people","ai_assessment"],"for_risk_control":["registry","ownership","compliance_risks","market_risks"],"for_sales_followup":["opportunities","procurement_signals","timeline","key_people","ai_assessment"]},"disclaimer":"string"}""";

    private const string ContentStructureAppend =
        """
         sections 必须包含且 id 固定为：registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment。每章含 id、title、summary、confidence（high|medium|low）、content、sources。ownership：shareholders[name,share_ratio,shareholder_type,note]、parent_company、ultimate_controller、listed_info(is_listed,stock_code,exchange)、ownership_notes。certifications：is_high_tech_enterprise、items[name,certification_type,issuer,valid_until,status]、honors[]。market_risks：risk_level、items[type,title,description,severity]、customer_concentration、competition_summary、policy_risks[]。procurement_signals：items[type,title,description,urgency,suggested_actions]、expansion_signals[]、bom_needs[]、localization_signals[]。key_people：people[name,role,department,background,public_contact]、org_summary、rd_team_summary。compliance_risks.checks[].count 查不到填 null，status 可填 unknown。ai_assessment.dimensions[].basis_section_ids 必须引用事实章节 id。relations.section_order 为上述 13 个 id 顺序数组。
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var hintSql = SchemaV2Hint.Replace("'", "''");
        var appendSql = ContentStructureAppend.Replace("'", "''");
        migrationBuilder.Sql(
            $@"
            UPDATE public.ai_prompt_template
            SET system_prompt = $$你是面向中国销售与风控人员的客户情报调查助手。根据企业名称等信息，仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case。所有描述性字符串值必须使用简体中文。未知用 null，无数据用空数组；禁止编造司法风险数量、行政处罚、联系方式、股东持股比例。必须输出 meta（schema_version 填 1.1）、query、sections（固定 13 章）、relations、disclaimer。{appendSql}$$,
                user_prompt_template = $$请调查以下企业的公开信息：企业名称 {{{{company_name}}}}；统一社会信用代码 {{{{credit_code}}}}；地区 {{{{region}}}}。返回 13 章客户情报 JSON（registry、ownership、business、scale、certifications、timeline、contacts、compliance_risks、market_risks、procurement_signals、opportunities、key_people、ai_assessment）。$$,
                json_schema_hint = '{hintSql}',
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'customer.intel.lookup' AND version = 1;

            UPDATE public.ai_scenario
            SET description = '按企业名称调查公开客户情报（13章结构化报告）',
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'customer.intel.lookup';

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.intel.lookup';
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.ai_prompt_template
            SET system_prompt = $$你是面向中国销售与风控人员的客户情报调查助手。根据企业名称等信息，仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case。所有描述性字符串值必须使用简体中文。未知用 null，无数据用空数组；禁止编造司法风险数量、行政处罚、联系方式。必须输出 meta、query、sections（8章：registry、business、scale、compliance_risks、opportunities、contacts、timeline、ai_assessment）、relations、disclaimer。$$,
                user_prompt_template = $$请调查以下企业的公开信息：企业名称 {{company_name}}；统一社会信用代码 {{credit_code}}；地区 {{region}}。返回客户情报 JSON。$$,
                json_schema_hint = $$ {"meta":{"schema_version":"1.0","company_name_primary":"string","credit_code":"string|null","overall_confidence":"string"},"sections":[{"id":"registry|business|scale|compliance_risks|opportunities|contacts|timeline|ai_assessment","title":"string","summary":"string","confidence":"string","content":{},"sources":[]}],"disclaimer":"string"} $$,
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'customer.intel.lookup' AND version = 1;

            UPDATE public.ai_scenario
            SET description = '按企业名称调查公开客户情报（8章结构化报告）',
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'customer.intel.lookup';

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.intel.lookup';
            """);
    }
}

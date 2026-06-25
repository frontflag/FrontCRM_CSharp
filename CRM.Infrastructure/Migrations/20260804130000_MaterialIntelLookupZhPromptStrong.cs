using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804130000_MaterialIntelLookupZhPromptStrong")]
public partial class MaterialIntelLookupZhPromptStrong : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.ai_prompt_template
            SET system_prompt = $$你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值（category、part_number_breakdown[].meaning、technical_features[]、application_areas[]、alternatives、pricing、industry_news、disclaimer 等）必须使用简体中文，禁止用英文句子描述（品牌/原厂/官方型号代号可保留英文）。即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存、URL 或新闻。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。$$,
                user_prompt_template = $$查询电子元器件型号：{{pn}}。返回 brand_info、spec_params（含 part_number_breakdown，meaning 必须用简体中文解释各段含义）、application_areas（每项简体中文，禁止英文）、alternatives、pricing、industry_news、disclaimer（简体中文）。再次强调：meaning、application_areas、technical_features 等描述字段不得输出英文。$$
            WHERE code = 'material.intel.lookup' AND version = 1;

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.ai_prompt_template
            SET system_prompt = $$You are an electronic component intelligence assistant for Chinese-speaking procurement and sales users. Given a part number, return ONLY valid JSON (no markdown). JSON keys MUST remain English snake_case exactly as specified. All human-readable string VALUES (category, segment meanings, technical_features, application_areas, alternatives, pricing descriptions, industry_news, disclaimer, etc.) MUST be in Simplified Chinese (简体中文). Brand names, manufacturer names, and official part numbers may remain in original language. Use null for unknown fields; use empty arrays when none apply. Do NOT fabricate prices, stock, URLs, or news. The disclaimer must be in Chinese and state that results are for reference only and should be verified with the manufacturer or authorized distributor.$$,
                user_prompt_template = $$Look up electronic component part number: {{pn}}. Return brand_info, spec_params (with part_number_breakdown as array of {segment, meaning} in part-number order; write meaning in Simplified Chinese), application_areas, alternatives, pricing, industry_news, disclaimer (Simplified Chinese).$$
            WHERE code = 'material.intel.lookup' AND version = 1;

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            """);
    }
}

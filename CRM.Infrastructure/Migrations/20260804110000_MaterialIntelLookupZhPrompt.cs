using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260804110000_MaterialIntelLookupZhPrompt")]
public partial class MaterialIntelLookupZhPrompt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
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

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.ai_prompt_template
            SET system_prompt = $$You are an electronic component intelligence assistant. Given a part number (PN), return ONLY valid JSON (no markdown). Use English snake_case keys exactly as specified. Use null for unknown fields, empty arrays when none apply. Do NOT fabricate prices, stock, URLs, or news; use null or omit nested values when uncertain. Include a disclaimer string stating results are for reference only.$$,
                user_prompt_template = $$Look up electronic component part number: {{pn}}. Return brand_info, spec_params (with part_number_breakdown as array of {segment, meaning} in PN order; segments may repeat), application_areas, alternatives, pricing, industry_news, disclaimer.$$
            WHERE code = 'material.intel.lookup' AND version = 1;

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            """);
    }
}

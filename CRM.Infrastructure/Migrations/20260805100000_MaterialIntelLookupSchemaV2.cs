using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805100000_MaterialIntelLookupSchemaV2")]
public partial class MaterialIntelLookupSchemaV2 : Migration
{
    private const string SchemaV2Hint =
        """{"brand_info":{"brand":"string|null","manufacturer":"string|null","origin":"string|null","product_line":"string|null","product_category":"string|null","series":"string|null"},"spec_params":{"category":"string|null","part_number_breakdown":[{"segment":"string","meaning":"string"}],"technical_features":["string"],"electrical_params":{},"datasheet_url":"string|null","image_url":"string|null"},"application_areas":["string"],"alternatives":[{"part_number":"string","brand":"string|null","note":"string|null"}],"pricing":{"market_price":{"reference_price":"string|null","currency":"string|null","note":"string|null"},"market_conditions":{"availability":"string|null","trend":"string|null","note":"string|null"},"price_tiers":[{"quantity":"string","unit_price":"string","currency":"string|null"}],"distributors":[{"distributor":"string","price_range":"string|null","currency":"string|null","stock_status":"string|null","moq":"string|null","last_updated":"string|null"}]},"industry_news":[{"title":"string","url":"string|null","summary":"string|null"}],"disclaimer":"string"}""";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var hintSql = SchemaV2Hint.Replace("'", "''");
        migrationBuilder.Sql(
            $@"
            UPDATE public.ai_prompt_template
            SET system_prompt = $$你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值必须使用简体中文（品牌/原厂/官方型号代号可保留英文）；即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存、URL 或新闻。spec_params 必须包含 datasheet_url 与 image_url（https 可访问链接，无法确认填 null）。alternatives 必须为对象数组，每项含 part_number、brand、note。pricing 必须为对象，含 market_price、market_conditions、price_tiers、distributors。industry_news 必须为对象数组，每项含 title、url、summary。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。$$,
                user_prompt_template = $$查询电子元器件型号：{{{{pn}}}}。严格按 JSON 结构返回：brand_info、spec_params（含 part_number_breakdown，meaning 用简体中文；含 datasheet_url、image_url）、application_areas（简体中文字符串数组）、alternatives（对象数组：part_number、brand、note）、pricing（market_price、market_conditions、price_tiers、distributors）、industry_news（对象数组：title、url、summary）、disclaimer。描述字段用简体中文；未知 null，无数组项 []。$$,
                json_schema_hint = '{hintSql}',
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'material.intel.lookup' AND version = 1;

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.ai_prompt_template
            SET system_prompt = $$你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值（category、part_number_breakdown[].meaning、technical_features[]、application_areas[]、alternatives、pricing、industry_news、disclaimer 等）必须使用简体中文，禁止用英文句子描述（品牌/原厂/官方型号代号可保留英文）。即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存或新闻。spec_params 中必须包含 datasheet_url（原厂或授权渠道 DataSheet 规格书链接）与 image_url（物料产品图链接）；可经联网检索公开来源，须为可访问的 https 链接，无法确认时填 null，禁止编造链接。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。$$,
                user_prompt_template = $$查询电子元器件型号：{{pn}}。返回 brand_info、spec_params（含 part_number_breakdown，meaning 必须用简体中文解释各段含义；须尽量提供 datasheet_url 与 image_url，无法确认填 null）、application_areas（每项简体中文，禁止英文）、alternatives、pricing、industry_news、disclaimer（简体中文）。再次强调：meaning、application_areas、technical_features 等描述字段不得输出英文。$$,
                json_schema_hint = $${"brand_info":{"brand":"string|null","manufacturer":"string|null","origin":"string|null"},"spec_params":{"category":"string|null","part_number_breakdown":[{"segment":"string","meaning":"string"}],"technical_features":["string"],"electrical_params":{},"datasheet_url":"string|null","image_url":"string|null"},"application_areas":["string"],"alternatives":["string"],"pricing":{"market_price":{},"market_conditions":{}},"industry_news":["string"],"disclaimer":"string"}$$,
                modify_time = (now() AT TIME ZONE 'utc')
            WHERE code = 'material.intel.lookup' AND version = 1;

            DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';
            """);
    }
}

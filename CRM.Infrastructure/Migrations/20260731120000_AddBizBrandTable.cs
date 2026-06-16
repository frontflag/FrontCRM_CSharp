using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>品牌主数据表 biz_brand。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260731120000_AddBizBrandTable")]
    public partial class AddBizBrandTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.biz_brand (
    id BIGSERIAL PRIMARY KEY,
    brand_e_name character varying(200) NULL,
    brand_c_name character varying(200) NULL,
    standard_brand character varying(300) NULL,
    alias character varying(500) NULL,
    country_code character varying(10) NULL,
    country character varying(100) NULL
);

COMMENT ON TABLE public.biz_brand IS '品牌主数据';
COMMENT ON COLUMN public.biz_brand.id IS '自增主键';
COMMENT ON COLUMN public.biz_brand.brand_e_name IS '品牌英文名（BrandEName）';
COMMENT ON COLUMN public.biz_brand.brand_c_name IS '品牌中文名（BrandCName）';
COMMENT ON COLUMN public.biz_brand.standard_brand IS '标准品牌名（StandardBrand）';
COMMENT ON COLUMN public.biz_brand.alias IS '别名（Alias）';
COMMENT ON COLUMN public.biz_brand.country_code IS '国家/地区代码（CountryCode）';
COMMENT ON COLUMN public.biz_brand.country IS '国家/地区名称（Country）';

CREATE INDEX IF NOT EXISTS ""IX_biz_brand_brand_e_name""
    ON public.biz_brand (brand_e_name);

CREATE INDEX IF NOT EXISTS ""IX_biz_brand_standard_brand""
    ON public.biz_brand (standard_brand);

CREATE INDEX IF NOT EXISTS ""IX_biz_brand_country_code""
    ON public.biz_brand (country_code);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.biz_brand;");
        }
    }
}

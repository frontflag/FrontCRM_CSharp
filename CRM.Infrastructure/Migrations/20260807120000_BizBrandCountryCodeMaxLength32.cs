using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// biz_brand.country_code 由 varchar(10) 扩至 varchar(32)。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260807120000_BizBrandCountryCodeMaxLength32")]
    public partial class BizBrandCountryCodeMaxLength32 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.biz_brand
  ALTER COLUMN country_code TYPE character varying(32);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.biz_brand
  ALTER COLUMN country_code TYPE character varying(10);
");
        }
    }
}

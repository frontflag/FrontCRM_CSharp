using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260731140000_BizBrandRemark")]
    public partial class BizBrandRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.biz_brand
  ADD COLUMN IF NOT EXISTS remark character varying(500) NULL;

COMMENT ON COLUMN public.biz_brand.remark IS '备注';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.biz_brand DROP COLUMN IF EXISTS remark;");
        }
    }
}

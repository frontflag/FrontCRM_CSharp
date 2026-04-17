using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关公司：<c>Name</c> 重命名为 <c>cname</c>；新增 <c>ename</c>、<c>Type</c>（10 深圳 / 20 香港）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260618100000_CustomsBrokerCnameEnameRegionType")]
    public partial class CustomsBrokerCnameEnameRegionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_broker
  RENAME COLUMN ""Name"" TO ""cname"";

ALTER TABLE IF EXISTS public.customs_broker
  ADD COLUMN IF NOT EXISTS ""ename"" character varying(200) NULL;

ALTER TABLE IF EXISTS public.customs_broker
  ADD COLUMN IF NOT EXISTS ""Type"" smallint NOT NULL DEFAULT 10;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_broker DROP COLUMN IF EXISTS ""Type"";
ALTER TABLE IF EXISTS public.customs_broker DROP COLUMN IF EXISTS ""ename"";
ALTER TABLE IF EXISTS public.customs_broker RENAME COLUMN ""cname"" TO ""Name"";
");
        }
    }
}

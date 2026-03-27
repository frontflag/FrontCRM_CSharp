using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326191000_AddVendorAuditRemarkAndAdjustStatusDefaults")]
    public partial class AddVendorAuditRemarkAndAdjustStatusDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditRemark",
                table: "vendorinfo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // 旧状态兼容：
            // 0(草稿) -> 1(新建)
            // 1(待审核) -> 2(待审核)
            // 2(合作中) -> 10(已审核)
            migrationBuilder.Sql(@"
UPDATE public.vendorinfo
SET ""Status"" = CASE ""Status""
    WHEN 0 THEN 1
    WHEN 1 THEN 2
    WHEN 2 THEN 10
    ELSE ""Status""
END;
");

            migrationBuilder.Sql(@"ALTER TABLE public.vendorinfo ALTER COLUMN ""Status"" SET DEFAULT 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.vendorinfo ALTER COLUMN ""Status"" SET DEFAULT 0;");

            migrationBuilder.DropColumn(
                name: "AuditRemark",
                table: "vendorinfo");
        }
    }
}


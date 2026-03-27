using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326180000_AddCustomerAuditRemarkAndAdjustStatusDefaults")]
    public partial class AddCustomerAuditRemarkAndAdjustStatusDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditRemark",
                table: "customerinfo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // 旧状态兼容：
            // - 旧 1(启用) -> 新 10(已审核)
            // - 旧 0(停用/草稿) -> 新 1(新建)
            migrationBuilder.Sql(@"
UPDATE public.customerinfo
SET ""Status"" = CASE ""Status""
    WHEN 1 THEN 10
    WHEN 0 THEN 1
    ELSE ""Status""
END;
");

            // 默认状态改为新建(1)
            migrationBuilder.Sql(@"ALTER TABLE public.customerinfo ALTER COLUMN ""Status"" SET DEFAULT 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.customerinfo ALTER COLUMN ""Status"" SET DEFAULT 0;");

            migrationBuilder.DropColumn(
                name: "AuditRemark",
                table: "customerinfo");
        }
    }
}


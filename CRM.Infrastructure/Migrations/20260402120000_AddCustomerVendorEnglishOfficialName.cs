using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 客户/供应商主表增加公司英文全称。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260402120000_AddCustomerVendorEnglishOfficialName")]
    public partial class AddCustomerVendorEnglishOfficialName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnglishOfficialName",
                table: "customerinfo",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishOfficialName",
                table: "vendorinfo",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EnglishOfficialName", table: "customerinfo");
            migrationBuilder.DropColumn(name: "EnglishOfficialName", table: "vendorinfo");
        }
    }
}

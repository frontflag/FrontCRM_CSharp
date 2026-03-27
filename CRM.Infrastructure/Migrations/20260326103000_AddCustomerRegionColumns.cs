using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// Persist 省/市/区 on customerinfo (地区级联); previously model fields were NotMapped and never saved.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326103000_AddCustomerRegionColumns")]
    public partial class AddCustomerRegionColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "customerinfo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "customerinfo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "customerinfo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Province", table: "customerinfo");
            migrationBuilder.DropColumn(name: "City", table: "customerinfo");
            migrationBuilder.DropColumn(name: "District", table: "customerinfo");
        }
    }
}

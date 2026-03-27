using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 供应商备注/官网/采购员名/付款方式（字符串），与账期天数（Payment）区分。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326120000_AddVendorRemarkWebsitePaymentMethod")]
    public partial class AddVendorRemarkWebsitePaymentMethod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "vendorinfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "vendorinfo",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaserName",
                table: "vendorinfo",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "vendorinfo",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Remark", table: "vendorinfo");
            migrationBuilder.DropColumn(name: "Website", table: "vendorinfo");
            migrationBuilder.DropColumn(name: "PurchaserName", table: "vendorinfo");
            migrationBuilder.DropColumn(name: "PaymentMethod", table: "vendorinfo");
        }
    }
}

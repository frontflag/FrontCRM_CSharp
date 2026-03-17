using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRFQTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BlackListAt",
                table: "customerinfo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlackListByUserId",
                table: "customerinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlackListByUserName",
                table: "customerinfo",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlackListReason",
                table: "customerinfo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeleteReason",
                table: "customerinfo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserName",
                table: "customerinfo",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rfq",
                columns: table => new
                {
                    rfq_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    rfq_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    contact_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sales_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    rfq_type = table.Column<short>(type: "smallint", nullable: false),
                    quote_method = table.Column<short>(type: "smallint", nullable: false),
                    assign_method = table.Column<short>(type: "smallint", nullable: false),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    target_type = table.Column<short>(type: "smallint", nullable: false),
                    importance = table.Column<short>(type: "smallint", nullable: false),
                    is_last_inquiry = table.Column<bool>(type: "boolean", nullable: false),
                    project_background = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    competitor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    item_count = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rfq_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq", x => x.rfq_id);
                });

            migrationBuilder.CreateTable(
                name: "rfqitem",
                columns: table => new
                {
                    item_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    rfq_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    customer_mpn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mpn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    price_currency = table.Column<short>(type: "smallint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 1m),
                    production_date = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_package_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    moq = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    alternatives = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfqitem", x => x.item_id);
                    table.ForeignKey(
                        name: "FK_rfqitem_rfq_rfq_id",
                        column: x => x.rfq_id,
                        principalTable: "rfq",
                        principalColumn: "rfq_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rfq_rfq_code",
                table: "rfq",
                column: "rfq_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfqitem_rfq_id",
                table: "rfqitem",
                column: "rfq_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rfqitem");

            migrationBuilder.DropTable(
                name: "rfq");

            migrationBuilder.DropColumn(
                name: "BlackListAt",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "BlackListByUserId",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "BlackListByUserName",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "BlackListReason",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "DeleteReason",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "DeletedByUserName",
                table: "customerinfo");
        }
    }
}

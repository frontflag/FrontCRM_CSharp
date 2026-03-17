using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "quote",
                columns: table => new
                {
                    QuoteId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    quote_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rfq_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    rfq_item_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    mpn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    sales_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    purchase_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    quote_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    remark = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote", x => x.QuoteId);
                });

            migrationBuilder.CreateTable(
                name: "quoteitem",
                columns: table => new
                {
                    QuoteItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    quote_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    vendor_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    vendor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vendor_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    contact_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    price_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    mpn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand_origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lead_time = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    label_type = table.Column<short>(type: "smallint", nullable: false),
                    wafer_origin = table.Column<short>(type: "smallint", nullable: false),
                    package_origin = table.Column<short>(type: "smallint", nullable: false),
                    free_shipping = table.Column<bool>(type: "boolean", nullable: false),
                    currency = table.Column<short>(type: "smallint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    unit_price = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    converted_price = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    min_package_qty = table.Column<int>(type: "integer", nullable: false),
                    min_package_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    stock_qty = table.Column<int>(type: "integer", nullable: false),
                    moq = table.Column<int>(type: "integer", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quoteitem", x => x.QuoteItemId);
                    table.ForeignKey(
                        name: "FK_quoteitem_quote_quote_id",
                        column: x => x.quote_id,
                        principalTable: "quote",
                        principalColumn: "QuoteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_quote_quote_code",
                table: "quote",
                column: "quote_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quoteitem_quote_id",
                table: "quoteitem",
                column: "quote_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quoteitem");

            migrationBuilder.DropTable(
                name: "quote");
        }
    }
}

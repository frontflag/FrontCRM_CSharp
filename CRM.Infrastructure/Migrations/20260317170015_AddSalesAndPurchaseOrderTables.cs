using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesAndPurchaseOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchaseorder",
                columns: table => new
                {
                    PurchaseOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    purchase_order_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    vendor_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vendor_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vendor_contact_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    purchase_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    purchase_user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_group_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    sales_group_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    err_status = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    currency = table.Column<short>(type: "smallint", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    convert_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    item_rows = table.Column<int>(type: "integer", nullable: false),
                    stock_status = table.Column<short>(type: "smallint", nullable: false),
                    finance_status = table.Column<short>(type: "smallint", nullable: false),
                    stock_out_status = table.Column<short>(type: "smallint", nullable: false),
                    invoice_status = table.Column<short>(type: "smallint", nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inner_comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseorder", x => x.PurchaseOrderId);
                });

            migrationBuilder.CreateTable(
                name: "sellorder",
                columns: table => new
                {
                    SellOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sell_order_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    customer_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sales_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    sales_user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    purchase_group_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    err_status = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    currency = table.Column<short>(type: "smallint", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    convert_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    item_rows = table.Column<int>(type: "integer", nullable: false),
                    purchase_order_status = table.Column<short>(type: "smallint", nullable: false),
                    stock_out_status = table.Column<short>(type: "smallint", nullable: false),
                    stock_in_status = table.Column<short>(type: "smallint", nullable: false),
                    finance_receipt_status = table.Column<short>(type: "smallint", nullable: false),
                    finance_payment_status = table.Column<short>(type: "smallint", nullable: false),
                    invoice_status = table.Column<short>(type: "smallint", nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sellorder", x => x.SellOrderId);
                });

            migrationBuilder.CreateTable(
                name: "sellorderitem",
                columns: table => new
                {
                    SellOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sell_order_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    quote_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    product_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    pn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    customer_pn_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    purchased_qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    price = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    currency = table.Column<short>(type: "smallint", nullable: false),
                    date_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sellorderitem", x => x.SellOrderItemId);
                    table.ForeignKey(
                        name: "FK_sellorderitem_sellorder_sell_order_id",
                        column: x => x.sell_order_id,
                        principalTable: "sellorder",
                        principalColumn: "SellOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchaseorderitem",
                columns: table => new
                {
                    PurchaseOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    purchase_order_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sell_order_item_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    vendor_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    product_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    pn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m),
                    cost = table.Column<decimal>(type: "numeric(18,6)", nullable: false, defaultValue: 0m),
                    currency = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    stock_in_status = table.Column<short>(type: "smallint", nullable: false),
                    finance_payment_status = table.Column<short>(type: "smallint", nullable: false),
                    stock_out_status = table.Column<short>(type: "smallint", nullable: false),
                    err_status = table.Column<short>(type: "smallint", nullable: false),
                    delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaseorderitem", x => x.PurchaseOrderItemId);
                    table.ForeignKey(
                        name: "FK_purchaseorderitem_purchaseorder_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalTable: "purchaseorder",
                        principalColumn: "PurchaseOrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchaseorderitem_sellorderitem_sell_order_item_id",
                        column: x => x.sell_order_item_id,
                        principalTable: "sellorderitem",
                        principalColumn: "SellOrderItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchaseorder_purchase_order_code",
                table: "purchaseorder",
                column: "purchase_order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchaseorderitem_purchase_order_id",
                table: "purchaseorderitem",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchaseorderitem_sell_order_item_id",
                table: "purchaseorderitem",
                column: "sell_order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_sellorder_sell_order_code",
                table: "sellorder",
                column: "sell_order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sellorderitem_sell_order_id",
                table: "sellorderitem",
                column: "sell_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchaseorderitem");

            migrationBuilder.DropTable(
                name: "purchaseorder");

            migrationBuilder.DropTable(
                name: "sellorderitem");

            migrationBuilder.DropTable(
                name: "sellorder");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseRequisitionModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchaserequisition",
                columns: table => new
                {
                    purchase_requisition_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    bill_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sell_order_item_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    sell_order_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    expected_purchase_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<short>(type: "smallint", nullable: false),
                    purchase_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    sales_user_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    quote_vendor_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    quote_cost = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    pn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchaserequisition", x => x.purchase_requisition_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchaserequisition");
        }
    }
}

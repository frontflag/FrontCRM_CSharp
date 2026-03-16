using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CRM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSerialNumberAndErrorLogAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "customerinfo",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "customerinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "customerinfo",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "sys_error_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ErrorDetail = table.Column<string>(type: "text", nullable: true),
                    DocumentNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DataId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequestPath = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestBody = table.Column<string>(type: "text", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolveRemark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_error_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sys_serial_number",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SequenceLength = table.Column<int>(type: "integer", nullable: false),
                    CurrentSequence = table.Column<int>(type: "integer", nullable: false),
                    ResetByYear = table.Column<bool>(type: "boolean", nullable: false),
                    ResetByMonth = table.Column<bool>(type: "boolean", nullable: false),
                    LastResetYear = table.Column<int>(type: "integer", nullable: true),
                    LastResetMonth = table.Column<int>(type: "integer", nullable: true),
                    Remark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_serial_number", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "sys_serial_number",
                columns: new[] { "Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Customer", "客户", "Cus", null, false, false, 4, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Vendor", "供应商", "Ven", null, false, false, 4, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Inquiry", "询价/需求", "INQ", null, false, false, 4, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Quotation", "报价", "QUO", null, false, false, 4, null },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "SalesOrder", "销售订单", "SO", null, false, false, 4, null },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "PurchaseOrder", "采购订单", "PO", null, false, false, 4, null },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "StockIn", "入库", "SIN", null, false, false, 4, null },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "StockOut", "出库", "SOUT", null, false, false, 4, null },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Inventory", "库存调整", "INV", null, false, false, 4, null },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Receipt", "收款", "REC", null, false, false, 4, null },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Payment", "付款", "PAY", null, false, false, 4, null },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "InputInvoice", "进项发票", "VINV", null, false, false, 4, null },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "OutputInvoice", "销项发票", "SINV", null, false, false, 4, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_sys_serial_number_ModuleCode",
                table: "sys_serial_number",
                column: "ModuleCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_error_log");

            migrationBuilder.DropTable(
                name: "sys_serial_number");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "customerinfo");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "customerinfo");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSysTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建流水号管理表
            migrationBuilder.CreateTable(
                name: "sys_serial_number",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModuleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SequenceLength = table.Column<int>(type: "integer", nullable: false, defaultValue: 4),
                    CurrentSequence = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ResetByYear = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ResetByMonth = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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

            // 创建唯一索引
            migrationBuilder.CreateIndex(
                name: "IX_sys_serial_number_ModuleCode",
                table: "sys_serial_number",
                column: "ModuleCode",
                unique: true);

            // 插入种子数据
            var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "sys_serial_number",
                columns: new[] { "Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime" },
                values: new object[,]
                {
                    { 1, "Customer", "客户", "Cus", 4, 0, false, false, seedTime },
                    { 2, "Vendor", "供应商", "Ven", 4, 0, false, false, seedTime },
                    { 3, "Inquiry", "询价/需求", "INQ", 4, 0, false, false, seedTime },
                    { 4, "Quotation", "报价", "QUO", 4, 0, false, false, seedTime },
                    { 5, "SalesOrder", "销售订单", "SO", 4, 0, false, false, seedTime },
                    { 6, "PurchaseOrder", "采购订单", "PO", 4, 0, false, false, seedTime },
                    { 7, "StockIn", "入库", "SIN", 4, 0, false, false, seedTime },
                    { 8, "StockOut", "出库", "SOUT", 4, 0, false, false, seedTime },
                    { 9, "Inventory", "库存调整", "INV", 4, 0, false, false, seedTime },
                    { 10, "Receipt", "收款", "REC", 4, 0, false, false, seedTime },
                    { 11, "Payment", "付款", "PAY", 4, 0, false, false, seedTime },
                    { 12, "InputInvoice", "进项发票", "VINV", 4, 0, false, false, seedTime },
                    { 13, "OutputInvoice", "销项发票", "SINV", 4, 0, false, false, seedTime }
                });

            // 创建错误日志表
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
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ResolveRemark = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_error_log", x => x.Id);
                });

            // 创建错误日志索引
            migrationBuilder.CreateIndex(
                name: "IX_sys_error_log_OccurredAt",
                table: "sys_error_log",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_sys_error_log_IsResolved",
                table: "sys_error_log",
                column: "IsResolved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sys_serial_number");
            migrationBuilder.DropTable(name: "sys_error_log");
        }
    }
}

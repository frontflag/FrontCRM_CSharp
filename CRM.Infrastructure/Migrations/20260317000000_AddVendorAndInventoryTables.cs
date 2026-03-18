using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorAndInventoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===== 供应商相关表 =====
            
            // 供应商主表
            migrationBuilder.CreateTable(
                name: "vendor",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OfficialName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NickName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Level = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    CreditLine = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payment = table.Column<short>(type: "smallint", nullable: false),
                    TradeCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    UniformNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor", x => x.Id);
                });

            // 供应商地址表
            migrationBuilder.CreateTable(
                name: "vendor_address",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AddressType = table.Column<short>(type: "smallint", nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_address", x => x.Id);
                });

            // 供应商联系人表
            migrationBuilder.CreateTable(
                name: "vendorcontactinfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QQ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WeChat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorcontactinfo", x => x.Id);
                });

            // 供应商银行账户表
            migrationBuilder.CreateTable(
                name: "vendorbankinfo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendorbankinfo", x => x.Id);
                });

            // ===== 库存相关表 =====

            // 库存主表
            migrationBuilder.CreateTable(
                name: "stock",
                columns: table => new
                {
                    StockId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LockedQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock", x => x.StockId);
                });

            // 入库单主表
            migrationBuilder.CreateTable(
                name: "stockin",
                columns: table => new
                {
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockInCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StockInType = table.Column<short>(type: "smallint", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockin", x => x.StockInId);
                });

            // 入库单明细表
            migrationBuilder.CreateTable(
                name: "stockinitem",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProductionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockinitem", x => x.ItemId);
                });

            // 出库单主表
            migrationBuilder.CreateTable(
                name: "stockout",
                columns: table => new
                {
                    StockOutId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockOutCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StockOutType = table.Column<short>(type: "smallint", nullable: false),
                    SourceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WarehouseId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockOutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockout", x => x.StockOutId);
                });

            // 出库单明细表
            migrationBuilder.CreateTable(
                name: "stockoutitem",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    StockOutId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    MaterialId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LocationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockoutitem", x => x.ItemId);
                });

            // 出库申请单表
            migrationBuilder.CreateTable(
                name: "stockoutrequest",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RequestCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SalesOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    RequestUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stockoutrequest", x => x.Id);
                });

            // 创建索引
            migrationBuilder.CreateIndex(name: "IX_vendor_Code", table: "vendor", column: "Code");
            migrationBuilder.CreateIndex(name: "IX_vendor_address_VendorId", table: "vendor_address", column: "VendorId");
            migrationBuilder.CreateIndex(name: "IX_vendorcontactinfo_VendorId", table: "vendorcontactinfo", column: "VendorId");
            migrationBuilder.CreateIndex(name: "IX_vendorbankinfo_VendorId", table: "vendorbankinfo", column: "VendorId");
            migrationBuilder.CreateIndex(name: "IX_stock_MaterialId", table: "stock", column: "MaterialId");
            migrationBuilder.CreateIndex(name: "IX_stock_WarehouseId", table: "stock", column: "WarehouseId");
            migrationBuilder.CreateIndex(name: "IX_stockin_StockInCode", table: "stockin", column: "StockInCode");
            migrationBuilder.CreateIndex(name: "IX_stockinitem_StockInId", table: "stockinitem", column: "StockInId");
            migrationBuilder.CreateIndex(name: "IX_stockout_StockOutCode", table: "stockout", column: "StockOutCode");
            migrationBuilder.CreateIndex(name: "IX_stockoutitem_StockOutId", table: "stockoutitem", column: "StockOutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "stockoutrequest");
            migrationBuilder.DropTable(name: "stockoutitem");
            migrationBuilder.DropTable(name: "stockout");
            migrationBuilder.DropTable(name: "stockinitem");
            migrationBuilder.DropTable(name: "stockin");
            migrationBuilder.DropTable(name: "stock");
            migrationBuilder.DropTable(name: "vendorbankinfo");
            migrationBuilder.DropTable(name: "vendorcontactinfo");
            migrationBuilder.DropTable(name: "vendor_address");
            migrationBuilder.DropTable(name: "vendor");
        }
    }
}

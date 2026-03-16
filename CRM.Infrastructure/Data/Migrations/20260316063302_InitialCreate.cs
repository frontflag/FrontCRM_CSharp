using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customercontacthistory",
                columns: table => new
                {
                    HistoryId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customercontacthistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "customerinfo",
                columns: table => new
                {
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OfficialName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StandardOfficialName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NickName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Level = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: true),
                    Scale = table.Column<short>(type: "smallint", nullable: true),
                    Background = table.Column<short>(type: "smallint", nullable: true),
                    DealMode = table.Column<short>(type: "smallint", nullable: true),
                    CompanyNature = table.Column<short>(type: "smallint", nullable: true),
                    Country = table.Column<short>(type: "smallint", nullable: true),
                    Industry = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Product = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Product2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Application = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TradeCurrency = table.Column<short>(type: "smallint", nullable: true),
                    TradeType = table.Column<short>(type: "smallint", nullable: true),
                    Payment = table.Column<short>(type: "smallint", nullable: true),
                    ExternalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreditLine = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreditLineRemain = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AscriptionType = table.Column<short>(type: "smallint", nullable: false),
                    ProtectStatus = table.Column<bool>(type: "boolean", nullable: false),
                    ProtectFromUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ProtectTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    BlackList = table.Column<bool>(type: "boolean", nullable: false),
                    DisenableStatus = table.Column<bool>(type: "boolean", nullable: false),
                    DisenableType = table.Column<short>(type: "smallint", nullable: true),
                    CommonSeaAuditStatus = table.Column<short>(type: "smallint", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    CompanyInfo = table.Column<string>(type: "text", nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DUNS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsControl = table.Column<bool>(type: "boolean", nullable: false),
                    CreditCode = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    IdentityType = table.Column<short>(type: "smallint", nullable: true),
                    SalesUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customerinfo", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Salt = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordPlain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    PasswordChangeTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RealName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "customeraddress",
                columns: table => new
                {
                    AddressId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AddressType = table.Column<short>(type: "smallint", nullable: false),
                    Country = table.Column<short>(type: "smallint", nullable: true),
                    Province = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customeraddress", x => x.AddressId);
                    table.ForeignKey(
                        name: "FK_customeraddress_customerinfo_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customerinfo",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customerbankinfo",
                columns: table => new
                {
                    BankId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankBranch = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Currency = table.Column<short>(type: "smallint", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customerbankinfo", x => x.BankId);
                    table.ForeignKey(
                        name: "FK_customerbankinfo_customerinfo_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customerinfo",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customercontactinfo",
                columns: table => new
                {
                    ContactId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Gender = table.Column<short>(type: "smallint", nullable: true),
                    Title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QQ = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    WeChat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customercontactinfo", x => x.ContactId);
                    table.ForeignKey(
                        name: "FK_customercontactinfo_customerinfo_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customerinfo",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customeraddress_CustomerId",
                table: "customeraddress",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customerbankinfo_CustomerId",
                table: "customerbankinfo",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_customercontactinfo_CustomerId",
                table: "customercontactinfo",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customeraddress");

            migrationBuilder.DropTable(
                name: "customerbankinfo");

            migrationBuilder.DropTable(
                name: "customercontacthistory");

            migrationBuilder.DropTable(
                name: "customercontactinfo");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "customerinfo");
        }
    }
}

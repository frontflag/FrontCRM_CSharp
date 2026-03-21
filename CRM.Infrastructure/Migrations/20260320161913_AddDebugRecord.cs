using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDebugRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financepaymentitem");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoiceitem");

            migrationBuilder.DropTable(
                name: "financereceiptitem");

            migrationBuilder.DropTable(
                name: "sellinvoiceitem");

            migrationBuilder.DropTable(
                name: "financepayment");

            migrationBuilder.DropTable(
                name: "financepurchaseinvoice");

            migrationBuilder.DropTable(
                name: "financereceipt");

            migrationBuilder.DropTable(
                name: "financesellinvoice");

            migrationBuilder.DeleteData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WechatAvatarUrl",
                table: "user",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WechatBindTime",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WechatNickname",
                table: "user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WechatOpenId",
                table: "user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WechatUnionId",
                table: "user",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "biz_draft",
                columns: table => new
                {
                    DraftId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DraftName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConvertedEntityId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_biz_draft", x => x.DraftId);
                });

            migrationBuilder.CreateTable(
                name: "debug",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debug", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "sys_department",
                columns: table => new
                {
                    DepartmentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    SaleDataScope = table.Column<short>(type: "smallint", nullable: false),
                    PurchaseDataScope = table.Column<short>(type: "smallint", nullable: false),
                    IdentityType = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_department", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "sys_permission",
                columns: table => new
                {
                    PermissionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PermissionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PermissionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_permission", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "sys_role",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "sys_role_permission",
                columns: table => new
                {
                    RolePermissionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    PermissionId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role_permission", x => x.RolePermissionId);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_department",
                columns: table => new
                {
                    UserDepartmentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    DepartmentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_department", x => x.UserDepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                columns: table => new
                {
                    UserRoleId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    RoleId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => x.UserRoleId);
                });

            migrationBuilder.CreateTable(
                name: "tag_definition",
                columns: table => new
                {
                    TagId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    OwnerUserId = table.Column<long>(type: "bigint", nullable: true),
                    Visibility = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)3),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_definition", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "tag_relation",
                columns: table => new
                {
                    RelationId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    TagId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AppliedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    AppliedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_relation", x => x.RelationId);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite",
                columns: table => new
                {
                    FavoriteId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite", x => x.FavoriteId);
                });

            migrationBuilder.CreateTable(
                name: "user_tag_preference",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    UseCount = table.Column<long>(type: "bigint", nullable: false),
                    LastUsedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tag_preference", x => new { x.UserId, x.TagId });
                });

            migrationBuilder.CreateTable(
                name: "wechat_bind_request",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpenId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wechat_bind_request", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "wechat_login_ticket",
                columns: table => new
                {
                    Ticket = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QrCodeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    OpenId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpireTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wechat_login_ticket", x => x.Ticket);
                });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 1,
                column: "Prefix",
                value: "CUS");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 2,
                column: "Prefix",
                value: "VEN");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ModuleCode", "Prefix" },
                values: new object[] { "RFQ", "RFQ" });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 7,
                column: "Prefix",
                value: "STI");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 8,
                column: "Prefix",
                value: "STO");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ModuleCode", "ModuleName", "Prefix" },
                values: new object[] { "Receipt", "收款", "REC" });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 12,
                column: "Prefix",
                value: "INVI");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 13,
                column: "Prefix",
                value: "INVO");

            migrationBuilder.InsertData(
                table: "sys_serial_number",
                columns: new[] { "Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime" },
                values: new object[] { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Stock", "库存", "STK", null, false, false, 4, null });

            migrationBuilder.CreateIndex(
                name: "IX_biz_draft_CreateTime",
                table: "biz_draft",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_biz_draft_UserId_EntityType_Status",
                table: "biz_draft",
                columns: new[] { "UserId", "EntityType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_department_ParentId",
                table: "sys_department",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_permission_PermissionCode",
                table: "sys_permission",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_RoleCode",
                table: "sys_role",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_permission_RoleId_PermissionId",
                table: "sys_role_permission",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_department_UserId_DepartmentId",
                table: "sys_user_department",
                columns: new[] { "UserId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_UserId_RoleId",
                table: "sys_user_role",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_definition_Code",
                table: "tag_definition",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relation_EntityType_EntityId",
                table: "tag_relation",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_tag_relation_EntityType_TagId",
                table: "tag_relation",
                columns: new[] { "EntityType", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_tag_relation_TagId",
                table: "tag_relation",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_EntityType_EntityId",
                table: "user_favorite",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_favorite_UserId_EntityType_EntityId",
                table: "user_favorite",
                columns: new[] { "UserId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_tag_preference_UserId",
                table: "user_tag_preference",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_tag_preference_UserId_IsFavorite",
                table: "user_tag_preference",
                columns: new[] { "UserId", "IsFavorite" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "biz_draft");

            migrationBuilder.DropTable(
                name: "debug");

            migrationBuilder.DropTable(
                name: "sys_department");

            migrationBuilder.DropTable(
                name: "sys_permission");

            migrationBuilder.DropTable(
                name: "sys_role");

            migrationBuilder.DropTable(
                name: "sys_role_permission");

            migrationBuilder.DropTable(
                name: "sys_user_department");

            migrationBuilder.DropTable(
                name: "sys_user_role");

            migrationBuilder.DropTable(
                name: "tag_definition");

            migrationBuilder.DropTable(
                name: "tag_relation");

            migrationBuilder.DropTable(
                name: "user_favorite");

            migrationBuilder.DropTable(
                name: "user_tag_preference");

            migrationBuilder.DropTable(
                name: "wechat_bind_request");

            migrationBuilder.DropTable(
                name: "wechat_login_ticket");

            migrationBuilder.DeleteData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "user");

            migrationBuilder.DropColumn(
                name: "WechatAvatarUrl",
                table: "user");

            migrationBuilder.DropColumn(
                name: "WechatBindTime",
                table: "user");

            migrationBuilder.DropColumn(
                name: "WechatNickname",
                table: "user");

            migrationBuilder.DropColumn(
                name: "WechatOpenId",
                table: "user");

            migrationBuilder.DropColumn(
                name: "WechatUnionId",
                table: "user");

            migrationBuilder.CreateTable(
                name: "financepayment",
                columns: table => new
                {
                    FinancePaymentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    FinancePaymentCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentAmountToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentCurrency = table.Column<byte>(type: "smallint", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMode = table.Column<short>(type: "smallint", nullable: false),
                    PaymentTotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepayment", x => x.FinancePaymentId);
                });

            migrationBuilder.CreateTable(
                name: "financepurchaseinvoice",
                columns: table => new
                {
                    FinancePurchaseInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ConfirmDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmStatus = table.Column<byte>(type: "smallint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ExcludTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    RedInvoiceStatus = table.Column<short>(type: "smallint", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VendorId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepurchaseinvoice", x => x.FinancePurchaseInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "financereceipt",
                columns: table => new
                {
                    FinanceReceiptId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FinanceReceiptCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseGroupId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ReceiptAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptBankId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ReceiptCurrency = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceiptMode = table.Column<short>(type: "smallint", nullable: false),
                    ReceiptUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SalesUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financereceipt", x => x.FinanceReceiptId);
                });

            migrationBuilder.CreateTable(
                name: "financesellinvoice",
                columns: table => new
                {
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<byte>(type: "smallint", nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceNo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InvoiceStatus = table.Column<short>(type: "smallint", nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MakeInvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReceiveDone = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiveStatus = table.Column<byte>(type: "smallint", nullable: false),
                    ReceiveToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SellInvoiceType = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financesellinvoice", x => x.FinanceSellInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "financepaymentitem",
                columns: table => new
                {
                    FinancePaymentItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    PaymentId = table.Column<string>(type: "character varying(36)", nullable: true),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    FinancePaymentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    PN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentAmountToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PurchaseOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    PurchaseOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    VerificationDone = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VerificationStatus = table.Column<short>(type: "smallint", nullable: false),
                    VerificationToBe = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepaymentitem", x => x.FinancePaymentItemId);
                    table.ForeignKey(
                        name: "FK_financepaymentitem_financepayment_FinancePaymentId",
                        column: x => x.FinancePaymentId,
                        principalTable: "financepayment",
                        principalColumn: "FinancePaymentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_financepaymentitem_financepayment_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "financepayment",
                        principalColumn: "FinancePaymentId");
                });

            migrationBuilder.CreateTable(
                name: "financepurchaseinvoiceitem",
                columns: table => new
                {
                    FinancePurchaseInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinancePurchaseInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BillCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BillQty = table.Column<long>(type: "bigint", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ExcludTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    PurchaseOrderCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    StockInCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    StockInCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    StockInId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financepurchaseinvoiceitem", x => x.FinancePurchaseInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_financepurchaseinvoiceitem_financepurchaseinvoice_FinancePu~",
                        column: x => x.FinancePurchaseInvoiceId,
                        principalTable: "financepurchaseinvoice",
                        principalColumn: "FinancePurchaseInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "financereceiptitem",
                columns: table => new
                {
                    FinanceReceiptItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    ReceiptId = table.Column<string>(type: "character varying(36)", nullable: true),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    FinanceReceiptId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    FinanceSellInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    PN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProductId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ReceiptAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceiptConvertAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellOrderId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SellOrderItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    StockOutItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    VerificationStatus = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financereceiptitem", x => x.FinanceReceiptItemId);
                    table.ForeignKey(
                        name: "FK_financereceiptitem_financereceipt_FinanceReceiptId",
                        column: x => x.FinanceReceiptId,
                        principalTable: "financereceipt",
                        principalColumn: "FinanceReceiptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_financereceiptitem_financereceipt_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "financereceipt",
                        principalColumn: "FinanceReceiptId");
                });

            migrationBuilder.CreateTable(
                name: "sellinvoiceitem",
                columns: table => new
                {
                    SellInvoiceItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    FinanceSellInvoiceId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<byte>(type: "smallint", nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Qty = table.Column<long>(type: "bigint", nullable: false),
                    ReceiveStatus = table.Column<short>(type: "smallint", nullable: false),
                    StockOutItemId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    TaxFreeTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValueAddedTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sellinvoiceitem", x => x.SellInvoiceItemId);
                    table.ForeignKey(
                        name: "FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId",
                        column: x => x.FinanceSellInvoiceId,
                        principalTable: "financesellinvoice",
                        principalColumn: "FinanceSellInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 1,
                column: "Prefix",
                value: "Cus");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 2,
                column: "Prefix",
                value: "Ven");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ModuleCode", "Prefix" },
                values: new object[] { "Inquiry", "INQ" });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 7,
                column: "Prefix",
                value: "SIN");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 8,
                column: "Prefix",
                value: "SOUT");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ModuleCode", "ModuleName", "Prefix" },
                values: new object[] { "Inventory", "库存调整", "INV" });

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 12,
                column: "Prefix",
                value: "VINV");

            migrationBuilder.UpdateData(
                table: "sys_serial_number",
                keyColumn: "Id",
                keyValue: 13,
                column: "Prefix",
                value: "SINV");

            migrationBuilder.InsertData(
                table: "sys_serial_number",
                columns: new[] { "Id", "CreateTime", "CurrentSequence", "LastResetMonth", "LastResetYear", "ModuleCode", "ModuleName", "Prefix", "Remark", "ResetByMonth", "ResetByYear", "SequenceLength", "UpdateTime" },
                values: new object[] { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, null, "Receipt", "收款", "REC", null, false, false, 4, null });

            migrationBuilder.CreateIndex(
                name: "IX_financepayment_FinancePaymentCode",
                table: "financepayment",
                column: "FinancePaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financepaymentitem_FinancePaymentId",
                table: "financepaymentitem",
                column: "FinancePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_financepaymentitem_PaymentId",
                table: "financepaymentitem",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId",
                table: "financepurchaseinvoiceitem",
                column: "FinancePurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_financereceipt_FinanceReceiptCode",
                table: "financereceipt",
                column: "FinanceReceiptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_financereceiptitem_FinanceReceiptId",
                table: "financereceiptitem",
                column: "FinanceReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_financereceiptitem_ReceiptId",
                table: "financereceiptitem",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_sellinvoiceitem_FinanceSellInvoiceId",
                table: "sellinvoiceitem",
                column: "FinanceSellInvoiceId");
        }
    }
}

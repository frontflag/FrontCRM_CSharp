using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售/采购订单主表增加 create_by_user_id、modify_by_user_id（GUID），与 JWT 用户主键一致；基类 CreateUserId(bigint) 无法表示当前用户体系。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411120000_AddSellOrderPurchaseOrderAuditUserIds")]
    public partial class AddSellOrderPurchaseOrderAuditUserIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "sellorder",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "sellorder",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "purchaseorder",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "purchaseorder",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "sellorder");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "sellorder");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "purchaseorder");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "purchaseorder");
        }
    }
}

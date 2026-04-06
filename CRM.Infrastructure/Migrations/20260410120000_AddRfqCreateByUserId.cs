using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 需求主表增加 create_by_user_id（GUID），用于列表「创建人」展示；业务员名由列表接口联表解析。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260410120000_AddRfqCreateByUserId")]
    public partial class AddRfqCreateByUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "rfq",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "rfq");
        }
    }
}

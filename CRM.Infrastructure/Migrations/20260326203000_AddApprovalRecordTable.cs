using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326203000_AddApprovalRecordTable")]
    public partial class AddApprovalRecordTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approval_record",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BizType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    DocumentCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ActionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromStatus = table.Column<short>(type: "smallint", nullable: true),
                    ToStatus = table.Column<short>(type: "smallint", nullable: true),
                    SubmitRemark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AuditRemark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmitterUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SubmitterUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApproverUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    ApproverUserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_record", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_record_ActionTime",
                table: "approval_record",
                column: "ActionTime");

            migrationBuilder.CreateIndex(
                name: "IX_approval_record_BizType_BusinessId",
                table: "approval_record",
                columns: new[] { "BizType", "BusinessId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_record");
        }
    }
}


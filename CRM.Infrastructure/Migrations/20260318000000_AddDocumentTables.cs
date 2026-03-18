using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    public partial class AddDocumentTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_daily_sequence",
                columns: table => new
                {
                    TheDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_document_daily_sequence", x => x.TheDate));

            migrationBuilder.CreateTable(
                name: "upload_document",
                columns: table => new
                {
                    DocumentId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    BizType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BizId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RelativePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ThumbnailRelativePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Remark = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeleteUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    UploadUserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    CreateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifyTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifyUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_upload_document", x => x.DocumentId));

            migrationBuilder.CreateIndex(
                name: "IX_upload_document_BizType_BizId",
                table: "upload_document",
                columns: new[] { "BizType", "BizId" });

            migrationBuilder.CreateIndex(
                name: "IX_upload_document_CreateTime",
                table: "upload_document",
                column: "CreateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "document_daily_sequence");
            migrationBuilder.DropTable(name: "upload_document");
        }
    }
}

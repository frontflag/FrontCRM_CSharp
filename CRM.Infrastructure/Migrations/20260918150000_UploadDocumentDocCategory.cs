using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 上传文档增加业务分类：出货照片 / 出货签收单 / 其他。历史空值归 OTHER。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260918150000_UploadDocumentDocCategory")]
public partial class UploadDocumentDocCategory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.upload_document
              ADD COLUMN IF NOT EXISTS "DocCategory" character varying(32) NOT NULL DEFAULT 'OTHER';

            UPDATE public.upload_document
               SET "DocCategory" = 'OTHER'
             WHERE "DocCategory" IS NULL OR btrim("DocCategory") = '';

            COMMENT ON COLUMN public.upload_document."DocCategory" IS '附件分类：SHIP_PHOTO 出货照片 / POD 出货签收单 / OTHER 其他';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.upload_document DROP COLUMN IF EXISTS "DocCategory";
            """);
    }
}

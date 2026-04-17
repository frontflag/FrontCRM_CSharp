using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关主单软删除：<c>IsDeleted</c>、<c>DeletedAt</c>、<c>deleted_by_user_id</c>；唯一索引改为仅未删除行。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260619120000_CustomsDeclarationSoftDelete")]
    public partial class CustomsDeclarationSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""deleted_by_user_id"" character varying(36) NULL;

DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"")
  WHERE NOT ""IsDeleted"";

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"")
  WHERE NOT ""IsDeleted"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";

UPDATE public.customs_declaration SET ""IsDeleted"" = false, ""DeletedAt"" = NULL, ""deleted_by_user_id"" = NULL WHERE ""IsDeleted"" = true;

ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""deleted_by_user_id"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""DeletedAt"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""IsDeleted"";

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"");
");
        }
        // Note: Down may fail if duplicate DeclarationCode/StockOutRequestId exist among non-deleted rows after re-adding strict unique.
    }
}

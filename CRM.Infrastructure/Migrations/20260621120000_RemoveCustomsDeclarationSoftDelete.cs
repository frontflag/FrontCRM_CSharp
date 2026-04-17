using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 回滚报关主单软删除：去掉 <c>IsDeleted</c> 等列，恢复全局唯一索引（与 <c>20260619120000_CustomsDeclarationSoftDelete</c> 相反）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260621120000_RemoveCustomsDeclarationSoftDelete")]
    public partial class RemoveCustomsDeclarationSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";

ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""deleted_by_user_id"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""DeletedAt"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""IsDeleted"";

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"");

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";

ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
ALTER TABLE IF EXISTS public.customs_declaration
  ADD COLUMN IF NOT EXISTS ""deleted_by_user_id"" character varying(36) NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"")
  WHERE NOT ""IsDeleted"";

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"")
  WHERE NOT ""IsDeleted"";
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关公司软删除：<c>IsDeleted</c>、<c>DeletedAt</c>、<c>deleted_by_user_id</c>。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620120000_CustomsBrokerSoftDelete")]
    public partial class CustomsBrokerSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_broker
  ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.customs_broker
  ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
ALTER TABLE IF EXISTS public.customs_broker
  ADD COLUMN IF NOT EXISTS ""deleted_by_user_id"" character varying(36) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_broker DROP COLUMN IF EXISTS ""deleted_by_user_id"";
ALTER TABLE IF EXISTS public.customs_broker DROP COLUMN IF EXISTS ""DeletedAt"";
ALTER TABLE IF EXISTS public.customs_broker DROP COLUMN IF EXISTS ""IsDeleted"";
");
        }
    }
}

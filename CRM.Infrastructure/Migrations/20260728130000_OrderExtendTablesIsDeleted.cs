using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>SO/PO extend 四表增加 is_deleted，业务改为软删除 extend 行。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260728130000_OrderExtendTablesIsDeleted")]
    public partial class OrderExtendTablesIsDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorderextend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.sellorderitemextend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.purchaseorderextend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.purchaseorderitemextend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                COMMENT ON COLUMN public.sellorderextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 sellorder 整单删除同步';
                COMMENT ON COLUMN public.sellorderitemextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 sellorderitem 明细删除同步';
                COMMENT ON COLUMN public.purchaseorderextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 purchaseorder 整单删除同步';
                COMMENT ON COLUMN public.purchaseorderitemextend.is_deleted IS '软删除标记：true 表示逻辑删除，与 purchaseorderitem 明细删除同步';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorderextend DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.purchaseorderextend DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS is_deleted;
                """);
        }
    }
}

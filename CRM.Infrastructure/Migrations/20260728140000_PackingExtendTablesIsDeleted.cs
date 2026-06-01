using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>装箱 extend 四表增加 is_deleted，删除装箱单时软删 extend 行。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260728140000_PackingExtendTablesIsDeleted")]
    public partial class PackingExtendTablesIsDeleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.packing_extend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.packing_extend_box
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.packing_extend_ship
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
                ALTER TABLE IF EXISTS public.packing_item_extend
                  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

                COMMENT ON COLUMN public.packing_extend.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
                COMMENT ON COLUMN public.packing_extend_box.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
                COMMENT ON COLUMN public.packing_extend_ship.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing 整单删除同步';
                COMMENT ON COLUMN public.packing_item_extend.is_deleted IS '软删除标记：true 表示逻辑删除，与 packing_item 明细删除同步';

                DROP INDEX IF EXISTS public."IX_packing_extend_box_PackingId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_box_PackingId"
                  ON public.packing_extend_box ("PackingId")
                  WHERE is_deleted = false;

                DROP INDEX IF EXISTS public."IX_packing_extend_ship_PackingId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_ship_PackingId"
                  ON public.packing_extend_ship ("PackingId")
                  WHERE is_deleted = false;

                DROP INDEX IF EXISTS public."IX_packing_item_extend_PackingItemId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_item_extend_PackingItemId"
                  ON public.packing_item_extend ("PackingItemId")
                  WHERE is_deleted = false;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS public."IX_packing_item_extend_PackingItemId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_item_extend_PackingItemId"
                  ON public.packing_item_extend ("PackingItemId");

                DROP INDEX IF EXISTS public."IX_packing_extend_ship_PackingId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_ship_PackingId"
                  ON public.packing_extend_ship ("PackingId");

                DROP INDEX IF EXISTS public."IX_packing_extend_box_PackingId";
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_packing_extend_box_PackingId"
                  ON public.packing_extend_box ("PackingId");

                ALTER TABLE IF EXISTS public.packing_item_extend DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.packing_extend_ship DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.packing_extend_box DROP COLUMN IF EXISTS is_deleted;
                ALTER TABLE IF EXISTS public.packing_extend DROP COLUMN IF EXISTS is_deleted;
                """);
        }
    }
}

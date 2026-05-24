using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>装箱单扩展表 packing_extend 重命名为 packing_extend_box。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518130000_RenamePackingExtendToPackingExtendBox")]
    public partial class RenamePackingExtendToPackingExtendBox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'packing_extend'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'packing_extend_box'
    ) THEN
        ALTER TABLE public.packing_extend RENAME TO packing_extend_box;
    END IF;
END $$;

ALTER INDEX IF EXISTS ""IX_packing_extend_PackingId"" RENAME TO ""IX_packing_extend_box_PackingId"";
ALTER TABLE IF EXISTS public.packing_extend_box RENAME CONSTRAINT ""PK_packing_extend"" TO ""PK_packing_extend_box"";
ALTER TABLE IF EXISTS public.packing_extend_box RENAME CONSTRAINT ""FK_packing_extend_packing_PackingId"" TO ""FK_packing_extend_box_packing_PackingId"";

COMMENT ON TABLE public.packing_extend_box IS '装箱单箱规扩展：净重/毛重/尺寸/箱数';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'packing_extend_box'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'packing_extend'
    ) THEN
        ALTER TABLE public.packing_extend_box RENAME TO packing_extend;
    END IF;
END $$;

ALTER INDEX IF EXISTS ""IX_packing_extend_box_PackingId"" RENAME TO ""IX_packing_extend_PackingId"";
ALTER TABLE IF EXISTS public.packing_extend RENAME CONSTRAINT ""PK_packing_extend_box"" TO ""PK_packing_extend"";
ALTER TABLE IF EXISTS public.packing_extend RENAME CONSTRAINT ""FK_packing_extend_box_packing_PackingId"" TO ""FK_packing_extend_packing_PackingId"";
");
        }
    }
}

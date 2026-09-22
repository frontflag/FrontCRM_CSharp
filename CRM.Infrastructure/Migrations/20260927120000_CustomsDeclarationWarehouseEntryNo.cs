using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关主表增加报关入仓号。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260927120000_CustomsDeclarationWarehouseEntryNo")]
    public partial class CustomsDeclarationWarehouseEntryNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration
                    ADD COLUMN IF NOT EXISTS warehouse_entry_no character varying(64) NULL;

                COMMENT ON COLUMN public.customs_declaration.warehouse_entry_no IS '报关入仓号';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration
                    DROP COLUMN IF EXISTS warehouse_entry_no;
                """);
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// warehouseinfo：启用状态 Status（1=启用 0=停用）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260535100000_WarehouseInfoStatusEnabled")]
    public partial class WarehouseInfoStatusEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.warehouseinfo ADD COLUMN IF NOT EXISTS ""Status"" smallint NOT NULL DEFAULT 1;
COMMENT ON COLUMN public.warehouseinfo.""Status"" IS '启用状态：1=启用 0=停用';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"COMMENT ON COLUMN public.warehouseinfo.""Status"" IS NULL;");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// warehouseinfo：地域类型 RegionType（10=境内 20=境外）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260422150000_AddWarehouseInfoRegionType")]
    public partial class AddWarehouseInfoRegionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.warehouseinfo ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;
COMMENT ON COLUMN public.warehouseinfo.""RegionType"" IS '地域类型 RegionType：10=境内 20=境外';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.warehouseinfo DROP COLUMN IF EXISTS ""RegionType"";");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockinnotify：地域类型 RegionType（10=境内 20=境外），与 warehouseinfo 共用语义。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260423120000_AddStockInNotifyRegionType")]
    public partial class AddStockInNotifyRegionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockinnotify ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;
COMMENT ON COLUMN public.stockinnotify.""RegionType"" IS '地域类型 RegionType：10=境内 20=境外（与仓库档案共用）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.stockinnotify DROP COLUMN IF EXISTS ""RegionType"";");
        }
    }
}

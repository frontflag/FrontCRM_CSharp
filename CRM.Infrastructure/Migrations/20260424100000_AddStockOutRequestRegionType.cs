using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockoutrequest：地域类型 RegionType（10=境内 20=境外），与 warehouseinfo、stockinnotify 共用语义。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424100000_AddStockOutRequestRegionType")]
    public partial class AddStockOutRequestRegionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutrequest ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;
COMMENT ON COLUMN public.stockoutrequest.""RegionType"" IS '地域类型 RegionType：10=境内 20=境外（与仓库、到货通知共用）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS ""RegionType"";");
        }
    }
}

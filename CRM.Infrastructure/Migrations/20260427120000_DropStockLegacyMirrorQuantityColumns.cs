using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 移除 <c>stock</c> 表与 <see cref="CRM.Core.Models.Inventory.StockInfo"/> 中已废弃的镜像字段
    /// <c>Quantity</c> / <c>AvailableQuantity</c> / <c>LockedQuantity</c>；以 <c>Qty*</c> 为准。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260427120000_DropStockLegacyMirrorQuantityColumns")]
    public partial class DropStockLegacyMirrorQuantityColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""Quantity"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""AvailableQuantity"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""LockedQuantity"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""Quantity"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""AvailableQuantity"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""LockedQuantity"" numeric(18,4) NOT NULL DEFAULT 0;
");
        }
    }
}

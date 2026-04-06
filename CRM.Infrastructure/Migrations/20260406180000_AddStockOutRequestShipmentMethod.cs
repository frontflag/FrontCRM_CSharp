using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 出库通知「出货方式」：存数据字典 LogisticsArrivalMethod 的 ItemCode（与物流来货方式同源）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260406180000_AddStockOutRequestShipmentMethod")]
    public partial class AddStockOutRequestShipmentMethod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS ""ShipmentMethod"" character varying(64) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS ""ShipmentMethod"";
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>出库单主表：出货方式、快递单号（可编辑、与文档关联 biz STOCK_OUT）</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260406210000_AddStockOutShipmentCourier")]
    public partial class AddStockOutShipmentCourier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout
  ADD COLUMN IF NOT EXISTS ""ShipmentMethod"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockout
  ADD COLUMN IF NOT EXISTS ""CourierTrackingNo"" character varying(128) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""CourierTrackingNo"";
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""ShipmentMethod"";
");
        }
    }
}

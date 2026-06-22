using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 到货通知：预计到货方式、预计到货快递单号（与出库通知出货方式共用 LogisticsArrivalMethod 字典）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260615120000_StockInNotifyShipmentCourier")]
    public partial class StockInNotifyShipmentCourier : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin_notify
  ADD COLUMN IF NOT EXISTS ""ShipmentMethod"" character varying(64) NULL;
COMMENT ON COLUMN public.stockin_notify.""ShipmentMethod"" IS '预计到货方式：数据字典 LogisticsArrivalMethod 的 ItemCode（与出库通知出货方式同源，存编码非展示名）';

ALTER TABLE IF EXISTS public.stockin_notify
  ADD COLUMN IF NOT EXISTS ""CourierTrackingNo"" character varying(128) NULL;
COMMENT ON COLUMN public.stockin_notify.""CourierTrackingNo"" IS '预计到货快递单号';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS ""ShipmentMethod"";
ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS ""CourierTrackingNo"";
");
        }
    }
}

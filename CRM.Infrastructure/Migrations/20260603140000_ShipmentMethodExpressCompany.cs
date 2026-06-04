using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>出库通知/装箱扩展：快递公司；装箱扩展：出货方式（替代 delivery_method）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603140000_ShipmentMethodExpressCompany")]
    public partial class ShipmentMethodExpressCompany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout_notify
  ADD COLUMN IF NOT EXISTS ""ExpressCompany"" character varying(64) NULL;

COMMENT ON COLUMN public.stockout_notify.""ExpressCompany"" IS '快递公司：数据字典 LogisticsExpressMethod 的 ItemCode；出货方式为快递时可填';

ALTER TABLE IF EXISTS public.packing_extend_ship
  ADD COLUMN IF NOT EXISTS shipment_method character varying(64) NULL,
  ADD COLUMN IF NOT EXISTS express_company character varying(64) NULL;

UPDATE public.packing_extend_ship
SET shipment_method = '1'
WHERE shipment_method IS NULL AND delivery_method = 10;

UPDATE public.packing_extend_ship
SET shipment_method = '2'
WHERE shipment_method IS NULL AND delivery_method = 20;

COMMENT ON COLUMN public.packing_extend_ship.shipment_method IS '出货方式：LogisticsArrivalMethod ItemCode（1送货 2自提 3快递）';
COMMENT ON COLUMN public.packing_extend_ship.express_company IS '快递公司：LogisticsExpressMethod ItemCode';

UPDATE public.sys_dict_item
SET ""IsActive"" = false
WHERE ""Category"" = 'LogisticsArrivalMethod' AND ""ItemCode"" = '4';

INSERT INTO public.sys_dict_item (""Id"", ""Category"", ""ItemCode"", ""NameZh"", ""NameEn"", ""SortOrder"", ""IsActive"", ""CreateTime"")
SELECT gen_random_uuid()::text, 'LogisticsExpressMethod', '6', '货拉拉', 'Huolala', 6, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (
  SELECT 1 FROM public.sys_dict_item d
  WHERE d.""Category"" = 'LogisticsExpressMethod' AND d.""ItemCode"" = '6'
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.sys_dict_item SET ""IsActive"" = true
WHERE ""Category"" = 'LogisticsArrivalMethod' AND ""ItemCode"" = '4';

DELETE FROM public.sys_dict_item
WHERE ""Category"" = 'LogisticsExpressMethod' AND ""ItemCode"" = '6';

ALTER TABLE IF EXISTS public.packing_extend_ship
  DROP COLUMN IF EXISTS express_company,
  DROP COLUMN IF EXISTS shipment_method;

ALTER TABLE IF EXISTS public.stockout_notify
  DROP COLUMN IF EXISTS ""ExpressCompany"";
");
        }
    }
}

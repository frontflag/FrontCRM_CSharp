using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>出库通知：报关状态 CustomsStatus。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260603150000_StockOutNotifyCustomsStatus")]
    public partial class StockOutNotifyCustomsStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout_notify
  ADD COLUMN IF NOT EXISTS ""CustomsStatus"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.stockout_notify.""CustomsStatus"" IS '报关状态：0=未知 10=无需报关 20=待报关 30=报关中 100=报关完成';

-- 待报关：存在 Open 待报关记录且尚未生成报关出库通知
UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 20
FROM public.customs_pendlist p
WHERE p.sales_stockout_notify_id = sor.""ID""
  AND p.is_deleted = false
  AND p.status = 1
  AND (p.customs_stockout_notify_id IS NULL OR btrim(p.customs_stockout_notify_id) = '');

-- 报关完成
UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 100
FROM public.customs_pendlist p
WHERE p.sales_stockout_notify_id = sor.""ID""
  AND p.is_deleted = false
  AND p.status = 10;

UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 100
FROM public.customs_pendlist p
WHERE p.customs_stockout_notify_id = sor.""ID""
  AND p.is_deleted = false
  AND p.status = 10;

-- 报关中
UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 30
FROM public.customs_pendlist p
WHERE p.sales_stockout_notify_id = sor.""ID""
  AND p.is_deleted = false
  AND p.customs_stockout_notify_id IS NOT NULL
  AND btrim(p.customs_stockout_notify_id) <> ''
  AND p.status NOT IN (10, -1)
  AND sor.""CustomsStatus"" = 0;

UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 30
FROM public.customs_pendlist p
WHERE p.customs_stockout_notify_id = sor.""ID""
  AND p.is_deleted = false
  AND p.status NOT IN (10, -1)
  AND sor.""CustomsStatus"" = 0;

-- 无需报关：已生成销售装箱且从未进入待报关
UPDATE public.stockout_notify sor
SET ""CustomsStatus"" = 10
WHERE sor.""CustomsStatus"" = 0
  AND sor.""StockOutType"" = 10
  AND sor.""Status"" IN (20, 100)
  AND NOT EXISTS (
    SELECT 1 FROM public.customs_pendlist p
    WHERE p.sales_stockout_notify_id = sor.""ID"" AND p.is_deleted = false
  );
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout_notify DROP COLUMN IF EXISTS ""CustomsStatus"";
");
        }
    }
}

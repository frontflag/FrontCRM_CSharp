using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// <c>stockin.RegionType</c> 冗余到货通知；<c>stock.RegionType</c> 冗余入库单；<c>stockout.RegionType</c> 冗余库存行。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260408120000_AddRegionTypeToStockinStockStockout")]
    public partial class AddRegionTypeToStockinStockStockout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;
ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;
ALTER TABLE IF EXISTS public.stockout ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;

UPDATE public.stockin s
SET ""RegionType"" = n.""RegionType""
FROM public.stockinnotify n
WHERE s.""SourceId"" IS NOT NULL AND BTRIM(s.""SourceId"") <> ''
  AND n.""UserId"" = BTRIM(s.""SourceId"");

DO $LM$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockledger') THEN
    UPDATE public.stock st
    SET ""RegionType"" = sub.rt
    FROM (
      SELECT DISTINCT ON (stx.""StockId"")
        stx.""StockId"",
        si2.""RegionType"" AS rt
      FROM public.stock stx
      INNER JOIN public.stockledger l ON l.""BizType"" = 'STOCK_IN' AND l.""QtyIn"" > 0
        AND l.""MaterialId"" = stx.""MaterialId"" AND l.""WarehouseId"" = stx.""WarehouseId""
      INNER JOIN public.stockin si2 ON si2.""StockInId"" = l.""BizId""
      ORDER BY stx.""StockId"", l.""CreateTime"" DESC NULLS LAST
    ) sub
    WHERE st.""StockId"" = sub.""StockId"";
  ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'inventoryledger') THEN
    UPDATE public.stock st
    SET ""RegionType"" = sub.rt
    FROM (
      SELECT DISTINCT ON (stx.""StockId"")
        stx.""StockId"",
        si2.""RegionType"" AS rt
      FROM public.stock stx
      INNER JOIN public.inventoryledger l ON l.""BizType"" = 'STOCK_IN' AND l.""QtyIn"" > 0
        AND l.""MaterialId"" = stx.""MaterialId"" AND l.""WarehouseId"" = stx.""WarehouseId""
      INNER JOIN public.stockin si2 ON si2.""StockInId"" = l.""BizId""
      ORDER BY stx.""StockId"", l.""CreateTime"" DESC NULLS LAST
    ) sub
    WHERE st.""StockId"" = sub.""StockId"";
  END IF;
END $LM$;

UPDATE public.stockout so
SET ""RegionType"" = sub.rt
FROM (
  SELECT DISTINCT ON (i.""StockOutId"")
    i.""StockOutId"",
    st.""RegionType"" AS rt
  FROM public.stockoutitem i
  INNER JOIN public.stock st ON st.""StockId"" = i.""StockId""
  ORDER BY i.""StockOutId"", i.""CreateTime"" NULLS LAST, i.""ItemId""
) sub
WHERE so.""StockOutId"" = sub.""StockOutId"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""RegionType"";
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS ""RegionType"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""RegionType"";
");
        }
    }
}

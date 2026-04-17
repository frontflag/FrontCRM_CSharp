using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260625100000_StockInExtendAndStockInItemCode")]
    public partial class StockInExtendAndStockInItemCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockinextend (
  ""StockInId"" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_stockinextend"" PRIMARY KEY (""StockInId""),
  CONSTRAINT ""FK_stockinextend_stockin"" FOREIGN KEY (""StockInId"")
    REFERENCES public.stockin (""StockInId"") ON DELETE CASCADE
);

ALTER TABLE IF EXISTS public.stockinitem
    ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

INSERT INTO public.stockinextend (""StockInId"", last_item_line_seq, ""CreateTime"")
SELECT sin.""StockInId"", 0, NOW()
FROM public.stockin sin
WHERE NOT EXISTS (
  SELECT 1 FROM public.stockinextend x WHERE x.""StockInId"" = sin.""StockInId"");

WITH numbered AS (
  SELECT si.""ItemId"",
    sin.""StockInCode"",
    ROW_NUMBER() OVER (PARTITION BY si.""StockInId"" ORDER BY si.""CreateTime"", si.""ItemId"") AS rn
  FROM public.stockinitem si
  INNER JOIN public.stockin sin ON sin.""StockInId"" = si.""StockInId""
)
UPDATE public.stockinitem u
SET stock_in_item_code = n.""StockInCode"" || '-' || n.rn::text
FROM numbered n
WHERE u.""ItemId"" = n.""ItemId"";

UPDATE public.stockinextend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.stockinitem si WHERE si.""StockInId"" = e.""StockInId""), 0);

UPDATE public.stockinitem u
SET stock_in_item_code = 'SI-LEGACY-' || replace(u.""ItemId""::text, '-', '')
WHERE u.stock_in_item_code IS NULL;

ALTER TABLE public.stockinitem ALTER COLUMN stock_in_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockinitem_stockin_linecode""
  ON public.stockinitem (""StockInId"", stock_in_item_code);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_stockinitem_stockin_linecode"";
ALTER TABLE IF EXISTS public.stockinitem DROP COLUMN IF EXISTS stock_in_item_code;
DROP TABLE IF EXISTS public.stockinextend;
");
        }
    }
}

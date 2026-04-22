using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 库存分桶扩展表 <c>stock_extend</c>（<c>last_item_line_seq</c>）与在库明细 <c>stock_item.stock_item_code</c>；
    /// 编号规则同入库明细：<c>{StockCode}-{行序号}</c>；无 <c>StockCode</c> 时用 <c>SKI-LEGACY-</c> 前缀兜底。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260703110000_StockExtendAndStockItemCode")]
    public partial class StockExtendAndStockItemCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stock_extend (
  ""StockId"" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_stock_extend"" PRIMARY KEY (""StockId""),
  CONSTRAINT ""FK_stock_extend_stock"" FOREIGN KEY (""StockId"")
    REFERENCES public.stock (""StockId"") ON DELETE CASCADE
);

INSERT INTO public.stock_extend (""StockId"", last_item_line_seq, ""CreateTime"")
SELECT DISTINCT si.""StockAggregateId"", 0, NOW()
FROM public.stock_item si
WHERE NOT EXISTS (SELECT 1 FROM public.stock_extend x WHERE x.""StockId"" = si.""StockAggregateId"");

ALTER TABLE public.stock_item ADD COLUMN IF NOT EXISTS stock_item_code character varying(64) NULL;

WITH numbered AS (
  SELECT si.""StockItemId"",
         NULLIF(trim(st.""StockCode""), '') AS sc,
         ROW_NUMBER() OVER (PARTITION BY si.""StockAggregateId"" ORDER BY si.""CreateTime"", si.""StockItemId"") AS rn
  FROM public.stock_item si
  INNER JOIN public.stock st ON st.""StockId"" = si.""StockAggregateId""
)
UPDATE public.stock_item u
SET stock_item_code = CASE
  WHEN n.sc IS NOT NULL THEN n.sc || '-' || n.rn::text
  ELSE 'SKI-LEGACY-' || replace(u.""StockItemId""::text, '-', '')
END
FROM numbered n
WHERE u.""StockItemId"" = n.""StockItemId"";

UPDATE public.stock_item u
SET stock_item_code = 'SKI-LEGACY-' || replace(u.""StockItemId""::text, '-', '')
WHERE u.stock_item_code IS NULL;

UPDATE public.stock_extend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.stock_item si WHERE si.""StockAggregateId"" = e.""StockId""), 0);

ALTER TABLE public.stock_item ALTER COLUMN stock_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_item_agg_linecode""
  ON public.stock_item (""StockAggregateId"", stock_item_code);

COMMENT ON TABLE public.stock_extend IS '库存分桶扩展：在库明细 stock_item_code 行序号水位';
COMMENT ON COLUMN public.stock_item.stock_item_code IS '在库明细业务编号：{StockCode}-{行序号}（规则同 stock_in_item_code）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_stock_item_agg_linecode"";
ALTER TABLE IF EXISTS public.stock_item DROP COLUMN IF EXISTS stock_item_code;
DROP TABLE IF EXISTS public.stock_extend;
");
        }
    }
}

-- =============================================================================
-- stockinitem：与当前代码库（较旧 18 列结构）比对后需补加的列及依赖
-- 列名与迁移 20260624120000、20260625100000 一致；主键/外键列假定已为 ""ItemId""、""StockInId""（PascalCase）
-- 若库中列名全小写，请将下文 ""ItemId"" 等改为 itemid / stockinid 等实际名称后执行
-- =============================================================================

-- 1) 采购 PN/品牌快照 + 采购币别（采销行主键/业务编号在 stockin 与 stockinitemextend，不在 stockinitem）
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_pn character varying(200) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS purchase_brand character varying(200) NULL;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS currency smallint NULL;

COMMENT ON COLUMN public.stock_in_item.purchase_pn IS '采购明细 PN 快照';
COMMENT ON COLUMN public.stock_in_item.purchase_brand IS '采购明细品牌快照';
COMMENT ON COLUMN public.stock_in_item.currency IS '采购币别（与 purchaseorderitem.currency 一致）';

-- 2) 入库明细业务编号 + 主单水位表（与 20260625100000 一致）
CREATE TABLE IF NOT EXISTS public.stockinextend (
  ""StockInId"" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_stockinextend"" PRIMARY KEY (""StockInId""),
  CONSTRAINT ""FK_stockinextend_stockin"" FOREIGN KEY (""StockInId"")
    REFERENCES public.stock_in (""StockInId"") ON DELETE CASCADE
);

ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

INSERT INTO public.stockinextend (""StockInId"", last_item_line_seq, ""CreateTime"")
SELECT sin.""StockInId"", 0, NOW()
FROM public.stock_in sin
WHERE NOT EXISTS (
  SELECT 1 FROM public.stockinextend x WHERE x.""StockInId"" = sin.""StockInId"");

WITH numbered AS (
  SELECT si.""ItemId"",
    sin.""StockInCode"",
    ROW_NUMBER() OVER (PARTITION BY si.""StockInId"" ORDER BY si.""CreateTime"", si.""ItemId"") AS rn
  FROM public.stock_in_item si
  INNER JOIN public.stock_in sin ON sin.""StockInId"" = si.""StockInId""
)
UPDATE public.stock_in_item u
SET stock_in_item_code = n.""StockInCode"" || '-' || n.rn::text
FROM numbered n
WHERE u.""ItemId"" = n.""ItemId"";

UPDATE public.stockinextend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.stock_in_item si WHERE si.""StockInId"" = e.""StockInId""), 0);

UPDATE public.stock_in_item u
SET stock_in_item_code = 'SI-LEGACY-' || replace(u.""ItemId""::text, '-', '')
WHERE u.stock_in_item_code IS NULL;

ALTER TABLE public.stock_in_item ALTER COLUMN stock_in_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockinitem_stockin_linecode""
  ON public.stock_in_item (""StockInId"", stock_in_item_code);

-- 3) 可选：按 MaterialId = 采购明细主键 回填快照与币别（无匹配则保持 NULL）
--    要求 purchaseorderitem 已存在列 purchase_order_item_code（见迁移 20260409120000）
UPDATE public.stock_in_item si
SET
  purchase_pn = pi.pn,
  purchase_brand = pi.brand,
  currency = pi.currency
FROM public.purchaseorderitem pi
WHERE si.""MaterialId"" = pi.""PurchaseOrderItemId"";


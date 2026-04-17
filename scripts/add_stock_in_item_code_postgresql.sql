-- =============================================================================
-- public.stock_in_item：补加 stock_in_item_code（与 EF StockInItem.StockInItemCode、
-- 迁移 20260625100000_StockInExtendAndStockInItemCode 一致；表名已为新名时执行本脚本）
-- 若库仍为旧表名 stockinitem，请改用迁移中的 public.stockinitem 版本或先执行表重命名。
-- =============================================================================

ALTER TABLE IF EXISTS public.stock_in_item
    ADD COLUMN IF NOT EXISTS stock_in_item_code character varying(64) NULL;

-- 仅空白视为未填，便于重新生成
UPDATE public.stock_in_item
SET stock_in_item_code = NULL
WHERE stock_in_item_code IS NOT NULL AND length(trim(stock_in_item_code)) = 0;

-- 按入库单号 + 行序生成业务编号：{StockInCode}-{1,2,...}
WITH numbered AS (
    SELECT si."ItemId",
           sin."StockInCode",
           ROW_NUMBER() OVER (PARTITION BY si."StockInId" ORDER BY si."CreateTime", si."ItemId") AS rn
    FROM public.stock_in_item si
    INNER JOIN public.stock_in sin ON sin."StockInId" = si."StockInId"
)
UPDATE public.stock_in_item u
SET stock_in_item_code = n."StockInCode" || '-' || n.rn::text
FROM numbered n
WHERE u."ItemId" = n."ItemId";

-- 无法关联头表、或仍未写入时的兜底（保证每行唯一，便于 NOT NULL + 唯一索引）
UPDATE public.stock_in_item u
SET stock_in_item_code = 'SI-LEGACY-' || replace(u."ItemId"::text, '-', '')
WHERE u.stock_in_item_code IS NULL;

ALTER TABLE public.stock_in_item ALTER COLUMN stock_in_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_stockinitem_stockin_linecode"
    ON public.stock_in_item ("StockInId", stock_in_item_code);

COMMENT ON COLUMN public.stock_in_item.stock_in_item_code IS '入库明细业务编号：{StockInCode}-{行序号}';

-- 若使用入库扩展表序号（stockinextend），可与迁移一致同步水位（可选）
-- UPDATE public.stockinextend e
-- SET last_item_line_seq = COALESCE((
--   SELECT COUNT(*)::integer FROM public.stock_in_item si WHERE si."StockInId" = e."StockInId"), 0);

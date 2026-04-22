-- =============================================================================
-- 为 public.stock_extend 补加缺失记录（PostgreSQL）
-- 说明：stock_extend 与 public.stock 分桶 1:1，用于 last_item_line_seq 水位（在库明细行序号）。
-- 执行前请备份。
-- =============================================================================

BEGIN;

-- 方式 A（推荐）：凡 public.stock 中存在、但 stock_extend 中还没有的分桶，一律插入一行（含尚无 stock_item 的空分桶）
INSERT INTO public.stock_extend ("StockId", last_item_line_seq, "CreateTime")
SELECT s."StockId", 0, NOW()
FROM public.stock s
WHERE NOT EXISTS (
  SELECT 1 FROM public.stock_extend x WHERE x."StockId" = s."StockId"
);

-- 方式 B（与迁移 20260703110000 一致）：仅根据已有在库明细所在分桶补行（若某分桶仅有 stock 无明细，需配合方式 A）
-- INSERT INTO public.stock_extend ("StockId", last_item_line_seq, "CreateTime")
-- SELECT DISTINCT si."StockAggregateId", 0, NOW()
-- FROM public.stock_item si
-- WHERE NOT EXISTS (
--   SELECT 1 FROM public.stock_extend x WHERE x."StockId" = si."StockAggregateId"
-- );

-- 将水位同步为「当前分桶下在库明细行数」（与迁移中 UPDATE 一致）
UPDATE public.stock_extend e
SET
  last_item_line_seq = COALESCE((
    SELECT COUNT(*)::integer
    FROM public.stock_item si
    WHERE si."StockAggregateId" = e."StockId"
  ), 0),
  "ModifyTime" = NOW()
WHERE TRUE;

COMMIT;

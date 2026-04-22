-- =============================================================================
-- 为已有 stock_in / stock_in_item 数据补齐 stock_in_extend（表已存在且可为空）
-- - 每张入库单一行扩展；last_item_line_seq = 该单下明细行数（与迁移 20260625100000 回填逻辑一致）
-- - CreateTime / ModifyTime 与 stockin 主表一致
-- - 已存在扩展行的 StockInId 会跳过（仅插入缺失）；若需重算水位可改用下方「仅更新」段
-- =============================================================================

INSERT INTO public.stock_in_extend (""StockInId"", last_item_line_seq, ""CreateTime"", ""ModifyTime"")
SELECT
  sin.""StockInId"",
  COALESCE((
    SELECT COUNT(*)::integer
    FROM public.stock_in_item si
    WHERE si.""StockInId"" = sin.""StockInId""), 0),
  sin.""CreateTime"",
  sin.""ModifyTime""
FROM public.stock_in sin
WHERE NOT EXISTS (
  SELECT 1 FROM public.stock_in_extend e WHERE e.""StockInId"" = sin.""StockInId"");

-- 可选：扩展行已存在但需按当前明细行数重算 last_item_line_seq，并同步时间
-- UPDATE public.stock_in_extend e
-- SET
--   last_item_line_seq = COALESCE((
--     SELECT COUNT(*)::integer FROM public.stock_in_item si WHERE si.""StockInId"" = e.""StockInId""), 0),
--   ""CreateTime"" = s.""CreateTime"",
--   ""ModifyTime"" = s.""ModifyTime""
-- FROM public.stock_in s
-- WHERE s.""StockInId"" = e.""StockInId"";


-- =============================================================================
-- 为已有 stockin / stockinitem 数据补齐 stockinitemextend（表已存在且可为空）
-- - 每行明细一行扩展：StockInItemId = ItemId；StockInId 与明细一致
-- - sell_order_* / purchase_order_* 四列取自 stockin 主表（与头表冗余一致）
-- - CreateTime 与入库主表 stockin 一致（ModifyTime / CreateUserId / ModifyUserId 一并从主表带出）
-- - 若 stockin 上采销列为 PascalCase（""SellOrderItemId"" 等），请将 sin.sell_order_* 改为实际列名
-- - 若 stockin.""CreateUserId"" 为 varchar 等非 bigint，请将最后两列改为 NULL::bigint 或自行 CAST
-- =============================================================================

INSERT INTO public.stock_in_item_extend (
    ""StockInItemId"",
    ""StockInId"",
    ""sell_order_item_id"",
    ""sell_order_item_code"",
    ""purchase_order_item_id"",
    ""purchase_order_item_code"",
    ""CreateTime"",
    ""CreateUserId"",
    ""ModifyTime"",
    ""ModifyUserId""
)
SELECT
    si.""ItemId"",
    si.""StockInId"",
    sin.sell_order_item_id,
    sin.sell_order_item_code,
    sin.purchase_order_item_id,
    sin.purchase_order_item_code,
    sin.""CreateTime"",
    sin.""CreateUserId"",
    sin.""ModifyTime"",
    sin.""ModifyUserId""
FROM public.stock_in_item si
INNER JOIN public.stock_in sin ON sin.""StockInId"" = si.""StockInId""
WHERE NOT EXISTS (
    SELECT 1 FROM public.stock_in_item_extend x WHERE x.""StockInItemId"" = si.""ItemId"");


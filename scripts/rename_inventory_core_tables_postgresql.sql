-- =============================================================================
-- 库存核心表重命名（与 EF 实体 ToTable / [Table] 一致）
-- 执行前请备份；PostgreSQL 会随表重命名自动维护指向该表的外键元数据。
-- 顺序：先扩展表/子表，再明细表，再主表，避免名称冲突；stock_item 在入库链改名之后再改。
-- =============================================================================

-- 入库链（旧名 -> 新名）
ALTER TABLE IF EXISTS public.stockinitemextend RENAME TO stock_in_item_extend;
ALTER TABLE IF EXISTS public.stockinitem RENAME TO stock_in_item;
ALTER TABLE IF EXISTS public.stockinextend RENAME TO stock_in_extend;
ALTER TABLE IF EXISTS public.stockin RENAME TO stock_in;

-- 在库明细（引用 stock_in / stock_in_item）
ALTER TABLE IF EXISTS public.stockitem RENAME TO stock_item;

-- 出库链
ALTER TABLE IF EXISTS public.stockoutitemextend RENAME TO stock_out_item_extend;
ALTER TABLE IF EXISTS public.stockoutitem RENAME TO stock_out_item;
ALTER TABLE IF EXISTS public.stockout RENAME TO stock_out;

-- 说明：未在此脚本改名的相关表仍保持旧名，例如 public.stockinnotify、public.stockoutrequest。


-- =============================================================================
-- 库存相关 DDL/DML（PostgreSQL）
-- 1) stockinextend → stock_in_extend
-- 2) 新建 stock_extend（1:1 public.stock）
-- 3) stock_item 增加 stock_item_code + 回填 + 唯一索引 + 注释
--
-- 前置假设（与当前 EF 迁移一致）：
--   - 主表已为 public.stock、明细已为 public.stock_item（若仍为 stockin/stockitem，
--     请先执行 scripts/rename_inventory_core_tables_postgresql.sql 或对应迁移）
--   - 执行前请备份数据库
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1) 入库主单扩展表重命名
-- -----------------------------------------------------------------------------
ALTER TABLE IF EXISTS public.stockinextend RENAME TO stock_in_extend;

-- 可选：外键约束名仍为旧前缀时，可改为与表名一致（若不存在会报错，按需取消注释）
-- ALTER TABLE IF EXISTS public.stock_in_extend
--   RENAME CONSTRAINT "FK_stockinextend_stockin" TO "FK_stock_in_extend_stock_in";
-- ALTER TABLE IF EXISTS public.stock_in_extend
--   RENAME CONSTRAINT "PK_stockinextend" TO "PK_stock_in_extend";

-- -----------------------------------------------------------------------------
-- 2) 库存分桶扩展表 stock_extend
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.stock_extend (
  "StockId" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  "CreateTime" timestamp with time zone NOT NULL,
  "ModifyTime" timestamp with time zone NULL,
  CONSTRAINT "PK_stock_extend" PRIMARY KEY ("StockId"),
  CONSTRAINT "FK_stock_extend_stock" FOREIGN KEY ("StockId")
    REFERENCES public.stock ("StockId") ON DELETE CASCADE
);

COMMENT ON TABLE public.stock_extend IS '库存分桶扩展：在库明细 stock_item_code 行序号水位';

-- 为已有在库明细对应的分桶插入扩展行（水位先置 0，后续由回填更新）
INSERT INTO public.stock_extend ("StockId", last_item_line_seq, "CreateTime")
SELECT DISTINCT si."StockAggregateId", 0, NOW()
FROM public.stock_item si
WHERE NOT EXISTS (SELECT 1 FROM public.stock_extend x WHERE x."StockId" = si."StockAggregateId");

-- -----------------------------------------------------------------------------
-- 3) stock_item.stock_item_code
-- -----------------------------------------------------------------------------
ALTER TABLE public.stock_item ADD COLUMN IF NOT EXISTS stock_item_code character varying(64) NULL;

-- 回填：同分桶内按时间、主键排序生成 {StockCode}-{序号}；无 StockCode 时用 SKI-LEGACY- 前缀
WITH numbered AS (
  SELECT si."StockItemId",
         NULLIF(trim(st."StockCode"), '') AS sc,
         ROW_NUMBER() OVER (PARTITION BY si."StockAggregateId" ORDER BY si."CreateTime", si."StockItemId") AS rn
  FROM public.stock_item si
  INNER JOIN public.stock st ON st."StockId" = si."StockAggregateId"
)
UPDATE public.stock_item u
SET stock_item_code = CASE
  WHEN n.sc IS NOT NULL THEN n.sc || '-' || n.rn::text
  ELSE 'SKI-LEGACY-' || replace(u."StockItemId"::text, '-', '')
END
FROM numbered n
WHERE u."StockItemId" = n."StockItemId";

UPDATE public.stock_item u
SET stock_item_code = 'SKI-LEGACY-' || replace(u."StockItemId"::text, '-', '')
WHERE u.stock_item_code IS NULL;

-- 同步 stock_extend 水位 = 各分桶下在库明细行数
UPDATE public.stock_extend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.stock_item si WHERE si."StockAggregateId" = e."StockId"), 0);

ALTER TABLE public.stock_item ALTER COLUMN stock_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_stock_item_agg_linecode"
  ON public.stock_item ("StockAggregateId", stock_item_code);

COMMENT ON COLUMN public.stock_item.stock_item_code IS '在库明细业务编号：{StockCode}-{行序号}（规则同 stock_in_item_code）';

COMMIT;

-- =============================================================================
-- 回滚参考（按需分段执行；有数据丢失风险）
-- =============================================================================
-- BEGIN;
-- DROP INDEX IF EXISTS "IX_stock_item_agg_linecode";
-- ALTER TABLE IF EXISTS public.stock_item DROP COLUMN IF EXISTS stock_item_code;
-- DROP TABLE IF EXISTS public.stock_extend;
-- ALTER TABLE IF EXISTS public.stock_in_extend RENAME TO stockinextend;
-- COMMIT;

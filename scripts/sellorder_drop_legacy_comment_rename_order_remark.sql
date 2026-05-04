-- 删除 sellorder 历史「拼接备注」列 comment，将 order_remark 重命名为 comment（列语义：自由备注）。
-- 警告：DROP comment 会永久删除该列数据；执行前请确认已用 Debug「刷新Sellorder」或其它方式完成拆分迁移。

ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS comment;
ALTER TABLE IF EXISTS public.sellorder RENAME COLUMN order_remark TO comment;

COMMENT ON COLUMN public.sellorder.comment IS '订单备注（自由文本；原 order_remark）';

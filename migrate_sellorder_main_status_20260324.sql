-- =============================================================================
-- 销售订单主状态：历史值 → 新语义（一次性割接脚本）
-- 旧: … 4已发货 5已收货 6已完成 -1已取消 -2已驳回
-- 新: … 3进行中 4完成 -1审核失败 -2取消
--
-- ⚠ 上线窗口内执行一次即可；勿重复执行（尤其 status=4 在新语义下表示「完成」，
--   若再次整表 CASE 会把「完成」误改为「进行中」）。
-- 执行前请备份。
-- =============================================================================

BEGIN;

-- 负值：旧 -1=取消 / -2=驳回 → 新 -2=取消 / -1=审核失败（分两避免中间态冲突）
UPDATE sellorder SET status = -99 WHERE status = -2;
UPDATE sellorder SET status = -2 WHERE status = -1;
UPDATE sellorder SET status = -1 WHERE status = -99;

-- 正值：旧 5/6 与新语义不重叠，可安全迁移
UPDATE sellorder SET status = 3 WHERE status = 5;   -- 已收货 -> 进行中
UPDATE sellorder SET status = 4 WHERE status = 6;   -- 已完成 -> 完成

-- 若历史数据中 status=4 表示「已发货」而非新语义「完成」，在确认库中尚无新「完成」单时执行：
-- UPDATE sellorder SET status = 3 WHERE status = 4;

COMMIT;

-- =============================================================================
-- 销售订单主状态：由旧编码 2/3/4 → 10/20/100（与 SellOrderMainStatus 一致）
-- 在曾使用「审核通过=2、进行中=3、完成=4」的库上执行一次；执行前备份。
-- =============================================================================

BEGIN;

UPDATE sellorder SET status = 10 WHERE status = 2;
UPDATE sellorder SET status = 20 WHERE status = 3;
UPDATE sellorder SET status = 100 WHERE status = 4;

COMMIT;

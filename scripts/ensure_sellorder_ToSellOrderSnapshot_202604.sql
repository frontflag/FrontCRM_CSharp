-- 若 API 仍为旧版本（EF 会 SELECT "ToSellOrderSnapshot"）而库中无此列，会导致
-- 42703 及销售订单列表 / 销售订单明细(lines) 等接口 500。
-- 任选其一：
--   A) 重新编译并重启 CRM.API（与当前仓库一致的可不再查此列，一般无需本脚本）
--   B) 在目标库执行本脚本一次，旧版 API 即可恢复
--
-- PostgreSQL：列名区分大小写时需双引号。

ALTER TABLE IF EXISTS public.sellorder
    ADD COLUMN IF NOT EXISTS "ToSellOrderSnapshot" text NULL;

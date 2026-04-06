-- 出库申请 stockoutrequest：补充 ShipmentMethod（出货方式）
-- 与 EF 迁移 20260406180000_AddStockOutRequestShipmentMethod 一致。
-- 若未跑过迁移，执行本脚本可消除「申请出库」时 42703 字段 ShipmentMethod 不存在。
-- 推荐长期做法：在开发环境执行
--   dotnet run --project CRM.DbMigrator -- --apply --force-dev
-- 或 dotnet ef database update（在 CRM.Infrastructure 项目上下文）

ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS "ShipmentMethod" character varying(64) NULL;

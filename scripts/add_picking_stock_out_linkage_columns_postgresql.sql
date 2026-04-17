-- =============================================================================
-- PostgreSQL：拣货 / 出库关联列补全（与 EF 迁移一致）
-- =============================================================================
-- 对应迁移：CRM.Infrastructure/Migrations/20260622120000_PickingStockItemAndStockOutLinkage.cs
--
-- 典型报错（Npgsql 42703）：
--   column p.stock_item_id does not exist        → pickingtaskitem
--   column s.picking_task_id does not exist      → stockout
--   column s.picking_task_item_id does not exist → stockoutitem
--
-- 建议：维护窗口、先备份；本脚本使用 IF NOT EXISTS，可重复执行。
--
-- 推荐长期做法：在部署机对同一连接串执行
--   dotnet ef database update --project CRM.Infrastructure --startup-project CRM.API
-- 若已手工执行本脚本补列，请再执行文末「补写 __EFMigrationsHistory」段，避免后续 ef 重复跑迁移体。
-- =============================================================================

ALTER TABLE IF EXISTS public.stockout ADD COLUMN IF NOT EXISTS "picking_task_id" character varying(36) NULL;
COMMENT ON COLUMN public.stockout."picking_task_id" IS '完成拣货后执行出库时关联的 pickingtask.Id';

ALTER TABLE IF EXISTS public.pickingtaskitem ADD COLUMN IF NOT EXISTS "stock_item_id" character varying(36) NULL;
COMMENT ON COLUMN public.pickingtaskitem."stock_item_id" IS '在库明细 stockitem.StockItemId；新流程拣货必填';

ALTER TABLE IF EXISTS public.stockoutitem ADD COLUMN IF NOT EXISTS "picking_task_item_id" character varying(36) NULL;
COMMENT ON COLUMN public.stockoutitem."picking_task_item_id" IS '来源拣货明细 pickingtaskitem.Id';

-- -----------------------------------------------------------------------------
-- 可选：仅当「已手工执行上述 ALTER、且未通过 dotnet ef 应用该迁移」时执行，
-- 将迁移登记到 __EFMigrationsHistory，避免日后 ef database update 再次执行同一 Up。
-- ProductVersion 请与当前部署的 Microsoft.EntityFrameworkCore 主版本一致（示例 9.0.11）。
-- -----------------------------------------------------------------------------
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260622120000_PickingStockItemAndStockOutLinkage', '9.0.11'
WHERE NOT EXISTS (
    SELECT 1 FROM public."__EFMigrationsHistory" h
    WHERE h."MigrationId" = '20260622120000_PickingStockItemAndStockOutLinkage'
);

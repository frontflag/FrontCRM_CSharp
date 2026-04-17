-- =============================================================================
-- public.stock_in：相对早期结构（如仅有头表基础列）需补加的列
-- 与当前 EF 映射一致：采购/销售行关联为 snake_case；质检单列为 ""QcCode""/""QCID""；
-- 地域为 ""RegionType""；操作人 Guid 为 create_by_user_id / modify_by_user_id。
-- 若库中仍存在历史 Pascal 冗余列（""PurchaseOrderItemCode"" 等），请先执行迁移
-- 20260417100000_RebuildOrderLineLinkColumnsSnakeCase 或手工 DROP，再执行本脚本，避免重复语义列。
-- =============================================================================

-- 到货通知 + 质检冗余（20260414120000）
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""SourceCode"" character varying(32) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""SourceId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""QcCode"" character varying(32) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""QCID"" character varying(36) NULL;

-- 采销行快照已迁至 stockinitemextend；若旧库仍有头表四列，请执行 scripts/drop_stockin_order_line_snapshot_columns_postgresql.sql 或迁移 20260629130000。

-- 地域（20260408120000）
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""RegionType"" smallint NOT NULL DEFAULT 10;

-- 质检状态、创建/审核人（与 20260319021703_AddVendorTables 中 stockin 对齐）
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""InspectStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""CreatedBy"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""ApprovedBy"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS ""ApprovedTime"" timestamp with time zone NULL;

-- 业务操作人 Guid（20260412100000，与 BaseEntity 长整型 CreateUserId 并存）
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL;
ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS modify_by_user_id character varying(36) NULL;

COMMENT ON COLUMN public.stock_in.""RegionType"" IS '地域类型：10=境内 20=境外';
COMMENT ON COLUMN public.stock_in.create_by_user_id IS '创建人用户主键（varchar Guid）';
COMMENT ON COLUMN public.stock_in.modify_by_user_id IS '修改人用户主键（varchar Guid）';

-- 可选：合计数量由 numeric(18,4) 改为 integer（20260511120000；有数据时先备份）
-- ALTER TABLE IF EXISTS public.stock_in
--   ALTER COLUMN ""TotalQuantity"" TYPE integer USING (round(""TotalQuantity"")::integer);


-- FrontCRM 提成：表 + 种子 + 权限（幂等）
-- 顺序：版本/系数 -> 计算参数 -> 动态/锁定/水位 -> 数据池 -> 权限
-- API 启动不改库，请手工执行后重启。

-- 1. 系数版本头
CREATE TABLE IF NOT EXISTS public.commission_rate_version (
  id character varying(36) NOT NULL,
  role_type smallint NOT NULL,
  version_no integer NOT NULL,
  remark character varying(200) NULL,
  status smallint NOT NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
  created_by character varying(36) NULL,
  updated_by character varying(36) NULL,
  CONSTRAINT "PK_commission_rate_version" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_rate_version_role_type" CHECK (role_type IN (1, 2)),
  CONSTRAINT "CK_commission_rate_version_status" CHECK (status IN (1, 2, 3)),
  CONSTRAINT "CK_commission_rate_version_no" CHECK (version_no >= 1),
  CONSTRAINT "UQ_commission_rate_version_role_no" UNIQUE (role_type, version_no)
);

CREATE UNIQUE INDEX IF NOT EXISTS "UQ_commission_rate_version_active"
  ON public.commission_rate_version (role_type)
  WHERE status = 2;

COMMENT ON TABLE public.commission_rate_version IS 'commission rate version';

INSERT INTO public.commission_rate_version (id, role_type, version_no, remark, status, created_at, updated_at)
VALUES
  ('c2000000-0000-4000-8000-000000000001', 1, 1, 'V1', 2, NOW(), NOW()),
  ('c2000000-0000-4000-8000-000000000002', 2, 1, 'V1', 2, NOW(), NOW())
ON CONFLICT (role_type, version_no) DO NOTHING;

-- 2. 系数行
CREATE TABLE IF NOT EXISTS public.commission_rate (
  id character varying(36) NOT NULL,
  version_id character varying(36) NULL,
  user_level smallint NOT NULL,
  threshold_1 numeric(18,2) NULL,
  threshold_2 numeric(18,2) NULL,
  threshold_3 numeric(18,2) NULL,
  threshold_4 numeric(18,2) NULL,
  threshold_5 numeric(18,2) NULL,
  threshold_6 numeric(18,2) NULL,
  threshold_7 numeric(18,2) NULL,
  threshold_8 numeric(18,2) NULL,
  threshold_9 numeric(18,2) NULL,
  threshold_10 numeric(18,2) NULL,
  rate_points_1 numeric(5,2) NULL,
  rate_points_2 numeric(5,2) NULL,
  rate_points_3 numeric(5,2) NULL,
  rate_points_4 numeric(5,2) NULL,
  rate_points_5 numeric(5,2) NULL,
  rate_points_6 numeric(5,2) NULL,
  rate_points_7 numeric(5,2) NULL,
  rate_points_8 numeric(5,2) NULL,
  rate_points_9 numeric(5,2) NULL,
  rate_points_10 numeric(5,2) NULL,
  remark character varying(200) NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
  created_by character varying(36) NULL,
  updated_by character varying(36) NULL,
  CONSTRAINT "PK_commission_rate" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_rate_user_level" CHECK (user_level BETWEEN 1 AND 20)
);

ALTER TABLE public.commission_rate ADD COLUMN IF NOT EXISTS version_id character varying(36);

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'commission_rate' AND column_name = 'role_type'
  ) THEN
    UPDATE public.commission_rate r
    SET version_id = v.id
    FROM public.commission_rate_version v
    WHERE r.version_id IS NULL
      AND v.role_type = r.role_type
      AND v.version_no = 1;
  END IF;
END $$;

UPDATE public.commission_rate
SET version_id = 'c2000000-0000-4000-8000-000000000001'
WHERE version_id IS NULL;

ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "UQ_commission_rate_role_level";
DROP INDEX IF EXISTS "IX_commission_rate_RoleType_UserLevel";
DROP INDEX IF EXISTS "IX_commission_rate_role_type_user_level";
ALTER TABLE public.commission_rate DROP CONSTRAINT IF EXISTS "CK_commission_rate_role_type";
ALTER TABLE public.commission_rate DROP COLUMN IF EXISTS role_type;
ALTER TABLE public.commission_rate ALTER COLUMN version_id SET NOT NULL;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'UQ_commission_rate_version_level') THEN
    ALTER TABLE public.commission_rate
      ADD CONSTRAINT "UQ_commission_rate_version_level" UNIQUE (version_id, user_level);
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_commission_rate_version') THEN
    ALTER TABLE public.commission_rate
      ADD CONSTRAINT "FK_commission_rate_version"
      FOREIGN KEY (version_id) REFERENCES public.commission_rate_version (id);
  END IF;
END $$;

INSERT INTO public.commission_rate (id, version_id, user_level, created_at, updated_at)
SELECT gen_random_uuid()::text, v.id, lv.lvl, NOW(), NOW()
FROM public.commission_rate_version v
CROSS JOIN generate_series(1, 20) AS lv(lvl)
WHERE v.version_no = 1
ON CONFLICT (version_id, user_level) DO NOTHING;

-- 3. 全局计算参数（延期列停用）
CREATE TABLE IF NOT EXISTS public.commission_calc_setting (
  id character varying(36) NOT NULL,
  receipt_writeoff_delay_days integer NOT NULL DEFAULT 0,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_by character varying(36) NULL,
  CONSTRAINT "PK_commission_calc_setting" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_calc_setting_delay_days"
    CHECK (receipt_writeoff_delay_days BETWEEN 0 AND 3650)
);

INSERT INTO public.commission_calc_setting (id, receipt_writeoff_delay_days, created_at, updated_at)
VALUES ('c2000000-0000-4000-8000-000000000010', 0, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- 4. 预计动态表
CREATE TABLE IF NOT EXISTS public.commission_dynamic (
  id character varying(36) NOT NULL,
  role_type smallint NOT NULL,
  stock_out_item_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  user_name character varying(50) NOT NULL,
  user_level smallint NOT NULL,
  version_id character varying(36) NULL,
  rate_points numeric(5,2) NOT NULL DEFAULT 0,
  gp_usd numeric(18,2) NOT NULL DEFAULT 0,
  period_gp_usd numeric(18,2) NOT NULL DEFAULT 0,
  commission_usd numeric(18,2) NOT NULL DEFAULT 0,
  sell_order_id character varying(36) NULL,
  sell_order_code character varying(32) NULL,
  sell_order_item_id character varying(36) NULL,
  sell_order_item_code character varying(64) NULL,
  purchase_order_id character varying(36) NULL,
  purchase_order_code character varying(32) NULL,
  stock_out_id character varying(36) NOT NULL,
  stock_out_code character varying(32) NOT NULL,
  stock_out_date date NULL,
  receipt_date date NOT NULL,
  pool_date date NOT NULL,
  calc_month character varying(7) NOT NULL DEFAULT '',
  entry_kind smallint NOT NULL DEFAULT 0,
  calc_date date NOT NULL,
  calc_at timestamp with time zone NOT NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_dynamic" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_dynamic_role_type" CHECK (role_type IN (1, 2)),
  CONSTRAINT "UQ_commission_dynamic_role_item" UNIQUE (role_type, stock_out_item_id)
);

ALTER TABLE public.commission_dynamic
  ADD COLUMN IF NOT EXISTS calc_month character varying(7) NOT NULL DEFAULT '';
ALTER TABLE public.commission_dynamic
  ADD COLUMN IF NOT EXISTS entry_kind smallint NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_user_pool"
  ON public.commission_dynamic (role_type, user_id, pool_date);
CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_calc"
  ON public.commission_dynamic (role_type, calc_date);
CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_user_month"
  ON public.commission_dynamic (role_type, user_id, calc_month);

-- 5. 正式锁定表
CREATE TABLE IF NOT EXISTS public.commission_locked (
  id character varying(36) NOT NULL,
  role_type smallint NOT NULL,
  stock_out_item_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  user_name character varying(50) NOT NULL,
  user_level smallint NOT NULL,
  version_id character varying(36) NULL,
  rate_points numeric(5,2) NOT NULL DEFAULT 0,
  gp_usd numeric(18,2) NOT NULL DEFAULT 0,
  period_gp_usd numeric(18,2) NOT NULL DEFAULT 0,
  commission_usd numeric(18,2) NOT NULL DEFAULT 0,
  sell_order_id character varying(36) NULL,
  sell_order_code character varying(32) NULL,
  sell_order_item_id character varying(36) NULL,
  sell_order_item_code character varying(64) NULL,
  purchase_order_id character varying(36) NULL,
  purchase_order_code character varying(32) NULL,
  stock_out_id character varying(36) NOT NULL,
  stock_out_code character varying(32) NOT NULL,
  stock_out_date date NULL,
  receipt_date date NOT NULL,
  pool_date date NOT NULL,
  calc_month character varying(7) NOT NULL DEFAULT '',
  entry_kind smallint NOT NULL DEFAULT 0,
  calc_date date NOT NULL,
  calc_at timestamp with time zone NOT NULL,
  term character(6) NOT NULL,
  lock_date date NOT NULL,
  locked_at timestamp with time zone NOT NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_locked" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_locked_role_type" CHECK (role_type IN (1, 2)),
  CONSTRAINT "UQ_commission_locked_role_item" UNIQUE (role_type, stock_out_item_id)
);

ALTER TABLE public.commission_locked
  ADD COLUMN IF NOT EXISTS calc_month character varying(7) NOT NULL DEFAULT '';
ALTER TABLE public.commission_locked
  ADD COLUMN IF NOT EXISTS entry_kind smallint NOT NULL DEFAULT 0;

CREATE INDEX IF NOT EXISTS "IX_commission_locked_role_term_user"
  ON public.commission_locked (role_type, term, user_id);
CREATE INDEX IF NOT EXISTS "IX_commission_locked_role_user_month"
  ON public.commission_locked (role_type, user_id, calc_month);

-- 6. Job 水位
CREATE TABLE IF NOT EXISTS public.commission_job_watermark (
  id character varying(36) NOT NULL,
  dynamic_job_date date NULL,
  lock_term character(6) NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_job_watermark" PRIMARY KEY (id)
);

INSERT INTO public.commission_job_watermark (id, created_at)
VALUES ('c2000000-0000-4000-8000-000000000011', NOW())
ON CONFLICT (id) DO NOTHING;

-- 7. 提成数据池
CREATE TABLE IF NOT EXISTS public.commission_pool (
  id character varying(36) NOT NULL,
  stock_out_item_id character varying(36) NOT NULL,
  stock_out_id character varying(36) NOT NULL,
  stock_out_code character varying(32) NOT NULL,
  stock_out_date date NULL,
  sales_user_id character varying(36) NULL,
  purchase_user_id character varying(36) NULL,
  gp_usd numeric(18,2) NOT NULL DEFAULT 0,
  receipt_progress_status smallint NOT NULL DEFAULT 0,
  receipt_date date NULL,
  sell_order_id character varying(36) NULL,
  sell_order_code character varying(32) NULL,
  sell_order_item_id character varying(36) NULL,
  sell_order_item_code character varying(64) NULL,
  purchase_order_id character varying(36) NULL,
  purchase_order_code character varying(32) NULL,
  purchase_order_item_id character varying(36) NULL,
  purchase_order_item_code character varying(64) NULL,
  sales_commission_status smallint NOT NULL DEFAULT 0,
  purchase_commission_status smallint NOT NULL DEFAULT 0,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_pool" PRIMARY KEY (id),
  CONSTRAINT "UQ_commission_pool_stock_out_item" UNIQUE (stock_out_item_id),
  CONSTRAINT "CK_commission_pool_sales_flag" CHECK (sales_commission_status IN (0, 1)),
  CONSTRAINT "CK_commission_pool_purchase_flag" CHECK (purchase_commission_status IN (0, 1, 2))
);

CREATE INDEX IF NOT EXISTS "IX_commission_pool_sell_order_item"
  ON public.commission_pool (sell_order_item_id);
CREATE INDEX IF NOT EXISTS "IX_commission_pool_sales_status"
  ON public.commission_pool (sales_commission_status);
CREATE INDEX IF NOT EXISTS "IX_commission_pool_purchase_status"
  ON public.commission_pool (purchase_commission_status);

-- 8. 权限：提成参数（仅 SYS_ADMIN / SYS_MANAGER）
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT v."PermissionId", v."PermissionCode", v."PermissionName", v."PermissionType", v."Resource", v."Action", v."Status", NOW()
FROM (VALUES
  ('31000000-0000-4000-8000-0000000000a1', 'system.params.commission.read', '系统-提成参数-查看', 'api', 'system.params.commission', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a2', 'system.params.commission.write', '系统-提成参数-维护', 'api', 'system.params.commission', 'write', 1),
  ('31000000-0000-4000-8000-0000000000a3', 'system.params.commission.sales.read', '系统-提成参数-业务员系数-查看', 'api', 'system.params.commission.sales', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a4', 'system.params.commission.sales.write', '系统-提成参数-业务员系数-维护', 'api', 'system.params.commission.sales', 'write', 1),
  ('31000000-0000-4000-8000-0000000000a5', 'system.params.commission.purchase.read', '系统-提成参数-采购员系数-查看', 'api', 'system.params.commission.purchase', 'read', 1),
  ('31000000-0000-4000-8000-0000000000a6', 'system.params.commission.purchase.write', '系统-提成参数-采购员系数-维护', 'api', 'system.params.commission.purchase', 'write', 1)
) AS v("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status")
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p."PermissionCode" = v."PermissionCode"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER')
  AND p."PermissionCode" LIKE 'system.params.commission%'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

DELETE FROM sys_role_permission rp
USING sys_permission p, sys_role r
WHERE rp."PermissionId" = p."PermissionId"
  AND rp."RoleId" = r."RoleId"
  AND p."PermissionCode" LIKE 'system.params.commission%'
  AND r."RoleCode" NOT IN ('SYS_ADMIN', 'SYS_MANAGER');

-- 9. 权限：预计 / 正式列表
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT v."PermissionId", v."PermissionCode", v."PermissionName", v."PermissionType", v."Resource", v."Action", v."Status", NOW()
FROM (VALUES
  ('c1000000-0000-4000-8000-000000000101', 'commission-estimated-sales.read', '业务预计提成-查看', 'api', 'commission-estimated-sales', 'read', 1),
  ('c1000000-0000-4000-8000-000000000102', 'commission-estimated-purchase.read', '采购预计提成-查看', 'api', 'commission-estimated-purchase', 'read', 1),
  ('c1000000-0000-4000-8000-000000000103', 'commission-official-sales.read', '业务正式提成-查看', 'api', 'commission-official-sales', 'read', 1),
  ('c1000000-0000-4000-8000-000000000104', 'commission-official-purchase.read', '采购正式提成-查看', 'api', 'commission-official-purchase', 'read', 1)
) AS v("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status")
WHERE NOT EXISTS (
  SELECT 1 FROM sys_permission p WHERE p."PermissionCode" = v."PermissionCode"
);

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER')
  AND p."PermissionCode" IN (
    'commission-estimated-sales.read',
    'commission-estimated-purchase.read',
    'commission-official-sales.read',
    'commission-official-purchase.read'
  )
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

DELETE FROM sys_role_permission rp
USING sys_permission p, sys_role r
WHERE rp."PermissionId" = p."PermissionId"
  AND rp."RoleId" = r."RoleId"
  AND p."PermissionCode" IN (
    'commission-estimated-sales.read',
    'commission-estimated-purchase.read',
    'commission-official-sales.read',
    'commission-official-purchase.read'
  )
  AND r."RoleCode" NOT IN ('SYS_ADMIN', 'SYS_MANAGER');

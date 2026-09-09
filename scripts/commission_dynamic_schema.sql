-- 预计提成动态表。API 启动不改库，请手工执行后重启。
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
  calc_date date NOT NULL,
  calc_at timestamp with time zone NOT NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_dynamic" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_dynamic_role_type" CHECK (role_type IN (1, 2)),
  CONSTRAINT "UQ_commission_dynamic_role_item" UNIQUE (role_type, stock_out_item_id)
);

CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_user_pool"
  ON public.commission_dynamic (role_type, user_id, pool_date);

CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_calc"
  ON public.commission_dynamic (role_type, calc_date);

COMMENT ON TABLE public.commission_dynamic IS '预计提成：每日覆盖未锁定出库行';
COMMENT ON COLUMN public.commission_dynamic.pool_date IS '首次满足出库+收款完成+延期的上海计算日';

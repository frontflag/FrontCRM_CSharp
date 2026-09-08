-- 仅建表 + 预置 40 行（幂等）。权限请用 seed_commission_params_permissions.sql。
CREATE TABLE IF NOT EXISTS public.commission_rate (
  id character varying(36) NOT NULL,
  role_type smallint NOT NULL,
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
  CONSTRAINT "CK_commission_rate_role_type" CHECK (role_type IN (1, 2)),
  CONSTRAINT "CK_commission_rate_user_level" CHECK (user_level BETWEEN 1 AND 20),
  CONSTRAINT "UQ_commission_rate_role_level" UNIQUE (role_type, user_level)
);

COMMENT ON TABLE public.commission_rate IS '提成系数：一行=类型+用户等级，最多 10 档';

INSERT INTO public.commission_rate (id, role_type, user_level, created_at, updated_at)
SELECT gen_random_uuid()::text, t.role_type, lv.lvl, NOW(), NOW()
FROM (VALUES (1), (2)) AS t(role_type)
CROSS JOIN generate_series(1, 20) AS lv(lvl)
ON CONFLICT (role_type, user_level) DO NOTHING;

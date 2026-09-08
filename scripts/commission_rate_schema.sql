-- 提成系数表 + 预置 40 行 + 权限（幂等）。API 启动不会自动改库，请手工执行。
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

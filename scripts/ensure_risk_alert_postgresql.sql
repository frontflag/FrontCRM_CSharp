-- 风险预警：全站一份门槛 + 控制台现算。可重复执行。
CREATE TABLE IF NOT EXISTS public.risk_alert_setting (
  id                          varchar(36)    NOT NULL,
  inventory_amount_usd_max    numeric(18,2)  NOT NULL DEFAULT 0,
  stock_age_days_max          integer        NOT NULL DEFAULT 90,
  receivable_amount_usd_max   numeric(18,2)  NOT NULL DEFAULT 0,
  customer_receivable_usd_max numeric(18,2)  NOT NULL DEFAULT 0,
  so_receivable_age_days_max  integer        NOT NULL DEFAULT 90,
  create_time                 timestamptz    NOT NULL,
  modify_time                 timestamptz    NOT NULL,
  CONSTRAINT pk_risk_alert_setting PRIMARY KEY (id)
);

INSERT INTO public.risk_alert_setting (
  id, inventory_amount_usd_max, stock_age_days_max,
  receivable_amount_usd_max, customer_receivable_usd_max, so_receivable_age_days_max,
  create_time, modify_time
)
SELECT 'risk-alert-default', 0, 90, 0, 0, 90, NOW(), NOW()
WHERE NOT EXISTS (
  SELECT 1 FROM public.risk_alert_setting s WHERE s.id = 'risk-alert-default'
);

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime") VALUES
('31000000-0000-4000-8000-0000000000b1', 'system.params.risk-alert.read', '系统-预警参数-查看', 'api', 'system.params.risk-alert', 'read', 1, NOW()),
('31000000-0000-4000-8000-0000000000b2', 'system.params.risk-alert.write', '系统-预警参数-维护', 'api', 'system.params.risk-alert', 'write', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
CROSS JOIN sys_permission p
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER')
  AND p."PermissionCode" IN ('system.params.risk-alert.read', 'system.params.risk-alert.write')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission rp
    WHERE rp."RoleId" = r."RoleId" AND rp."PermissionId" = p."PermissionId"
  );

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260914140000_RiskAlert', '9.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM public."__EFMigrationsHistory" h
  WHERE h."MigrationId" = '20260914140000_RiskAlert'
);

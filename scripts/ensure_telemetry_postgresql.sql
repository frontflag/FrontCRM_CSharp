-- 用户行为埋点：事件明细 + 日汇总 + 权限（幂等）

CREATE TABLE IF NOT EXISTS public.telemetry_event (
    id BIGSERIAL PRIMARY KEY,
    event_id VARCHAR(36) NOT NULL,
    event_type VARCHAR(32) NOT NULL,
    event_name VARCHAR(64) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    received_at TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    session_id VARCHAR(36) NULL,
    user_id VARCHAR(36) NULL,
    user_name VARCHAR(50) NULL,
    page_key VARCHAR(200) NULL,
    route_path VARCHAR(500) NULL,
    browser VARCHAR(80) NULL,
    os VARCHAR(80) NULL,
    device_type VARCHAR(40) NULL,
    screen_w INT NULL,
    screen_h INT NULL,
    user_agent VARCHAR(500) NULL,
    payload_json TEXT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_telemetry_event_event_id
    ON public.telemetry_event (event_id);
CREATE INDEX IF NOT EXISTS ix_telemetry_event_occurred
    ON public.telemetry_event (occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_telemetry_event_type_name_time
    ON public.telemetry_event (event_type, event_name, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_telemetry_event_session
    ON public.telemetry_event (session_id);

COMMENT ON TABLE public.telemetry_event IS '用户行为埋点明细（保留 90 天）';

CREATE TABLE IF NOT EXISTS public.telemetry_daily_page (
    id BIGSERIAL PRIMARY KEY,
    stat_date DATE NOT NULL,
    page_key VARCHAR(200) NOT NULL,
    view_count BIGINT NOT NULL DEFAULT 0,
    visible_ms_sum BIGINT NOT NULL DEFAULT 0,
    active_ms_sum BIGINT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_telemetry_daily_page
    ON public.telemetry_daily_page (stat_date, page_key);

CREATE TABLE IF NOT EXISTS public.telemetry_daily_action (
    id BIGSERIAL PRIMARY KEY,
    stat_date DATE NOT NULL,
    page_key VARCHAR(200) NOT NULL DEFAULT '',
    action_id VARCHAR(200) NOT NULL,
    click_count BIGINT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_telemetry_daily_action
    ON public.telemetry_daily_action (stat_date, page_key, action_id);

CREATE TABLE IF NOT EXISTS public.telemetry_daily_api (
    id BIGSERIAL PRIMARY KEY,
    stat_date DATE NOT NULL,
    method VARCHAR(16) NOT NULL DEFAULT 'GET',
    path_template VARCHAR(300) NOT NULL,
    call_count BIGINT NOT NULL DEFAULT 0,
    fail_count BIGINT NOT NULL DEFAULT 0,
    duration_ms_sum BIGINT NOT NULL DEFAULT 0,
    duration_ms_max INT NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_telemetry_daily_api
    ON public.telemetry_daily_api (stat_date, method, path_template);

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT gen_random_uuid()::text, 'biz.telemetry.analytics', '埋点分析-查看', 'api', 'telemetry', 'analytics', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'biz.telemetry.analytics');

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.telemetry.analytics' AND p."Status" = 1
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

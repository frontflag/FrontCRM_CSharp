-- AI 用户反馈助手：会话/消息/工单表 + 场景种子 + 权限
-- 可重复执行

CREATE TABLE IF NOT EXISTS public.ai_assistant_session (
    id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    active_skill VARCHAR(32) NOT NULL DEFAULT 'feedback',
    status VARCHAR(20) NOT NULL DEFAULT 'open',
    preferred_category VARCHAR(20) NULL,
    page_url VARCHAR(500) NULL,
    route_name VARCHAR(100) NULL,
    route_params_json TEXT NULL,
    route_query_json TEXT NULL,
    user_agent VARCHAR(500) NULL,
    consecutive_off_topic_count INTEGER NOT NULL DEFAULT 0,
    user_turn_count INTEGER NOT NULL DEFAULT 0,
    inferred_biz_ref VARCHAR(200) NULL,
    "CreateTime" TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "CreateUserId" BIGINT NULL,
    "ModifyTime" TIMESTAMPTZ NULL,
    "ModifyUserId" BIGINT NULL
);

CREATE INDEX IF NOT EXISTS ix_ai_assistant_session_user_time
    ON public.ai_assistant_session (user_id, "CreateTime" DESC);
CREATE INDEX IF NOT EXISTS ix_ai_assistant_session_status
    ON public.ai_assistant_session (status);

COMMENT ON TABLE public.ai_assistant_session IS 'AI 助手会话（首期技能：反馈）';

CREATE TABLE IF NOT EXISTS public.ai_assistant_message (
    id VARCHAR(36) PRIMARY KEY,
    session_id VARCHAR(36) NOT NULL,
    role VARCHAR(20) NOT NULL,
    content TEXT NULL,
    attachment_document_id VARCHAR(36) NULL,
    "CreateTime" TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "CreateUserId" BIGINT NULL,
    "ModifyTime" TIMESTAMPTZ NULL,
    "ModifyUserId" BIGINT NULL
);

CREATE INDEX IF NOT EXISTS ix_ai_assistant_message_session_time
    ON public.ai_assistant_message (session_id, "CreateTime");

COMMENT ON TABLE public.ai_assistant_message IS 'AI 助手多轮消息';

CREATE TABLE IF NOT EXISTS public.user_feedback (
    id VARCHAR(36) PRIMARY KEY,
    session_id VARCHAR(36) NOT NULL,
    category VARCHAR(20) NOT NULL,
    title VARCHAR(200) NOT NULL,
    summary TEXT NOT NULL,
    biz_ref VARCHAR(200) NULL,
    repro_steps TEXT NULL,
    page_url VARCHAR(500) NULL,
    route_name VARCHAR(100) NULL,
    route_params_json TEXT NULL,
    route_query_json TEXT NULL,
    submit_user_id VARCHAR(36) NOT NULL,
    needs_handling BOOLEAN NOT NULL DEFAULT TRUE,
    is_handled BOOLEAN NOT NULL DEFAULT FALSE,
    completed_date DATE NULL,
    handle_remark VARCHAR(2000) NULL,
    modify_by_user_id VARCHAR(36) NULL,
    "CreateTime" TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "CreateUserId" BIGINT NULL,
    "ModifyTime" TIMESTAMPTZ NULL,
    "ModifyUserId" BIGINT NULL
);

CREATE INDEX IF NOT EXISTS ix_user_feedback_create_time
    ON public.user_feedback ("CreateTime" DESC);
CREATE INDEX IF NOT EXISTS ix_user_feedback_handling
    ON public.user_feedback (needs_handling, is_handled);
CREATE INDEX IF NOT EXISTS ix_user_feedback_submit_user
    ON public.user_feedback (submit_user_id);

COMMENT ON TABLE public.user_feedback IS '用户反馈工单（AI 精炼字段 + 运维处理标识）';
COMMENT ON COLUMN public.user_feedback.biz_ref IS '业务单号/Id（自动或用户告知）';
COMMENT ON COLUMN public.user_feedback.needs_handling IS '是否需要处理';
COMMENT ON COLUMN public.user_feedback.is_handled IS '是否完成处理';

-- Prompt + Scenario（默认 mock，可在 AI 配置改为 moonshot）
INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-0000000000f1',
    'assistant.feedback.collect',
    1,
    '你是 FrontCRM 反馈助手（系统反馈助手），只处理本系统的问题反馈与改进建议，禁止闲聊、讲笑话、百科、写作。根据对话与页面上下文，只返回合法 JSON（禁止 markdown 代码块），字段：assistantMessage, intent(feedback|off_topic), conversationAction(ask|finalize|decline|reject_offtopic), slots{category,title,summary,bizRef,reproSteps}, missingSlots[]。信息不足则 ask 追问；齐套则 finalize 并静默精炼 title/summary（勿向用户念精炼稿）；跑题则 reject_offtopic；多轮仍不齐则 decline。结束语只能类似「已记录并通知开发团队」，禁止承诺解决日期。路由已有业务 Id 则写入 bizRef 不必再问。category=bug 且无路由 Id 时必须追问单号（用户说没有单号可在 summary 注明）；category=suggestion/other 不要追问单号，聚焦痛点与期望。',
    '根据历史消息继续对话并输出 JSON。',
    'json',
    '{"assistantMessage":"string","intent":"feedback|off_topic","conversationAction":"ask|finalize|decline|reject_offtopic","slots":{"category":"bug|suggestion|other|null","title":"string|null","summary":"string|null","bizRef":"string|null","reproSteps":"string|null"},"missingSlots":["string"]}',
    true
)
ON CONFLICT (code, version) DO UPDATE SET
    system_prompt = EXCLUDED.system_prompt,
    user_prompt_template = EXCLUDED.user_prompt_template,
    json_schema_hint = EXCLUDED.json_schema_hint,
    is_active = EXCLUDED.is_active;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search
)
VALUES (
    'a3000001-0000-4000-8000-0000000000f1',
    'assistant.feedback.collect',
    'AI 反馈助手收集',
    '顶栏多轮反馈/建议收集',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-0000000000f1',
    0,
    '[]'::jsonb,
    '[]'::jsonb,
    2048,
    0.30,
    'biz.feedback.submit',
    30,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    gen_random_uuid()::text,
    'biz.feedback.submit',
    '反馈助手-提交',
    'api',
    'feedback',
    'submit',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'biz.feedback.submit'
);

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT
    gen_random_uuid()::text,
    'biz.feedback.admin',
    '反馈助手-运维管理',
    'api',
    'feedback',
    'admin',
    1,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'biz.feedback.admin'
);

-- 系统管理员：提交 + 运维
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('biz.feedback.submit', 'biz.feedback.admin') AND p."Status" = 1
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 全部启用角色均可提交反馈（运维管理仍仅 SYS_ADMIN 等显式授权）
INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.feedback.submit' AND p."Status" = 1
WHERE COALESCE(r."Status", 1) = 1
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

-- 检查：客户新闻动态监测 场景 + 模板 + 最近调用 + 最近简报
-- DBeaver-safe / Navicat-safe：占位符用 CHR 拼接判断，勿写双花括号字面量
-- 在 DBeaver 整脚本执行；关注 ok_* 列，应为 true。

-- 1) 模板正文与占位符
SELECT
    t.code,
    t.version,
    t.name,
    t.is_active,
    t.is_deleted,
    t.output_format,
    length(t.system_prompt) AS system_len,
    length(t.user_prompt_template) AS user_len,
    (t.is_active AND NOT t.is_deleted) AS ok_active,
    (t.output_format = 'text') AS ok_output_text,
    (position('核心摘要' in t.system_prompt) > 0) AS ok_has_core_heading_rule,
    (position('核心摘要' in coalesce(t.json_schema_hint, '')) > 0) AS ok_hint_core,
    (position(
        CHR(123) || CHR(123) || 'company_name' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_company,
    (position(
        CHR(123) || CHR(123) || 'start_date' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_start,
    (position(
        CHR(123) || CHR(123) || 'end_date' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_end,
    (position(
        CHR(123) || CHR(123) || 'monitor_mode' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_mode,
    (position(
        CHR(123) || CHR(123) || 'window_months' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_window,
    (position('窗口：' in t.user_prompt_template) > 0) AS ok_v2_window_line,
    (position('禁止输出检索过程' in t.system_prompt) > 0
        OR position('让我继续搜索' in t.system_prompt) > 0) AS ok_no_search_loop_rule,
    (position('检索充分性' in t.system_prompt) > 0) AS ok_v4_thorough,
    (position('然后立即停止生成' in t.system_prompt) = 0) AS ok_no_early_stop,
    left(t.system_prompt, 180) AS system_head
FROM public.ai_prompt_template t
WHERE t.code = 'customer.news.monitor'
ORDER BY t.version DESC;

-- 2) 场景（对照行业新闻：生产应为非 mock 且联网）
SELECT
    s.code,
    s.is_enabled,
    s.is_deleted,
    s.enable_web_search,
    s.provider_code,
    s.model,
    s.max_tokens,
    t.code AS template_code,
    t.version AS template_version,
    p.is_enabled AS provider_enabled,
    (s.is_enabled AND NOT s.is_deleted) AS ok_scenario_on,
    (s.provider_code <> 'mock') AS ok_not_mock,
    (s.enable_web_search = true) AS ok_web_search,
    (s.prompt_template_id = t.id) AS ok_template_linked,
    (p.id IS NOT NULL AND p.is_enabled AND NOT p.is_deleted) AS ok_provider
FROM public.ai_scenario s
LEFT JOIN public.ai_prompt_template t ON t.id = s.prompt_template_id
LEFT JOIN public.ai_provider p ON p.code = s.provider_code
WHERE s.code IN ('customer.news.monitor', 'industry.news.briefing')
ORDER BY s.code;

-- 3) 最近 10 次调用
SELECT
    created_at,
    status,
    provider_code,
    model,
    from_cache,
    latency_ms,
    prompt_tokens,
    completion_tokens,
    left(coalesce(error_message, ''), 200) AS error_message,
    left(coalesce(prompt_preview, ''), 120) AS prompt_preview
FROM public.ai_invocation_log
WHERE scenario_code = 'customer.news.monitor'
ORDER BY created_at DESC
LIMIT 10;

-- 4) 最近简报（markdown_len 很小且 looks_empty_stub=true = 被收成空简报）
SELECT
    generated_at,
    status,
    period_start,
    period_end,
    length(markdown) AS markdown_len,
    (position('本期无重大事项' in markdown) > 0) AS looks_empty_stub,
    (position('让我继续搜索' in markdown) > 0) AS looks_search_loop,
    left(markdown, 160) AS markdown_head
FROM public.customer_news_briefing
ORDER BY generated_at DESC
LIMIT 10;

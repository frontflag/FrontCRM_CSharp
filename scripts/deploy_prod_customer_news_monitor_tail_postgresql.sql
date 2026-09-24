-- 生产收尾：客户新闻动态监测 切真实厂商 + 联网 + 校验
-- 须先执行：
--   scripts/ensure_customer_news_monitor_postgresql.sql
--   scripts/ensure_customer_news_monitor_prompt_v3_postgresql.sql
-- DBeaver-safe / Navicat-safe：无双花括号字面量
-- 可重复执行。
-- 客户详情一级页签 / 控制台业务总览：无新表、无新权限码，本段不涉及。

UPDATE public.ai_prompt_template
SET name = '客户新闻动态监测',
    is_active = true,
    is_deleted = false,
    output_format = 'text',
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor'
  AND version = 1;

-- 优先抄行业新闻已配置的非 mock 厂商；否则 moonshot / kimi-k2.6
UPDATE public.ai_scenario dest
SET
    provider_code = src.provider_code,
    model = src.model,
    enable_web_search = true,
    is_enabled = true,
    is_deleted = false,
    max_tokens = 8192,
    modify_time = (now() AT TIME ZONE 'utc')
FROM (
    SELECT provider_code, model
    FROM public.ai_scenario
    WHERE code = 'industry.news.briefing' AND provider_code <> 'mock'
    UNION ALL
    SELECT provider_code, model
    FROM public.ai_scenario
    WHERE code = 'material.intel.lookup' AND provider_code <> 'mock'
    LIMIT 1
) src
WHERE dest.code = 'customer.news.monitor';

UPDATE public.ai_scenario
SET
    provider_code = 'moonshot',
    model = 'kimi-k2.6',
    enable_web_search = true,
    is_enabled = true,
    is_deleted = false,
    max_tokens = 8192,
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor'
  AND provider_code = 'mock';

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.news.monitor';

-- 校验：ok_not_mock / ok_web_search / ok_v3 均应为 true
SELECT
    s.code,
    s.is_enabled,
    s.enable_web_search,
    s.provider_code,
    s.model,
    s.max_tokens,
    t.version AS template_version,
    t.is_active,
    length(t.system_prompt) AS system_len,
    (s.provider_code <> 'mock') AS ok_not_mock,
    (s.enable_web_search = true) AS ok_web_search,
    (s.max_tokens >= 8192) AS ok_max_tokens,
    (position('让我继续搜索' in t.system_prompt) > 0) AS ok_v3,
    (position(
        CHR(123) || CHR(123) || 'start_date' || CHR(125) || CHR(125)
        in t.user_prompt_template) > 0) AS ok_ph_start,
    (p.code IS NOT NULL AND p.is_enabled AND NOT p.is_deleted) AS ok_provider
FROM public.ai_scenario s
JOIN public.ai_prompt_template t ON t.id = s.prompt_template_id
LEFT JOIN public.ai_provider p ON p.code = s.provider_code
WHERE s.code = 'customer.news.monitor';

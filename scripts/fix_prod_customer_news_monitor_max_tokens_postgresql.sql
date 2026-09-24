-- 生产修复：客户新闻动态监测 max_tokens 调回 8192，并解开空简报锁死的首次窗口
-- 原因：生产脚本曾把上限压到 2048。联网检索多轮后写不完「## 核心摘要」，
--       落库被收成「本期无重大事项」。本地种子是 8192，所以本地能出长简报。
-- DBeaver 整段执行。可重复。

-- 1) 与本地对齐：额度 + 联网
UPDATE public.ai_scenario
SET
    max_tokens = 8192,
    enable_web_search = true,
    is_enabled = true,
    is_deleted = false,
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor';

-- 2) 校验：provider 不能是 mock；max_tokens 应为 8192
SELECT
    s.code,
    s.provider_code,
    s.model,
    s.enable_web_search,
    s.max_tokens,
    s.is_enabled,
    (s.provider_code <> 'mock') AS ok_not_mock,
    (s.enable_web_search = true) AS ok_web_search,
    (s.max_tokens >= 8192) AS ok_max_tokens
FROM public.ai_scenario s
WHERE s.code = 'customer.news.monitor';

-- 3) 对照：本地成功 vs 生产空简报（拓邦 CUS00005H）
SELECT
    generated_at,
    status,
    period_start,
    period_end,
    length(markdown) AS markdown_len,
    (position('本期无重大事项' in markdown) > 0
        AND length(markdown) < 200) AS looks_empty_stub,
    invocation_id
FROM public.customer_news_briefing
WHERE customer_id = (
    SELECT "CustomerId" FROM public.customerinfo
    WHERE "CustomerCode" = 'CUS00005H'
    LIMIT 1
)
ORDER BY generated_at DESC
LIMIT 10;

SELECT
    created_at,
    status,
    provider_code,
    model,
    latency_ms,
    prompt_tokens,
    completion_tokens,
    left(coalesce(error_message, ''), 200) AS error_message
FROM public.ai_invocation_log
WHERE scenario_code = 'customer.news.monitor'
ORDER BY created_at DESC
LIMIT 10;

-- 4) 空成功简报会把下次窗口锁成「当天」。删掉后再点「立即抓取」，才会重新覆盖近 3 个月。
DELETE FROM public.customer_news_briefing
WHERE customer_id = (
    SELECT "CustomerId" FROM public.customerinfo
    WHERE "CustomerCode" = 'CUS00005H'
    LIMIT 1
)
AND status = 'success'
AND length(markdown) < 200
AND position('本期无重大事项' in markdown) > 0;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.news.monitor';

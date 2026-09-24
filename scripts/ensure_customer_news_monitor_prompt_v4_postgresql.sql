-- 增量：客户新闻动态监测提示词 v4
-- 纠正「未知当成未上市」「一次检索失败就停」「最多两轮」导致上市公司简报过薄
-- DBeaver-safe / Navicat-safe：无双花括号字面量
-- 可重复执行。执行后须重启 CRM.API（C# 硬约束与 stock_code 输入一并改了）。

-- 1) 最多两轮 -> 四轮（与代码 WebSearchMaxRounds=4 对齐）
UPDATE public.ai_prompt_template
SET system_prompt = replace(
      system_prompt,
      convert_from(decode('e88194e7bd91e6a380e7b4a2e69c80e5a49ae4b8a4e8bdae', 'hex'), 'UTF8'),
      convert_from(decode('e88194e7bd91e6a380e7b4a2e69c80e5a49ae59b9be8bdae', 'hex'), 'UTF8')
    ),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor' AND version = 1
  AND position(convert_from(decode('e88194e7bd91e6a380e7b4a2e69c80e5a49ae4b8a4e8bdae', 'hex'), 'UTF8') in system_prompt) > 0;

-- 2) 去掉「找不到就立即停止生成」
UPDATE public.ai_prompt_template
SET system_prompt = replace(
      system_prompt,
      convert_from(decode('e689bee4b88de588b0e585ace5bc80e4bfa1e681afe697b6efbc8ce59084e88a82e58faae58699e3808ce69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e3808defbc8ce784b6e5908ee7ab8be58db3e5819ce6ada2e7949fe68890e38082', 'hex'), 'UTF8'),
      convert_from(decode('e5bf85e9a1bbe68c89e585ace58fb8e585a8e7a7b0e6a380e7b4a2e698afe590a6e4b88ae5b882e58f8ae5b7a8e6bdae2fe4baa4e69893e68980e585ace5918ae38081e585ace58fb8e5ae98e7bd91e38081e69d83e5a881e8b4a2e7bb8fe5aa92e4bd93e38082e8be93e585a5e4b8ade79a84e882a1e7a5a8e4bba3e7a081e88ba5e4b8bae3808ce69caae79fa5e3808de4b88de5be97e5bd93e68890e69caae4b88ae5b882e38082e4bb85e5bd93e69f90e7bbb4e5baa6e5ae8ce68890e6a380e7b4a2e5908ee7aa97e58fa3e58685e7a1aee5ae9ee697a0e4bfa1e681afe697b6e6898de58699e3808ce69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e3808de38082e7a681e6ada2e59ba0e4b880e6aca1e6a380e7b4a2e697a0e7bb93e69e9ce5b0b1e5819ce6ada2e38082e7a681e6ada2e58699e585a5e8b5b7e6ada2e697a5e4b98be5a496e79a84e697a7e997bbe38082e6a0b8e5bf83e69198e8a681e5b7b2e69c89e4ba8be5ae9ee697b6efbc8ce4ba8be5ae9ee8afa6e68385e4b88de5be97e695b4e7af87e58699e69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e38082', 'hex'), 'UTF8')
    ),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor' AND version = 1
  AND position('然后立即停止生成' in system_prompt) > 0;

-- 3) 补检索充分性（幂等）
UPDATE public.ai_prompt_template
SET system_prompt = system_prompt || convert_from(decode('0a2d20e38090e6a380e7b4a2e58585e58886e680a7e38091e882a1e7a5a8e4bba3e7a081e88ba5e4b8bae3808ce69caae79fa5e3808de68896e3808ce69caae7bbb4e68aa4e3808defbc8ce5bf85e9a1bbe68c89e585ace58fb8e585a8e7a7b0e6a380e7b4a22041e882a12fe6b8afe882a1e4b88ae5b882e4b8bbe4bd93e4b88ee4bba3e7a081efbc8ce7a681e6ada2e68a8ae69caae79fa5e5bd93e68890e69caae4b88ae5b882e38082e5bf85e9a1bbe6a380e7b4a2e5b7a8e6bdaee8b584e8aeafe38081e6b7b1e4baa4e689802fe4b88ae4baa4e68980e585ace5918ae38081e585ace58fb8e5ae98e7bd91e696b0e997bbe38081e69d83e5a881e8b4a2e7bb8fe5aa92e4bd93e38082e8bf91e4b889e4b8aae69c88e7aa97e58fa3e58685e79a84e4b88ae5b882e585ace58fb8e9809ae5b8b8e69c89e58d8ae5b9b4e68aa5e68896e5ae9ae69c9fe585ace5918aefbc8ce4b88de5be97e69caae6a380e7b4a2e5b0b1e58699e69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e38082e3808ce69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e3808de58faae883bde59ca8e8afa5e7bbb4e5baa6e7a1aee5ae9ee6a380e7b4a2e8bf87e5908ee4bdbfe794a8e38082e6a0b8e5bf83e69198e8a681e5b7b2e69c89e4ba8be5ae9ee697b6efbc8ce4ba8be5ae9ee8afa6e68385e59084e5b08fe88a82e4b88de5be97e4b880e5be8be58699e69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e38082e4ba8be5ae9ee69da1e79baee697a5e69c9fe5bf85e9a1bbe890bde59ca8e794a8e688b7e7bb99e587bae79a84e8b5b7e6ada2e697a5e4b98be58685efbc8ce7a681e6ada2e794a8e7aa97e58fa3e5a496e697a7e997bbe58585e695b0e380820a', 'hex'), 'UTF8'),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor' AND version = 1
  AND position('【检索充分性】' in system_prompt) = 0;

UPDATE public.ai_scenario
SET max_tokens = 8192,
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor'
  AND max_tokens < 8192;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.news.monitor';

SELECT
    t.code,
    (position('检索充分性' in t.system_prompt) > 0) AS ok_v4,
    (position('然后立即停止生成' in t.system_prompt) = 0) AS ok_no_early_stop,
    (position('联网检索最多两轮' in t.system_prompt) = 0) AS ok_not_two_rounds,
    s.max_tokens,
    (s.max_tokens >= 8192) AS ok_max_tokens
FROM public.ai_prompt_template t
JOIN public.ai_scenario s ON s.prompt_template_id = t.id
WHERE t.code = 'customer.news.monitor' AND t.version = 1;

-- 可选：删掉半空成功简报，才能重新抓近 3 个月（否则下次窗口从上次 period_end 次日开始）。
-- 先 SELECT 确认再 DELETE。
--
-- SELECT b.id, c."OfficialName", b.period_start, b.period_end, length(b.markdown) AS markdown_len
-- FROM public.customer_news_briefing b
-- JOIN public.customerinfo c ON c."CustomerId" = b.customer_id
-- WHERE b.status = 'success'
--   AND position('本期无重大事项' in b.markdown) > 0;
--
-- DELETE FROM public.customer_news_briefing b
-- USING public.customerinfo c
-- WHERE c."CustomerId" = b.customer_id
--   AND c."OfficialName" LIKE '%拓邦%'
--   AND b.status = 'success'
--   AND position('本期无重大事项' in b.markdown) > 0;

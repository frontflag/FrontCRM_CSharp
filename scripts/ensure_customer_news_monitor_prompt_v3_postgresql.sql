-- 增量：客户新闻动态监测提示词防检索循环 + 缩短 max_tokens
-- DBeaver-safe / Navicat-safe：无双花括号字面量
-- 可重复执行。

UPDATE public.ai_prompt_template
SET system_prompt = system_prompt || convert_from(decode('0a2d20e88194e7bd91e6a380e7b4a2e69c80e5a49ae4b8a4e8bdaeefbc8ce6a380e7b4a2e8bf87e7a88be4b88de5be97e58699e585a5e6ada3e69687e38082e7a681e6ada2e587bae78eb0e3808ce8aea9e68891e7bba7e7bbade6909ce7b4a2e3808de3808ce8aea9e68891e8b083e695b4e6909ce7b4a2e7ad96e795a5e3808de3808ce8aea9e68891e8bf9be8a18ce4b880e6aca1e69bb4e585a8e99da2e79a84e6909ce7b4a2e3808de58f8ae5908ce4b989e58f8de5a48de380820a2d20e689bee4b88de588b0e585ace5bc80e4bfa1e681afe697b6efbc8ce59084e88a82e58faae58699e3808ce69cace69c9fe697a0e9878de5a4a7e4ba8be9a1b9e3808defbc8ce784b6e5908ee7ab8be58db3e5819ce6ada2e7949fe68890e380820a2d20e58699e5ae8ce3808ce5be85e6a0b8e5ae9ee58cbae3808de5bf85e9a1bbe5819ce6ada2efbc8ce7a681e6ada2e9878de5a48de5908ce4b880e58fa5e8af9de380820a', 'hex'), 'UTF8'),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'customer.news.monitor' AND version = 1
  AND position('让我继续搜索' in system_prompt) = 0;

UPDATE public.ai_scenario
SET max_tokens = 2048
WHERE code = 'customer.news.monitor'
  AND max_tokens > 2048;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'customer.news.monitor';

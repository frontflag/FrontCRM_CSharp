-- 增量：ai_scenario.enable_web_search（联网搜索开关）
-- 执行后重启 API 非必须；在 AI 配置页即可保存「联网搜索」。

ALTER TABLE public.ai_scenario
    ADD COLUMN IF NOT EXISTS enable_web_search boolean NOT NULL DEFAULT false;

-- 可选：默认开启 material.intel.lookup 联网
-- UPDATE public.ai_scenario SET enable_web_search = true WHERE code = 'material.intel.lookup';

-- 增量：ai_prompt_template 增加中文名称，供 AI 配置「模板」列表展示
-- DBeaver-safe / Navicat-safe：无双花括号占位符
-- 可重复执行。空名称从同 code / 关联场景回填。

ALTER TABLE public.ai_prompt_template
  ADD COLUMN IF NOT EXISTS name varchar(200) NOT NULL DEFAULT '';

UPDATE public.ai_prompt_template t
SET name = s.name
FROM public.ai_scenario s
WHERE btrim(COALESCE(t.name, '')) = ''
  AND btrim(COALESCE(s.name, '')) <> ''
  AND (
    s.prompt_template_id = t.id
    OR s.code = t.code
  );

UPDATE public.ai_prompt_template SET name = '查询物料规格' WHERE code = 'material.spec.lookup' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '物料情报查询' WHERE code = 'material.intel.lookup' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '客户情报调查' WHERE code = 'customer.intel.lookup' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '供应商情报调查' WHERE code = 'vendor.intel.lookup' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '行业新闻简报' WHERE code = 'industry.news.briefing' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '客户新闻动态监测' WHERE code = 'customer.news.monitor' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = 'AI 反馈助手收集' WHERE code = 'assistant.feedback.collect' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建客户' WHERE code = 'entity.parse.customer' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建需求' WHERE code = 'entity.parse.rfq' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建供应商' WHERE code = 'entity.parse.vendor' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建客户联系人' WHERE code = 'entity.parse.customer_contact' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建供应商联系人' WHERE code = 'entity.parse.vendor_contact' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建客户地址' WHERE code = 'entity.parse.customer_address' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '解析创建供应商地址' WHERE code = 'entity.parse.vendor_address' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '名片创建客户' WHERE code = 'entity.parse.customer_business_card' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '名片创建供应商' WHERE code = 'entity.parse.vendor_business_card' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '需求 Excel 列映射' WHERE code = 'entity.parse.rfq_excel_column_map' AND btrim(name) = '';
UPDATE public.ai_prompt_template SET name = '需求 Excel 品牌映射' WHERE code = 'entity.parse.rfq_excel_brand_map' AND btrim(name) = '';

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260914170000_AiPromptTemplateName', '9.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM public."__EFMigrationsHistory" h
  WHERE h."MigrationId" = '20260914170000_AiPromptTemplateName'
);

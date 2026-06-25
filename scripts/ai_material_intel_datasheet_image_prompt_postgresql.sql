-- 增量：material.intel.lookup 提示词 — 要求返回 DataSheet 与物料图片链接
-- 执行后清除缓存；需部署含 JsonSchemaHint 注入的最新 API

UPDATE public.ai_prompt_template
SET system_prompt = '你是面向中国采购与销售用户的电子元器件情报助手。根据型号仅返回合法 JSON（禁止 markdown 代码块）。JSON 键名必须保持英文 snake_case 不变。所有描述性字符串值（category、part_number_breakdown[].meaning、technical_features[]、application_areas[]、alternatives、pricing、industry_news、disclaimer 等）必须使用简体中文，禁止用英文句子描述（品牌/原厂/官方型号代号可保留英文）。即使联网检索到英文网页，也必须先翻译为简体中文再写入 JSON。未知用 null，无数据用空数组。不要编造价格、库存或新闻。spec_params 中必须包含 datasheet_url（原厂或授权渠道 DataSheet 规格书链接）与 image_url（物料产品图链接）；可经联网检索公开来源，须为可访问的 https 链接，无法确认时填 null，禁止编造链接。disclaimer 用中文说明仅供参考，请以原厂或授权渠道规格为准。',
    user_prompt_template = convert_from(
        decode(
            'e69fa5e8afa2e794b5e5ad90e58583e599a8e4bbb6e59e8be58fb7efbc9a7b7b706e7d7de38082e8bf94e59b9e206272616e645f696e666fe38081737065635f706172616d73efbc88e590ab20706172745f6e756d6265725f627265616b646f776eefbc8c6d65616e696e6720e5bf85e9a1bbe794a8e7ae80e4bd93e4b8ade69687e8a7a3e9878ae59084e6aeb5e590abe4b989efbc9be9a1bbe5b0bde9878fe68f90e4be9b206461746173686565745f75726c20e4b88e20696d6167655f75726cefbc8ce697a0e6b395e7a1aee8aea4e5a1ab206e756c6cefbc89e380816170706c69636174696f6e5f6172656173efbc88e6af8fe9a1b9e7ae80e4bd93e4b8ade69687efbc8ce7a681e6ada2e88bb1e69687efbc89e38081616c7465726e617469766573e3808170726963696e67e38081696e6475737472795f6e657773e38081646973636c61696d6572efbc88e7ae80e4bd93e4b8ade69687efbc89e38082e5868de6aca1e5bcbae8b083efbc9a6d65616e696e67e380816170706c69636174696f6e5f6172656173e38081746563686e6963616c5f666561747572657320e7ad89e68f8fe8bfb0e5ad97e6aeb5e4b88de5be97e8be93e587bae88bb1e69687e38082',
            'hex'
        ),
        'UTF8'
    ),
    modify_time = (now() AT TIME ZONE 'utc')
WHERE code = 'material.intel.lookup' AND version = 1;

DELETE FROM public.ai_invocation_cache WHERE scenario_code = 'material.intel.lookup';

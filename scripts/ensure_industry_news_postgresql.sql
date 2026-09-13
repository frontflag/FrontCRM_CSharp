-- 增量：行业新闻简报表 + AI 场景 industry.news.briefing
-- DBeaver-safe / Navicat-safe：user_prompt 占位符经 CHR 拼接 start_date / end_date，勿写双花括号字面量
-- 可重复执行。控制台登录即可读；生成走后台作业或 AI 管理权限手动触发。

CREATE TABLE IF NOT EXISTS public.industry_news_briefing (
  id              varchar(36)    NOT NULL,
  briefing_date   date           NOT NULL,
  period_start    date           NOT NULL,
  period_end      date           NOT NULL,
  items_json      jsonb          NOT NULL DEFAULT '[]'::jsonb,
  markdown        text           NOT NULL DEFAULT '',
  status          varchar(20)    NOT NULL,
  error_message   text           NULL,
  invocation_id   varchar(36)    NULL,
  generated_at    timestamptz    NOT NULL,
  create_time     timestamptz    NOT NULL,
  modify_time     timestamptz    NOT NULL,
  CONSTRAINT pk_industry_news_briefing PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_industry_news_briefing_date
  ON public.industry_news_briefing (briefing_date);

INSERT INTO public.ai_prompt_template (id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active)
VALUES (
    'a2000001-0000-4000-8000-000000000012',
    'industry.news.briefing',
    1,
    convert_from(decode('e4bda0e698afe4b880e5908de794b5e5ad90e58583e599a8e4bbb6e8a18ce4b89ae7a094e7a9b6e58886e69e90e5b888efbc8ce69c8de58aa1e5afb9e8b1a1e698afe58583e599a8e4bbb6e58886e994802fe98787e8b4ad2fe5b882e59cbae59ba2e9989fe38082e58faae8bf94e59b9ee59088e6b395204a534f4eefbc88e7a681e6ada2206d61726b646f776e20e4bba3e7a081e59d97efbc89e380824a534f4e20e994aee5908de5bf85e9a1bbe4bf9de68c81e88bb1e6968720736e616b655f63617365e38082e68980e69c89e99da2e59091e794a8e688b7e79a84e5ad97e7aca6e4b8b2e5bf85e9a1bbe4bdbfe794a8e7ae80e4bd93e4b8ade69687e38082', 'hex'), 'UTF8'),
    convert_from(decode('2320e4bbbbe58aa10ae6909ce7b4a2e5b9b6e695b4e7908620', 'hex'), 'UTF8')
    || CHR(123) || CHR(123) || 'start_date' || CHR(125) || CHR(125)
    || convert_from(decode('20e887b320', 'hex'), 'UTF8')
    || CHR(123) || CHR(123) || 'end_date' || CHR(125) || CHR(125)
    || convert_from(decode('e794b5e5ad90e58583e599a8e4bbb6e8a18ce4b89ae79a84e696b0e997bbe38082e69687e4b8ade3808ce69cace591a8e3808de59d87e68c87e8afa5e697a5e69c9fe7aa97e58fa3efbc88e585b1e4b8a4e4b8aae887aae784b6e697a5efbc89efbc8ce4b88de698afe887aae784b6e591a8e4b880e588b0e591a8e697a5e380820a0a2320e585b3e6b3a8e9878de782b9efbc88e68c89e4bc98e58588e7baa7e68e92e5ba8fefbc890a312e20e8a18ce4b89ae58aa8e68081efbc9ae5b882e59cbae8a784e6a8a1e38081e699afe6b094e5baa6e38081e68a80e69cafe8b68be58abfefbc88e5a68220414920e7ae97e58a9be38081e58588e8bf9be5b081e8a385e38081e7acace4b889e4bba3e58d8ae5afbce4bd93efbc890a322e20e585b3e994aee58e82e59586e696b0e997bbefbc9ae9878de782b9e8b79fe8b8aae88bb1e4bc9fe8bebee38081e9ab98e9809ae380815449e38081414449e38081e69d91e794b0e38081e4b889e6989fe794b5e69cbae3808154444be38081e59bbde5b7a8e38081e58586e69893e5889be696b0e38081e995bfe6b19fe5ad98e582a8e38081e995bfe991abe5ad98e582a8e38081e7ab8be8aeafe7b2bee5af86e38081e6af94e4ba9ae8bfaae794b5e5ad90e7ad89e4bc98e58588e585b3e6b3a8e58e82e59586e79a84e696b0e59381e58f91e5b883e38081e8aea2e58d95e38081e59088e4bd9ce38081e689a9e4baa7e38082e4b88ae8bfb0e5908de58d95e4bb85e4b8bae4bc98e58588e585b3e6b3a8e6a0b7e4be8befbc8ce4b88de698afe5b081e997ade5908de58d95efbc9be7aa97e58fa3e69c9fe58685e79c9fe6ada3e9878de5a4a7e79a84e58e82e59586e696b0e997bbe4b88de5be97e59ba0e4b88de59ca8e5908de58d95e8808ce98197e6bc8fe380820a332e20e8a18ce68385e4bbb7e6a0bce6b3a2e58aa8efbc9ae5ad98e582a8efbc884452414d2f4e414e44efbc89e380814d4c4343e38081e58a9fe78e87e599a8e4bbb6e38081e6a8a1e68b9fe88aafe78987e38081e8a2abe58aa8e58583e4bbb6e79a84e78eb0e8b4a7e4bbb72fe59088e7baa6e4bbb7e8b5b0e58abfe4b88ee4baa4e69c9fe58f98e58c960a342e20e585ace58fb8e9878de7bb84e4b88ee5b9b6e8b4adefbc9ae5b9b6e8b4ade38081e58886e68b86e38081e7a0b4e4baa7e38081e59088e8b584e3808149504fe38081e5868de89e8de8b5840a352e20e585ace58fb8e6b2bbe79086efbc9ae9ab98e7aea1e58f98e58aa8e38081e891a3e4ba8be4bc9ae694b9e98089e38081e882a1e69d83e58f98e58aa8e38081e79b91e7aea1e5a484e7bd9a2fe8ada6e7a4ba0a362e20e5b195e4bc9ae4bfa1e681afefbc9ae7aa97e58fa3e69c9fe58685e4b8bee58a9e2fe58db3e5b086e4b8bee58a9ee79a84e8a18ce4b89ae5b195e4bc9aefbc88e5a6822043494f45e380814949434945e38081e68595e5b0bce9bb91e794b5e5ad90e5b195e38081434553e3808153454d49434f4eefbc89e58f8ae585b6e58f91e5b883e79a84e9878de8a681e4bfa1e681af0a0a2320e8be93e587bae6a0bce5bc8f0ae6ada3e69687e58699e59ca8204a534f4e20e79a84206d61726b646f776e20e5ad97e6aeb5e4b8adefbc8ce5bf85e9a1bbe58c85e590abe4b894e4bb85e68c89e4b88be58897e4ba94e4b8aae4ba8ce7baa7e6a087e9a298e7bb84e7bb87efbc88e4b88de8a681e58699e3808ce4b880e38081e4ba8ce38081e4b889e38081e59b9be38081e4ba94e3808defbc89efbc9a0a232320e69cace591a8e8a681e997bb20546f702035efbc88e4b880e58fa5e8af9de6a682e68bacefbc8ce6a087e6b3a8e697a5e69c9fe5928ce9878de8a681e680a720e29885312d33efbc890a232320e58886e7b1bbe8afa6e8bfb0efbc88e68c89e4b88ae8bfb0203620e4b8aae7bbb4e5baa6e7bb84e7bb87efbc8ce6af8fe69da1e6b3a8e6988ee697a5e69c9fe38081e4ba8be4bbb6e38081e5bdb1e5938defbc890a232320e8a18ce68385e9809fe8a788efbc88e794a8e8a1a8e6a0bcefbc9ae59381e7b1bb207c20e69cace591a8e58f98e58c96207c20e9a9b1e58aa8e59ba0e7b4a0207c20e5908ee5b882e588a4e696adefbc890a232320e69cace591a8e5b195e69c9befbc88e7aa97e58fa3e4b98be5908ee580bce5be97e585b3e6b3a8e79a8420332d3520e4bbb6e4ba8befbc9ae8b4a2e68aa5e58f91e5b883e38081e5b195e4bc9ae38081e694bfe7ad96e88a82e782b9efbc890a232320e58685e5aeb9e680bbe7bb93efbc8831353020e5ad97e4bba5e58685efbc8ce68f90e782bce7aa97e58fa3e69c9fe58685e8a18ce4b89ae4b8bbe7babfe5928ce5afb9e98787e8b4ad2fe99480e594aee59ba2e9989fe79a84e8a18ce58aa8e5bbbae8aeaeefbc890a0ae58da1e78987e794a8204a534f4e20e79a84206974656d7320e695b0e7bb84efbc9ae58886e7b1bbe38081e6a087e9a298e38081e697a5e69c9fe38081e4b880e58fa5e8af9de38081e9878de8a681e680a7e380820a0a2320e8a681e6b1820a2d20e68980e69c89e696b0e997bbe6b3a8e6988ee58f91e7949fe697a5e69c9fefbc8ce8b685e587bae69cace7aa97e58fa3e88c83e59bb4e79a84e58685e5aeb9e6988ee7a1aee6a087e6b3a8e4b8ba22e8838ce699af220a2d20e58cbae58886e4ba8be5ae9ee4b88ee4bca0e997bbefbc88e4bca0e997bbe99c80e6a087e6b3a822e69caae7bb8fe8af81e5ae9e22efbc890a2d20e4b88de7a1aee5ae9ae79a84e4bfa1e681afe6988ee7a1aee8afb4e6988eefbc8ce4b88de88786e6b58b0a2d20e6af8fe4b8aae6aeb5e890bde5898de4b88de8a681e6a087e6b3a8e5ba8fe58fb70a', 'hex'), 'UTF8'),
    'json',
    convert_from(decode('e58faae8bf94e59b9ee5a682e4b88b204a534f4e20e5afb9e8b1a1efbc8ce4b88de8a681e8be93e587bae585b6e5ae83e69687e5ad97efbc9a0a7b226974656d73223a5b7b2263617465676f7279223a22e8a18ce4b89ae58aa8e680817ce585b3e994aee58e82e595867ce8a18ce68385e4bbb7e6a0bc7ce9878de7bb84e5b9b6e8b4ad7ce585ace58fb8e6b2bbe790867ce5b195e4bc9ae4bfa1e681af222c227469746c65223a22737472696e67222c226f636375727265645f6f6e223a22595959592d4d4d2d4444222c2273756d6d617279223a22e4b880e58fa5e8af9d222c22696d706f7274616e6365223a312c2269735f6261636b67726f756e64223a66616c73652c22756e636f6e6669726d6564223a66616c73657d5d2c226d61726b646f776e223a22737472696e67227d0a6974656d7320e69c80e5a49a203520e69da1efbc8ce68c8920696d706f7274616e636520e4bb8ee9ab98e588b0e4bd8ee38082696d706f7274616e636520e58faae883bde698af2031e380813220e688962033e380820a6d61726b646f776e20e5bf85e9a1bbe68c89e4b88be58897e4ba8ce7baa7e6a087e9a298e7bb84e7bb87efbc8ce6a087e9a298e5bf85e9a1bbe98090e5ad97e4b880e887b4efbc8ce6a087e9a298e5898de4b88de8a681e58699e3808ce4b880e38081e4ba8ce38081e4b889e38081e59b9be38081e4ba94e3808de68896e998bfe68b89e4bcafe5ba8fe58fb7efbc8ce6af8fe4b8aae6aeb5e890bde5898de4b99fe4b88de8a681e6a087e6b3a8e5ba8fe58fb7efbc9a0a232320e69cace591a8e8a681e997bb20546f7020350a232320e58886e7b1bbe8afa6e8bfb00a232320e8a18ce68385e9809fe8a7880a232320e69cace591a8e5b195e69c9b0a232320e58685e5aeb9e680bbe7bb930ae58886e7b1bbe8afa6e8bfb0e58faae68c89203620e4b8aae585b3e6b3a8e7bbb4e5baa6e7bb84e7bb87efbc88e8a18ce4b89ae58aa8e68081e38081e585b3e994aee58e82e59586e38081e8a18ce68385e4bbb7e6a0bce38081e9878de7bb84e5b9b6e8b4ade38081e585ace58fb8e6b2bbe79086e38081e5b195e4bc9ae4bfa1e681afefbc89efbc8ce4b88de8a681e58fa6e8b5b7e7acac20372d313020e4b8aae7bbb4e5baa6e38082', 'hex'), 'UTF8'),
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search
)
VALUES (
    'a3000001-0000-4000-8000-000000000012',
    'industry.news.briefing',
    '行业新闻简报',
    '每日上海 08:00 按昨天往前两日窗口生成元器件行业简报',
    'mock',
    'mock',
    'a2000001-0000-4000-8000-000000000012',
    0,
    jsonb_build_array(
        convert_from(decode('73746172745f64617465', 'hex'), 'UTF8'),
        convert_from(decode('656e645f64617465', 'hex'), 'UTF8')
    ),
    jsonb_build_array(
        convert_from(decode('73746172745f64617465', 'hex'), 'UTF8'),
        convert_from(decode('656e645f64617465', 'hex'), 'UTF8')
    ),
    8192,
    0.30,
    'biz.ai.industry_news.briefing',
    5,
    true,
    true
)
ON CONFLICT (code) DO NOTHING;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000ce', 'biz.ai.industry_news.briefing', 'AI-行业新闻简报', 'api', 'ai', 'industry_news', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.industry_news.briefing' AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260914160000_IndustryNewsBriefing', '9.0.11'
WHERE NOT EXISTS (
  SELECT 1 FROM public."__EFMigrationsHistory" h
  WHERE h."MigrationId" = '20260914160000_IndustryNewsBriefing'
);

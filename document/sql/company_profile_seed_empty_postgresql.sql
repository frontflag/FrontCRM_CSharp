-- =============================================================================
-- 公司信息（SysParam）空模板种子 — PostgreSQL
-- =============================================================================
-- 用途：公司信息页从 sysparam 读取 JSON；若库中无记录或 ValueJson 为空数组，前端
--  v-for 无行可渲染。本脚本为各段插入「一组默认空白」JSON（与 API DTO 字段一致，
--  camelCase，与 CompanyProfileController / CompanyProfileBundleLoader 一致）。
--
-- 执行：在业务库运行本文件；可重复执行（仅当对应 ParamCode 不存在时插入）。
-- 若已有错误数据，可先 DELETE FROM public.sysparam WHERE "ParamCode" LIKE 'Company.Profile.%';
--  再执行（会丢失已填内容，慎用）。
-- =============================================================================

-- DataType: 5 = Json（见 CRM.Core ParamDataType.Json）

-- 公司基础信息
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.BasicInfos',
  '公司基础信息（多组）',
  NULL,
  5,
  '[{"id":"11111111-1111-1111-1111-111111111101","isDefault":true,"enabled":true,"companyName":"","taxId":"","legalPerson":"","address":"","postalCode":"","phone":"","fax":"","email":""}]',
  TRUE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.BasicInfos');

-- 公司银行信息
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.BankInfos',
  '公司银行信息（多组）',
  NULL,
  5,
  '[{"id":"22222222-2222-2222-2222-222222222202","isDefault":true,"enabled":true,"bankName":"","accountName":"","bankAddress":"","swift":"","iban":"","bankCode":"","currency":"RMB","bankType":"rmb","purposeType":"payment","remark":""}]',
  TRUE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.BankInfos');

-- 公司 Logo
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.Logos',
  '公司Logo（多组）',
  NULL,
  5,
  '[{"id":"33333333-3333-3333-3333-333333333303","isDefault":true,"enabled":true,"logoName":"","documentId":null,"fileName":null}]',
  TRUE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.Logos');

-- 公司印章
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.Seals',
  '公司印章（多组）',
  NULL,
  5,
  '[{"id":"44444444-4444-4444-4444-444444444404","isDefault":true,"enabled":true,"sealName":"","useScene":"","documentId":null,"fileName":null}]',
  TRUE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.Seals');

-- 公司仓库信息
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.Warehouses',
  '公司仓库信息（多组）',
  NULL,
  5,
  '[{"id":"55555555-5555-5555-5555-555555555505","isDefault":true,"enabled":true,"warehouseName":"","address":"","contactName":"","contactPhone":"","workHours":""}]',
  TRUE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.Warehouses');

-- 公司邮箱（SMTP）— 单对象，非数组（与 UpsertSmtpEmailAsync 一致）
INSERT INTO public.sysparam (
  "ParamId", "ParamCode", "ParamName", "GroupId", "DataType", "ValueJson",
  "IsArray", "IsSystem", "IsEditable", "IsVisible", "SortOrder", "Status", "CreateTime"
)
SELECT
  gen_random_uuid()::text,
  'Company.Profile.SmtpEmail',
  '公司邮箱（SMTP 发信）',
  NULL,
  5,
  '{"enabled":false,"smtpHost":"","smtpPort":587,"user":null,"password":null,"fromAddress":"","fromName":"FrontCRM","useSsl":true}',
  FALSE, TRUE, TRUE, TRUE, 0, 1,
  NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam s WHERE s."ParamCode" = 'Company.Profile.SmtpEmail');

-- -----------------------------------------------------------------------------
-- 修复：行已存在但 ValueJson 为 []、空串或 NULL（与 INSERT…WHERE NOT EXISTS 不冲突）
-- -----------------------------------------------------------------------------
UPDATE public.sysparam SET
  "ValueJson" = '[{"id":"11111111-1111-1111-1111-111111111101","isDefault":true,"enabled":true,"companyName":"","taxId":"","legalPerson":"","address":"","postalCode":"","phone":"","fax":"","email":""}]',
  "DataType" = 5, "IsArray" = TRUE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.BasicInfos'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '[]');

UPDATE public.sysparam SET
  "ValueJson" = '[{"id":"22222222-2222-2222-2222-222222222202","isDefault":true,"enabled":true,"bankName":"","accountName":"","bankAddress":"","swift":"","iban":"","bankCode":"","currency":"RMB","bankType":"rmb","purposeType":"payment","remark":""}]',
  "DataType" = 5, "IsArray" = TRUE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.BankInfos'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '[]');

UPDATE public.sysparam SET
  "ValueJson" = '[{"id":"33333333-3333-3333-3333-333333333303","isDefault":true,"enabled":true,"logoName":"","documentId":null,"fileName":null}]',
  "DataType" = 5, "IsArray" = TRUE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.Logos'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '[]');

UPDATE public.sysparam SET
  "ValueJson" = '[{"id":"44444444-4444-4444-4444-444444444404","isDefault":true,"enabled":true,"sealName":"","useScene":"","documentId":null,"fileName":null}]',
  "DataType" = 5, "IsArray" = TRUE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.Seals'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '[]');

UPDATE public.sysparam SET
  "ValueJson" = '[{"id":"55555555-5555-5555-5555-555555555505","isDefault":true,"enabled":true,"warehouseName":"","address":"","contactName":"","contactPhone":"","workHours":""}]',
  "DataType" = 5, "IsArray" = TRUE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.Warehouses'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '[]');

UPDATE public.sysparam SET
  "ValueJson" = '{"enabled":false,"smtpHost":"","smtpPort":587,"user":null,"password":null,"fromAddress":"","fromName":"FrontCRM","useSsl":true}',
  "DataType" = 5, "IsArray" = FALSE, "ModifyTime" = NOW() AT TIME ZONE 'utc'
WHERE "ParamCode" = 'Company.Profile.SmtpEmail'
  AND ("ValueJson" IS NULL OR trim("ValueJson") = '' OR trim("ValueJson") = '{}');

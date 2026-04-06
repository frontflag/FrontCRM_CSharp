-- 客户/供应商：公司英文全称（与迁移 20260402120000 一致）
-- PostgreSQL

ALTER TABLE IF EXISTS public.customerinfo
  ADD COLUMN IF NOT EXISTS "EnglishOfficialName" character varying(128) NULL;

ALTER TABLE IF EXISTS public.vendorinfo
  ADD COLUMN IF NOT EXISTS "EnglishOfficialName" character varying(128) NULL;

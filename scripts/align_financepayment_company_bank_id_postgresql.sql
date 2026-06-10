-- financepayment：公司付款银行账户（付款环节写入）
ALTER TABLE public.financepayment
  ADD COLUMN IF NOT EXISTS "CompanyBankId" character varying(36) NULL;

CREATE INDEX IF NOT EXISTS "IX_financepayment_CompanyBankId"
  ON public.financepayment ("CompanyBankId");

COMMENT ON COLUMN public.financepayment."CompanyBankId" IS '公司银行账户主键（company_bankinfo.Id）';

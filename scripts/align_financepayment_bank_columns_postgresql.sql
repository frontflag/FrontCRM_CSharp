-- financepayment：请款/付款银行关联字段（VendorBankId + CompanyBankId）
-- 报错示例：
--   42703: 字段 f.VendorBankId 不存在
--   42703: 字段 f.CompanyBankId 不存在

ALTER TABLE IF EXISTS public.financepayment
  ADD COLUMN IF NOT EXISTS "VendorBankId" character varying(36) NULL;

COMMENT ON COLUMN public.financepayment."VendorBankId"
  IS '供应商银行账户 ID（vendorbankinfo.BankId）';

CREATE INDEX IF NOT EXISTS "IX_financepayment_VendorBankId"
  ON public.financepayment ("VendorBankId")
  WHERE "VendorBankId" IS NOT NULL;

ALTER TABLE IF EXISTS public.financepayment
  ADD COLUMN IF NOT EXISTS "CompanyBankId" character varying(36) NULL;

COMMENT ON COLUMN public.financepayment."CompanyBankId"
  IS '公司银行账户主键（company_bankinfo.Id）';

CREATE INDEX IF NOT EXISTS "IX_financepayment_CompanyBankId"
  ON public.financepayment ("CompanyBankId")
  WHERE "CompanyBankId" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260629150000_FinancePaymentVendorBankId', COALESCE(
  (SELECT "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1),
  '9.0.0'
)
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '20260629150000_FinancePaymentVendorBankId'
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260605120000_FinancePaymentCompanyBankId', COALESCE(
  (SELECT "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1),
  '9.0.0'
)
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '20260605120000_FinancePaymentCompanyBankId'
);

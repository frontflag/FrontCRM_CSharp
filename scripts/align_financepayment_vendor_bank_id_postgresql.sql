-- financepayment：请款关联供应商银行账户（VendorBankId）
-- 报错示例：42703: 关系 "financepayment" 的 "VendorBankId" 字段不存在
-- 与迁移 20260629150000_FinancePaymentVendorBankId 一致

ALTER TABLE IF EXISTS public.financepayment
  ADD COLUMN IF NOT EXISTS "VendorBankId" character varying(36) NULL;

COMMENT ON COLUMN public.financepayment."VendorBankId"
  IS '供应商银行账户 ID（vendorbankinfo.BankId）';

CREATE INDEX IF NOT EXISTS "IX_financepayment_VendorBankId"
  ON public.financepayment ("VendorBankId")
  WHERE "VendorBankId" IS NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20260629150000_FinancePaymentVendorBankId', COALESCE(
  (SELECT "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1),
  '9.0.0'
)
WHERE NOT EXISTS (
  SELECT 1 FROM "__EFMigrationsHistory"
  WHERE "MigrationId" = '20260629150000_FinancePaymentVendorBankId'
);

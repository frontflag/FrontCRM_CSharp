-- vendorbankinfo：关联财务参数「付款银行」主键（供应商资料开户银行下拉枚举）
ALTER TABLE IF EXISTS public.vendorbankinfo
  ADD COLUMN IF NOT EXISTS "FinancePaymentBankId" character varying(36) NULL;

COMMENT ON COLUMN public.vendorbankinfo."FinancePaymentBankId"
  IS '财务参数-付款银行主键（financepaymentbank.FinancePaymentBankId）；与 BankName 冗余展示，请款时默认供应商银行';

CREATE INDEX IF NOT EXISTS "IX_vendorbankinfo_FinancePaymentBankId"
  ON public.vendorbankinfo ("FinancePaymentBankId")
  WHERE "FinancePaymentBankId" IS NOT NULL;

-- 历史数据：按开户银行名称匹配已启用的付款银行
UPDATE public.vendorbankinfo vb
SET "FinancePaymentBankId" = fp."FinancePaymentBankId"
FROM public.financepaymentbank fp
WHERE vb."FinancePaymentBankId" IS NULL
  AND vb."BankName" IS NOT NULL
  AND TRIM(vb."BankName") <> ''
  AND LOWER(TRIM(vb."BankName")) = LOWER(TRIM(fp."BankName"))
  AND COALESCE(fp."IsDisabled", false) = false;

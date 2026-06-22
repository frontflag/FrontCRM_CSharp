-- 公司银行账户：是否出现在付款窗口「付款银行」下拉
ALTER TABLE public.company_bankinfo
  ADD COLUMN IF NOT EXISTS available_for_payment boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.company_bankinfo.available_for_payment IS '可用付款：勾选后出现在付款单付款银行下拉';

-- 与旧逻辑对齐：已启用且用途为付款的账户默认视为可用付款
UPDATE public.company_bankinfo
SET available_for_payment = true
WHERE enabled = true
  AND lower(trim(purpose_type)) = 'payment';

-- 验证（应能看到 available_for_payment 列及 true/false）
-- SELECT bank_name, purpose_type, enabled, available_for_payment FROM public.company_bankinfo ORDER BY sort_order;

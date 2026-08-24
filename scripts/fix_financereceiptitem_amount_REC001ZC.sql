-- REC001ZC：单头 3239.48、明细误为 1.00（确认前改单头未同步明细；现网 UpdateAsync 已会同步默认明细）。
-- 已确认单不能再编辑，故按单头回写明细金额与折算金额。未核销、未转预收。可重复执行。

UPDATE public.financereceiptitem i
SET "ReceiptAmount" = r."ReceiptAmount",
    "ReceiptConvertAmount" = r."ReceiptAmount",
    "ModifyTime" = timezone('utc', now())
FROM public.financereceipt r
WHERE r."FinanceReceiptCode" = 'REC001ZC'
  AND i."FinanceReceiptId" = r."FinanceReceiptId"
  AND COALESCE(i.is_deleted, false) = false
  AND i."VerificationStatus" = 0
  AND i."VerifiedAmount" = 0
  AND COALESCE(i.advance_pool_amount, 0) = 0
  AND (
    i."ReceiptAmount" IS DISTINCT FROM r."ReceiptAmount"
    OR i."ReceiptConvertAmount" IS DISTINCT FROM r."ReceiptAmount"
  );

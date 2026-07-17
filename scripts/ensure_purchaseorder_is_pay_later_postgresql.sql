-- =============================================================================
-- 采购订单主表增加 is_pay_later（后付款标记，可重复执行）
-- 对应 EF 迁移：20260717120000_PurchaseOrderIsPayLater
--
-- 业务含义：先欠供应商款，等客户付款后再给供应商结款。
-- 仅主单标记提醒，不拦截申请付款；与供应商账期无关。
-- =============================================================================

ALTER TABLE IF EXISTS public.purchaseorder
  ADD COLUMN IF NOT EXISTS is_pay_later boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.purchaseorder.is_pay_later IS
  '后付款：客户付款后再给供应商付款（仅标记提醒，不拦截申请付款）';

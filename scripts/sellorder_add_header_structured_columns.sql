-- 销售订单主表：订单信息区拆分为独立列（与迁移 20260710140000_SellOrderHeaderStructuredRemarks 一致）
-- 可在 Navicat / psql 中单独执行；若已通过 EF 迁移应用则可跳过。

ALTER TABLE IF EXISTS public.sellorder
  ADD COLUMN IF NOT EXISTS product_kind character varying(64) NULL,
  ADD COLUMN IF NOT EXISTS customer_contact_name character varying(200) NULL,
  ADD COLUMN IF NOT EXISTS invoice_info character varying(500) NULL,
  ADD COLUMN IF NOT EXISTS payment_terms_text character varying(500) NULL,
  ADD COLUMN IF NOT EXISTS order_remark character varying(500) NULL;

COMMENT ON COLUMN public.sellorder.product_kind IS '产品类型（现货/期货/排单/样品等）';
COMMENT ON COLUMN public.sellorder.customer_contact_name IS '客户联系人（展示名或手工填写）';
COMMENT ON COLUMN public.sellorder.invoice_info IS '发票信息（公司、税号等）';
COMMENT ON COLUMN public.sellorder.payment_terms_text IS '账期/付款条款（展示文案）';
COMMENT ON COLUMN public.sellorder.order_remark IS '订单自由备注（不含结构化前缀行）';
COMMENT ON COLUMN public.sellorder.comment IS '已弃用：历史多行拼接订单信息；新单不再写入，由应用解析后写入独立列';

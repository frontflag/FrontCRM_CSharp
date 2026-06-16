-- RFQ 明细：关联 biz_brand 供应品牌 ID
ALTER TABLE public.rfqitem
  ADD COLUMN IF NOT EXISTS brand_id BIGINT NULL;

COMMENT ON COLUMN public.rfqitem.brand_id IS '供应品牌ID（关联 biz_brand.id）';

CREATE INDEX IF NOT EXISTS "IX_rfqitem_brand_id"
  ON public.rfqitem (brand_id);

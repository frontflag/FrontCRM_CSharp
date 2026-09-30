-- 报关明细行备注（费用面板整行展示；最长 1000）
ALTER TABLE public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS remark character varying(1000) NULL;

COMMENT ON COLUMN public.customs_declaration_item.remark IS '报关费用行备注；作废只读；已完成/锁定仅管理员可改';

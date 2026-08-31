-- =============================================================================
-- 报关公司装箱单收货人字段（联系人 / 电话 / 邮箱 / 地址）
-- 说明：document/System/报关/报关公司装箱单收货人-设计与实现.md
-- 对应 EF：20260901080000_CustomsBrokerPrintConsignee
-- 幂等：ADD COLUMN IF NOT EXISTS
-- =============================================================================

BEGIN;

ALTER TABLE public.customs_broker
    ADD COLUMN IF NOT EXISTS contact_name character varying(100) NULL,
    ADD COLUMN IF NOT EXISTS tel character varying(64) NULL,
    ADD COLUMN IF NOT EXISTS email character varying(200) NULL,
    ADD COLUMN IF NOT EXISTS address character varying(500) NULL;

COMMENT ON COLUMN public.customs_broker.contact_name IS '装箱单收货人联系人';
COMMENT ON COLUMN public.customs_broker.tel IS '装箱单收货人电话';
COMMENT ON COLUMN public.customs_broker.email IS '装箱单收货人邮箱';
COMMENT ON COLUMN public.customs_broker.address IS '装箱单收货人地址（按需印出的原文）';

COMMIT;

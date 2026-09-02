-- =============================================================================
-- 报关单头：代理费率系统/手工来源
-- 说明：document/System/报关/报关费用方案.md
-- 对应 EF：20260903160000_CustomsDeclarationAgencyRateManual
-- 幂等：ADD COLUMN IF NOT EXISTS
-- =============================================================================

BEGIN;

ALTER TABLE public.customs_declaration
    ADD COLUMN IF NOT EXISTS agency_rate_manual boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.customs_declaration.agency_rate_manual IS '代理费率来源：false=报关公司资料，true=本单手工；换报关公司时强制回 false';
COMMENT ON COLUMN public.customs_declaration.broker_agency_rate IS '代理费率快照（1+纯费率）。系统模式试算时从 customs_broker.agency_rate 覆盖；手工模式保留本列';

COMMIT;

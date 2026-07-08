-- =============================================================================
-- 报关费用 F1 — PostgreSQL DDL（主数据 + 扩展列）
-- 说明文档：document/System/报关/报关费用方案.md §4、报关V2_DDL §16
-- 依赖：customs_broker、customs_declaration、customs_declaration_item 已存在
-- 执行前：备份数据库
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. 采购系数主数据 purchase_cost_param
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.purchase_cost_param (
    id                  character varying(36)   NOT NULL,
    ratio               numeric(10,4)           NOT NULL,
    start_time          timestamp with time zone NOT NULL,
    remark              character varying(500)  NULL,
    is_deleted          boolean                 NOT NULL DEFAULT false,
    create_time         timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time         timestamp with time zone NULL,
    create_by_user_id   character varying(36)   NULL,
    modify_by_user_id   character varying(36)   NULL,
    CONSTRAINT "PK_purchase_cost_param" PRIMARY KEY (id),
    CONSTRAINT "CK_purchase_cost_param_ratio" CHECK (ratio > 0)
);

CREATE INDEX IF NOT EXISTS "IX_purchase_cost_param_effective"
    ON public.purchase_cost_param (start_time DESC, create_time DESC)
    WHERE is_deleted = false;

COMMENT ON TABLE public.purchase_cost_param IS '采购报关系数（全局）；对齐 EBS PurchaseCostParam';
COMMENT ON COLUMN public.purchase_cost_param.ratio IS '采购系数乘数，如 1.0000、1.0500';
COMMENT ON COLUMN public.purchase_cost_param.start_time IS '生效开始时间（UTC）；取 start_time<=当前 最新一条';

-- ---------------------------------------------------------------------------
-- 2. 采购系数变更日志
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.purchase_cost_param_change_log (
    id                  character varying(36)   NOT NULL,
    purchase_cost_param_id character varying(36) NULL,
    ratio               numeric(10,4)           NOT NULL,
    start_time          timestamp with time zone NOT NULL,
    change_user_id      character varying(36)   NULL,
    change_user_name    character varying(100)  NULL,
    change_summary      character varying(500)  NULL,
    create_time         timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT "PK_purchase_cost_param_change_log" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS "IX_purchase_cost_param_change_log_time"
    ON public.purchase_cost_param_change_log (create_time DESC);

-- ---------------------------------------------------------------------------
-- 3. customs_broker：代理费率（1+纯费率，如 1.03）
-- ---------------------------------------------------------------------------
ALTER TABLE public.customs_broker
    ADD COLUMN IF NOT EXISTS agency_rate numeric(10,6) NOT NULL DEFAULT 1;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_customs_broker_agency_rate') THEN
        ALTER TABLE public.customs_broker
            ADD CONSTRAINT "CK_customs_broker_agency_rate" CHECK (agency_rate >= 1);
    END IF;
END $$;

COMMENT ON COLUMN public.customs_broker.agency_rate IS '报关代理费率：1+纯费率，代理费=基数×(agency_rate-1)';

-- ---------------------------------------------------------------------------
-- 4. customs_declaration：费用头扩展
-- ---------------------------------------------------------------------------
ALTER TABLE public.customs_declaration
    ADD COLUMN IF NOT EXISTS broker_agency_rate numeric(10,6) NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS fees_calculated_at timestamp with time zone NULL,
    ADD COLUMN IF NOT EXISTS fees_locked boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.customs_declaration."ExchangeRate" IS '报关汇率 USD→CNY，关务手工维护';
COMMENT ON COLUMN public.customs_declaration.broker_agency_rate IS '试算时快照 customs_broker.agency_rate';
COMMENT ON COLUMN public.customs_declaration.fees_calculated_at IS '最后一次成功费用试算时间（UTC）';
COMMENT ON COLUMN public.customs_declaration.fees_locked IS '费用锁定（可选，结关后仅允许改杂费/商检）';

-- ---------------------------------------------------------------------------
-- 5. customs_declaration_item：费用行扩展
-- ---------------------------------------------------------------------------
ALTER TABLE public.customs_declaration_item
    ADD COLUMN IF NOT EXISTS purchase_cost_param_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS purchase_ratio numeric(10,4) NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS purchase_currency smallint NULL,
    ADD COLUMN IF NOT EXISTS cost_usd numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS duty_rate numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS vat_rate numeric(18,6) NOT NULL DEFAULT 0.13,
    ADD COLUMN IF NOT EXISTS customs_usd_price numeric(18,6) NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_cdi_purchase_cost_param') THEN
        ALTER TABLE public.customs_declaration_item
            ADD CONSTRAINT "FK_cdi_purchase_cost_param"
            FOREIGN KEY (purchase_cost_param_id)
            REFERENCES public.purchase_cost_param (id)
            ON DELETE RESTRICT;
    END IF;
END $$;

COMMENT ON COLUMN public.customs_declaration_item.purchase_cost_param_id IS '试算时使用的采购系数配置 Id';
COMMENT ON COLUMN public.customs_declaration_item.purchase_ratio IS '采购系数快照';
COMMENT ON COLUMN public.customs_declaration_item.purchase_currency IS '采购币别快照（CurrencyCode）';
COMMENT ON COLUMN public.customs_declaration_item.cost_usd IS '采购美金价 CostUSD，含系数';
COMMENT ON COLUMN public.customs_declaration_item.duty_rate IS '关税税率 CustomsDutyRate';
COMMENT ON COLUMN public.customs_declaration_item.vat_rate IS '增值税率，默认 0.13';
COMMENT ON COLUMN public.customs_declaration_item.customs_usd_price IS '报关美金价 CustomsUSDPrice';

-- ---------------------------------------------------------------------------
-- 6. 初始种子：默认系数 1.0（仅当无任何未删记录时）
-- ---------------------------------------------------------------------------
INSERT INTO public.purchase_cost_param (id, ratio, start_time, remark, is_deleted, create_time)
SELECT
    '00000000-0000-4000-8000-0000000000f1',
    1.0000,
    TIMESTAMPTZ '2020-01-01 00:00:00+00',
    'F1 初始默认采购系数',
    false,
    (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (
    SELECT 1 FROM public.purchase_cost_param WHERE is_deleted = false
);

COMMIT;

-- =============================================================================
-- Down（慎用）
-- =============================================================================
-- ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_purchase_cost_param";
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_usd_price;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS vat_rate;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS duty_rate;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS cost_usd;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_currency;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_ratio;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_cost_param_id;
-- ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS fees_locked;
-- ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS fees_calculated_at;
-- ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS broker_agency_rate;
-- ALTER TABLE public.customs_broker DROP CONSTRAINT IF EXISTS "CK_customs_broker_agency_rate";
-- ALTER TABLE public.customs_broker DROP COLUMN IF EXISTS agency_rate;
-- DROP TABLE IF EXISTS public.purchase_cost_param_change_log;
-- DROP TABLE IF EXISTS public.purchase_cost_param;

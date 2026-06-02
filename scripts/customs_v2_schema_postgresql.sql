-- =============================================================================
-- 报关 V2 — PostgreSQL DDL（结构变更）
-- 说明文档：document/实现方案/报关V2_DDL与表结构.md
-- 执行前：备份数据库；建议在事务外逐段执行并验证。
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. 新建：customs_pendlist（待报关列表）
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.customs_pendlist (
    id                          character varying(36)  NOT NULL,
    sales_stockout_notify_id    character varying(36)  NOT NULL,
    sell_order_item_id          character varying(36)  NOT NULL,
    qty                         integer                NOT NULL,
    status                      smallint               NOT NULL DEFAULT 1,
    customs_stockout_notify_id  character varying(36)  NULL,
    overseas_warehouse_id       character varying(36)  NULL,
    create_time                 timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time                 timestamp with time zone NULL,
    create_by_user_id           character varying(36)  NULL,
    modify_by_user_id           character varying(36)  NULL,
    is_deleted                  boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_customs_pendlist" PRIMARY KEY (id),
    CONSTRAINT "FK_customs_pendlist_sales_sor"
        FOREIGN KEY (sales_stockout_notify_id)
        REFERENCES public.stockout_notify ("ID")
        ON DELETE RESTRICT,
    CONSTRAINT "FK_customs_pendlist_customs_sor"
        FOREIGN KEY (customs_stockout_notify_id)
        REFERENCES public.stockout_notify ("ID")
        ON DELETE RESTRICT,
    CONSTRAINT "CK_customs_pendlist_qty" CHECK (qty > 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_customs_pendlist_sales_sor"
    ON public.customs_pendlist (sales_stockout_notify_id)
    WHERE is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_customs_pendlist_customs_sor"
    ON public.customs_pendlist (customs_stockout_notify_id)
    WHERE is_deleted = false AND customs_stockout_notify_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS "IX_customs_pendlist_sell_line"
    ON public.customs_pendlist (sell_order_item_id)
    WHERE is_deleted = false;

COMMENT ON TABLE public.customs_pendlist IS '待报关列表：与销售出库通知 1:1；无独立业务单号';
COMMENT ON COLUMN public.customs_pendlist.status IS '1=Open 2=CustomsOutNotifyCreated 3=InCustomsProcess 10=Closed -1=Cancelled';
COMMENT ON COLUMN public.customs_pendlist.sales_stockout_notify_id IS '销售出库通知 stockout_notify.ID（StockOutType=10）';
COMMENT ON COLUMN public.customs_pendlist.customs_stockout_notify_id IS '报关出库通知 stockout_notify.ID（StockOutType=20）';

-- ---------------------------------------------------------------------------
-- 2. stockout_notify：报关出库通知关联 pendlist
-- ---------------------------------------------------------------------------
ALTER TABLE public.stockout_notify
    ADD COLUMN IF NOT EXISTS customs_pendlist_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockout_notify_customs_pendlist'
    ) THEN
        ALTER TABLE public.stockout_notify
            ADD CONSTRAINT "FK_stockout_notify_customs_pendlist"
            FOREIGN KEY (customs_pendlist_id)
            REFERENCES public.customs_pendlist (id)
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_stockout_notify_customs_pendlist"
    ON public.stockout_notify (customs_pendlist_id)
    WHERE is_deleted = false AND customs_pendlist_id IS NOT NULL;

COMMENT ON COLUMN public.stockout_notify."Status" IS '出库通知状态：-1取消 5待报关 10待装箱 20已装箱 100已出库';
COMMENT ON COLUMN public.stockout_notify.customs_pendlist_id IS '仅 StockOutType=20（报关出库通知）使用，指向 customs_pendlist';

-- ---------------------------------------------------------------------------
-- 3. packing：报关公司 + 报关记录反向关联
-- ---------------------------------------------------------------------------
ALTER TABLE public.packing
    ADD COLUMN IF NOT EXISTS customs_broker_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS customs_declaration_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_customs_broker') THEN
        ALTER TABLE public.packing
            ADD CONSTRAINT "FK_packing_customs_broker"
            FOREIGN KEY (customs_broker_id) REFERENCES public.customs_broker ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_packing_customs_declaration"
    ON public.packing (customs_declaration_id)
    WHERE is_deleted = false AND customs_declaration_id IS NOT NULL;

COMMENT ON COLUMN public.packing.customs_broker_id IS '报关装箱单：报关公司 customs_broker.Id（不用 customer_id）';
COMMENT ON COLUMN public.packing.customs_declaration_id IS '装箱确认后关联 customs_declaration（1:1）';

-- ---------------------------------------------------------------------------
-- 4. packing_item：冗余 pendlist（主绑定仍为 stockout_notify_id = 报关出库通知）
-- ---------------------------------------------------------------------------
ALTER TABLE public.packing_item
    ADD COLUMN IF NOT EXISTS customs_pendlist_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_item_customs_pendlist') THEN
        ALTER TABLE public.packing_item
            ADD CONSTRAINT "FK_packing_item_customs_pendlist"
            FOREIGN KEY (customs_pendlist_id) REFERENCES public.customs_pendlist (id)
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_packing_item_customs_pendlist"
    ON public.packing_item (customs_pendlist_id)
    WHERE is_deleted = false AND customs_pendlist_id IS NOT NULL;

COMMENT ON COLUMN public.packing_item.stockout_notify_id IS '报关装箱：1:1 绑定报关出库通知（StockOutType=20）';
COMMENT ON COLUMN public.packing_item.customs_pendlist_id IS '冗余 FK，便于回退与溯源';

-- ---------------------------------------------------------------------------
-- 5. customs_declaration：去掉 StockOutRequestId，增加 packing_id
-- ---------------------------------------------------------------------------
ALTER TABLE public.customs_declaration
    ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

-- 删除旧 1:1 销售出库通知约束（V2 改由明细 + pendlist 关联）
ALTER TABLE public.customs_declaration
    DROP CONSTRAINT IF EXISTS "FK_customs_declaration_sor";

DROP INDEX IF EXISTS public."IX_customs_declaration_StockOutRequestId";

ALTER TABLE public.customs_declaration
    DROP COLUMN IF EXISTS "StockOutRequestId";

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_customs_declaration_packing') THEN
        ALTER TABLE public.customs_declaration
            ADD CONSTRAINT "FK_customs_declaration_packing"
            FOREIGN KEY (packing_id) REFERENCES public.packing ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_customs_declaration_packing"
    ON public.customs_declaration (packing_id)
    WHERE is_deleted = false AND packing_id IS NOT NULL;

COMMENT ON COLUMN public.customs_declaration.packing_id IS '报关装箱单 1:1；确认装箱时写入';
COMMENT ON COLUMN public.customs_declaration."FromWarehouseId" IS '由明细源 stock_item 境外仓自动带出';
COMMENT ON COLUMN public.customs_declaration."ToWarehouseId" IS '境内目标仓，关务手动选择';

-- packing.customs_declaration_id 反向 FK（在 declaration 存在后添加）
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_customs_declaration') THEN
        ALTER TABLE public.packing
            ADD CONSTRAINT "FK_packing_customs_declaration"
            FOREIGN KEY (customs_declaration_id)
            REFERENCES public.customs_declaration ("CustomsDeclarationId")
            ON DELETE RESTRICT;
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 6. customs_declaration_item：V2 关联列 + 拣货前可空源在库行
-- ---------------------------------------------------------------------------
ALTER TABLE public.customs_declaration_item
    ALTER COLUMN "SourceStockItemId" DROP NOT NULL;

ALTER TABLE public.customs_declaration_item
    ADD COLUMN IF NOT EXISTS customs_pendlist_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS customs_stockout_notify_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS packing_item_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS original_purchase_price numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS vendor_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_cdi_customs_pendlist') THEN
        ALTER TABLE public.customs_declaration_item
            ADD CONSTRAINT "FK_cdi_customs_pendlist"
            FOREIGN KEY (customs_pendlist_id) REFERENCES public.customs_pendlist (id)
            ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_cdi_customs_sor') THEN
        ALTER TABLE public.customs_declaration_item
            ADD CONSTRAINT "FK_cdi_customs_sor"
            FOREIGN KEY (customs_stockout_notify_id) REFERENCES public.stockout_notify ("ID")
            ON DELETE RESTRICT;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_cdi_packing_item') THEN
        ALTER TABLE public.customs_declaration_item
            ADD CONSTRAINT "FK_cdi_packing_item"
            FOREIGN KEY (packing_item_id) REFERENCES public.packing_item ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_cdi_packing_item"
    ON public.customs_declaration_item (packing_item_id)
    WHERE is_deleted = false AND packing_item_id IS NOT NULL;

COMMENT ON COLUMN public.customs_declaration_item."SourceStockItemId" IS '拣货后回写；装箱生成报关明细时可为空';
COMMENT ON COLUMN public.customs_declaration_item."StockOutRequestId" IS '销售出库通知 stockout_notify.ID（Type=10）';
COMMENT ON COLUMN public.customs_declaration_item.original_purchase_price IS '原始采购价 P0 快照';
COMMENT ON COLUMN public.customs_declaration_item."TaxIncludedUnitPrice" IS '报关采购价 P1（含税单价）';

-- ---------------------------------------------------------------------------
-- 7. stock_out_item_extend：报关溯源与 P0
-- ---------------------------------------------------------------------------
ALTER TABLE public.stock_out_item_extend
    ADD COLUMN IF NOT EXISTS original_purchase_price numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS vendor_id character varying(36) NULL,
    ADD COLUMN IF NOT EXISTS customs_declaration_item_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_sox_extend_cdi') THEN
        ALTER TABLE public.stock_out_item_extend
            ADD CONSTRAINT "FK_sox_extend_cdi"
            FOREIGN KEY (customs_declaration_item_id)
            REFERENCES public.customs_declaration_item ("CustomsDeclarationItemId")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_sox_extend_cdi"
    ON public.stock_out_item_extend (customs_declaration_item_id)
    WHERE is_deleted = false AND customs_declaration_item_id IS NOT NULL;

COMMENT ON COLUMN public.stock_out_item_extend.original_purchase_price IS '原始采购价 P0';
COMMENT ON COLUMN public.stock_out_item_extend."PurchasePrice" IS '报关出库=P0；销售出库=P1（报关采购价）';
COMMENT ON COLUMN public.stock_out_item_extend.vendor_id IS '原始供应商';
COMMENT ON COLUMN public.stock_out_item_extend.customs_declaration_item_id IS '报关明细溯源 FK';

-- ---------------------------------------------------------------------------
-- 8. stockin_notify：报关到货关联报关明细
-- ---------------------------------------------------------------------------
ALTER TABLE public.stockin_notify
    ADD COLUMN IF NOT EXISTS customs_declaration_item_id character varying(36) NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockin_notify_cdi') THEN
        ALTER TABLE public.stockin_notify
            ADD CONSTRAINT "FK_stockin_notify_cdi"
            FOREIGN KEY (customs_declaration_item_id)
            REFERENCES public.customs_declaration_item ("CustomsDeclarationItemId")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_stockin_notify_cdi"
    ON public.stockin_notify (customs_declaration_item_id)
    WHERE is_deleted = false AND customs_declaration_item_id IS NOT NULL;

COMMENT ON COLUMN public.stockin_notify.customs_declaration_item_id IS '报关到货：从报关明细发起（StockInType=20）';

COMMIT;

-- =============================================================================
-- 回滚参考（Down）— 生产慎用；按环境评估后手工执行
-- =============================================================================
-- BEGIN;
-- ALTER TABLE public.stockin_notify DROP CONSTRAINT IF EXISTS "FK_stockin_notify_cdi";
-- DROP INDEX IF EXISTS "UX_stockin_notify_cdi";
-- ALTER TABLE public.stockin_notify DROP COLUMN IF EXISTS customs_declaration_item_id;
--
-- ALTER TABLE public.stock_out_item_extend DROP CONSTRAINT IF EXISTS "FK_sox_extend_cdi";
-- DROP INDEX IF EXISTS "IX_sox_extend_cdi";
-- ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS customs_declaration_item_id;
-- ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS vendor_id;
-- ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS original_purchase_price;
--
-- DROP INDEX IF EXISTS "UX_cdi_packing_item";
-- ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_packing_item";
-- ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_customs_sor";
-- ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_customs_pendlist";
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS vendor_id;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS original_purchase_price;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS packing_item_id;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_stockout_notify_id;
-- ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_pendlist_id;
-- ALTER TABLE public.customs_declaration_item ALTER COLUMN "SourceStockItemId" SET NOT NULL;
--
-- ALTER TABLE public.packing DROP CONSTRAINT IF EXISTS "FK_packing_customs_declaration";
-- ALTER TABLE public.customs_declaration DROP CONSTRAINT IF EXISTS "FK_customs_declaration_packing";
-- DROP INDEX IF EXISTS "UX_customs_declaration_packing";
-- ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS packing_id;
-- ALTER TABLE public.customs_declaration ADD COLUMN IF NOT EXISTS "StockOutRequestId" character varying(36);
--
-- ALTER TABLE public.packing_item DROP CONSTRAINT IF EXISTS "FK_packing_item_customs_pendlist";
-- DROP INDEX IF EXISTS "UX_packing_item_customs_pendlist";
-- ALTER TABLE public.packing_item DROP COLUMN IF EXISTS customs_pendlist_id;
--
-- ALTER TABLE public.packing DROP CONSTRAINT IF EXISTS "FK_packing_customs_broker";
-- DROP INDEX IF EXISTS "UX_packing_customs_declaration";
-- ALTER TABLE public.packing DROP COLUMN IF EXISTS customs_declaration_id;
-- ALTER TABLE public.packing DROP COLUMN IF EXISTS customs_broker_id;
--
-- ALTER TABLE public.stockout_notify DROP CONSTRAINT IF EXISTS "FK_stockout_notify_customs_pendlist";
-- DROP INDEX IF EXISTS "UX_stockout_notify_customs_pendlist";
-- ALTER TABLE public.stockout_notify DROP COLUMN IF EXISTS customs_pendlist_id;
--
-- DROP TABLE IF EXISTS public.customs_pendlist;
-- COMMIT;

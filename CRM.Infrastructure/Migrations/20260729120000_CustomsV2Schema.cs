using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关 V2 表结构：customs_pendlist、packing/declaration 关联列、移除 customs_declaration.StockOutRequestId。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260729120000_CustomsV2Schema")]
    public partial class CustomsV2Schema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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

                ALTER TABLE public.customs_declaration
                    ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

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
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.stockin_notify DROP CONSTRAINT IF EXISTS "FK_stockin_notify_cdi";
                DROP INDEX IF EXISTS "UX_stockin_notify_cdi";
                ALTER TABLE public.stockin_notify DROP COLUMN IF EXISTS customs_declaration_item_id;

                ALTER TABLE public.stock_out_item_extend DROP CONSTRAINT IF EXISTS "FK_sox_extend_cdi";
                DROP INDEX IF EXISTS "IX_sox_extend_cdi";
                ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS customs_declaration_item_id;
                ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS vendor_id;
                ALTER TABLE public.stock_out_item_extend DROP COLUMN IF EXISTS original_purchase_price;

                DROP INDEX IF EXISTS "UX_cdi_packing_item";
                ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_packing_item";
                ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_customs_sor";
                ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_customs_pendlist";
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS vendor_id;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS original_purchase_price;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS packing_item_id;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_stockout_notify_id;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_pendlist_id;
                ALTER TABLE public.customs_declaration_item ALTER COLUMN "SourceStockItemId" SET NOT NULL;

                ALTER TABLE public.packing DROP CONSTRAINT IF EXISTS "FK_packing_customs_declaration";
                ALTER TABLE public.customs_declaration DROP CONSTRAINT IF EXISTS "FK_customs_declaration_packing";
                DROP INDEX IF EXISTS "UX_customs_declaration_packing";
                ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS packing_id;
                ALTER TABLE public.customs_declaration ADD COLUMN IF NOT EXISTS "StockOutRequestId" character varying(36);

                ALTER TABLE public.packing_item DROP CONSTRAINT IF EXISTS "FK_packing_item_customs_pendlist";
                DROP INDEX IF EXISTS "UX_packing_item_customs_pendlist";
                ALTER TABLE public.packing_item DROP COLUMN IF EXISTS customs_pendlist_id;

                ALTER TABLE public.packing DROP CONSTRAINT IF EXISTS "FK_packing_customs_broker";
                DROP INDEX IF EXISTS "UX_packing_customs_declaration";
                ALTER TABLE public.packing DROP COLUMN IF EXISTS customs_declaration_id;
                ALTER TABLE public.packing DROP COLUMN IF EXISTS customs_broker_id;

                ALTER TABLE public.stockout_notify DROP CONSTRAINT IF EXISTS "FK_stockout_notify_customs_pendlist";
                DROP INDEX IF EXISTS "UX_stockout_notify_customs_pendlist";
                ALTER TABLE public.stockout_notify DROP COLUMN IF EXISTS customs_pendlist_id;

                DROP TABLE IF EXISTS public.customs_pendlist;
                """);
        }
    }
}

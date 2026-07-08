using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关费用 F1：purchase_cost_param、broker.agency_rate、declaration/item 费用扩展列。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260807120000_CustomsFeesF1Schema")]
    public partial class CustomsFeesF1Schema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
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

                ALTER TABLE public.customs_broker
                    ADD COLUMN IF NOT EXISTS agency_rate numeric(10,6) NOT NULL DEFAULT 1;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'CK_customs_broker_agency_rate') THEN
                        ALTER TABLE public.customs_broker
                            ADD CONSTRAINT "CK_customs_broker_agency_rate" CHECK (agency_rate >= 1);
                    END IF;
                END $$;

                ALTER TABLE public.customs_declaration
                    ADD COLUMN IF NOT EXISTS broker_agency_rate numeric(10,6) NOT NULL DEFAULT 1,
                    ADD COLUMN IF NOT EXISTS fees_calculated_at timestamp with time zone NULL,
                    ADD COLUMN IF NOT EXISTS fees_locked boolean NOT NULL DEFAULT false;

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
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_declaration_item DROP CONSTRAINT IF EXISTS "FK_cdi_purchase_cost_param";
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS customs_usd_price;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS vat_rate;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS duty_rate;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS cost_usd;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_currency;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_ratio;
                ALTER TABLE public.customs_declaration_item DROP COLUMN IF EXISTS purchase_cost_param_id;
                ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS fees_locked;
                ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS fees_calculated_at;
                ALTER TABLE public.customs_declaration DROP COLUMN IF EXISTS broker_agency_rate;
                ALTER TABLE public.customs_broker DROP CONSTRAINT IF EXISTS "CK_customs_broker_agency_rate";
                ALTER TABLE public.customs_broker DROP COLUMN IF EXISTS agency_rate;
                DROP TABLE IF EXISTS public.purchase_cost_param_change_log;
                DROP TABLE IF EXISTS public.purchase_cost_param;
                """);
        }
    }
}

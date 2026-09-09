using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260910120000_CommissionPoolAndCalcMonth")]
public partial class CommissionPoolAndCalcMonth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.commission_pool (
              id character varying(36) NOT NULL,
              stock_out_item_id character varying(36) NOT NULL,
              stock_out_id character varying(36) NOT NULL,
              stock_out_code character varying(32) NOT NULL,
              stock_out_date date NULL,
              sales_user_id character varying(36) NULL,
              purchase_user_id character varying(36) NULL,
              gp_usd numeric(18,2) NOT NULL DEFAULT 0,
              receipt_progress_status smallint NOT NULL DEFAULT 0,
              receipt_date date NULL,
              sell_order_id character varying(36) NULL,
              sell_order_code character varying(32) NULL,
              sell_order_item_id character varying(36) NULL,
              sell_order_item_code character varying(64) NULL,
              purchase_order_id character varying(36) NULL,
              purchase_order_code character varying(32) NULL,
              purchase_order_item_id character varying(36) NULL,
              purchase_order_item_code character varying(64) NULL,
              sales_commission_status smallint NOT NULL DEFAULT 0,
              purchase_commission_status smallint NOT NULL DEFAULT 0,
              created_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_at timestamp with time zone NULL,
              CONSTRAINT "PK_commission_pool" PRIMARY KEY (id),
              CONSTRAINT "UQ_commission_pool_stock_out_item" UNIQUE (stock_out_item_id),
              CONSTRAINT "CK_commission_pool_sales_flag" CHECK (sales_commission_status IN (0, 1)),
              CONSTRAINT "CK_commission_pool_purchase_flag" CHECK (purchase_commission_status IN (0, 1, 2))
            );

            CREATE INDEX IF NOT EXISTS "IX_commission_pool_sell_order_item"
              ON public.commission_pool (sell_order_item_id);

            CREATE INDEX IF NOT EXISTS "IX_commission_pool_sales_status"
              ON public.commission_pool (sales_commission_status);

            CREATE INDEX IF NOT EXISTS "IX_commission_pool_purchase_status"
              ON public.commission_pool (purchase_commission_status);

            ALTER TABLE public.commission_dynamic
              ADD COLUMN IF NOT EXISTS calc_month character varying(7) NOT NULL DEFAULT '';

            ALTER TABLE public.commission_dynamic
              ADD COLUMN IF NOT EXISTS entry_kind smallint NOT NULL DEFAULT 0;

            ALTER TABLE public.commission_locked
              ADD COLUMN IF NOT EXISTS calc_month character varying(7) NOT NULL DEFAULT '';

            ALTER TABLE public.commission_locked
              ADD COLUMN IF NOT EXISTS entry_kind smallint NOT NULL DEFAULT 0;

            CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_user_month"
              ON public.commission_dynamic (role_type, user_id, calc_month);

            CREATE INDEX IF NOT EXISTS "IX_commission_locked_role_user_month"
              ON public.commission_locked (role_type, user_id, calc_month);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public."IX_commission_dynamic_role_user_month";
            DROP INDEX IF EXISTS public."IX_commission_locked_role_user_month";
            ALTER TABLE public.commission_dynamic DROP COLUMN IF EXISTS calc_month;
            ALTER TABLE public.commission_dynamic DROP COLUMN IF EXISTS entry_kind;
            ALTER TABLE public.commission_locked DROP COLUMN IF EXISTS calc_month;
            ALTER TABLE public.commission_locked DROP COLUMN IF EXISTS entry_kind;
            DROP TABLE IF EXISTS public.commission_pool;
            """);
    }
}

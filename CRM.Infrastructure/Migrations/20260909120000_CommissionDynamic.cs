using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260909120000_CommissionDynamic")]
public partial class CommissionDynamic : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.commission_dynamic (
              id character varying(36) NOT NULL,
              role_type smallint NOT NULL,
              stock_out_item_id character varying(36) NOT NULL,
              user_id character varying(36) NOT NULL,
              user_name character varying(50) NOT NULL,
              user_level smallint NOT NULL,
              version_id character varying(36) NULL,
              rate_points numeric(5,2) NOT NULL DEFAULT 0,
              gp_usd numeric(18,2) NOT NULL DEFAULT 0,
              period_gp_usd numeric(18,2) NOT NULL DEFAULT 0,
              commission_usd numeric(18,2) NOT NULL DEFAULT 0,
              sell_order_id character varying(36) NULL,
              sell_order_code character varying(32) NULL,
              sell_order_item_id character varying(36) NULL,
              sell_order_item_code character varying(64) NULL,
              purchase_order_id character varying(36) NULL,
              purchase_order_code character varying(32) NULL,
              stock_out_id character varying(36) NOT NULL,
              stock_out_code character varying(32) NOT NULL,
              stock_out_date date NULL,
              receipt_date date NOT NULL,
              pool_date date NOT NULL,
              calc_date date NOT NULL,
              calc_at timestamp with time zone NOT NULL,
              created_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_at timestamp with time zone NULL,
              CONSTRAINT "PK_commission_dynamic" PRIMARY KEY (id),
              CONSTRAINT "CK_commission_dynamic_role_type" CHECK (role_type IN (1, 2)),
              CONSTRAINT "UQ_commission_dynamic_role_item" UNIQUE (role_type, stock_out_item_id)
            );

            CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_user_pool"
              ON public.commission_dynamic (role_type, user_id, pool_date);

            CREATE INDEX IF NOT EXISTS "IX_commission_dynamic_role_calc"
              ON public.commission_dynamic (role_type, calc_date);

            CREATE TABLE IF NOT EXISTS public.commission_job_watermark (
              id character varying(36) NOT NULL,
              dynamic_job_date date NULL,
              lock_term character(6) NULL,
              created_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_at timestamp with time zone NULL,
              CONSTRAINT "PK_commission_job_watermark" PRIMARY KEY (id)
            );

            INSERT INTO public.commission_job_watermark (id, created_at)
            VALUES ('c2000000-0000-4000-8000-000000000011', NOW())
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.commission_dynamic;
            DROP TABLE IF EXISTS public.commission_job_watermark;
            """);
    }
}

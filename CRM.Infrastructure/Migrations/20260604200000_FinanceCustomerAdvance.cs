using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604200000_FinanceCustomerAdvance")]
    public partial class FinanceCustomerAdvance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.finance_customer_advance (
    ""FinanceCustomerAdvanceId"" character varying(36) NOT NULL,
    customer_id character varying(36) NOT NULL,
    customer_name character varying(200) NULL,
    ""Currency"" smallint NOT NULL DEFAULT 1,
    balance numeric(18,2) NOT NULL DEFAULT 0,
    total_in numeric(18,2) NOT NULL DEFAULT 0,
    total_applied numeric(18,2) NOT NULL DEFAULT 0,
    total_refund numeric(18,2) NOT NULL DEFAULT 0,
    sales_user_id character varying(36) NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    ""ModifyTime"" timestamp with time zone NULL,
    CONSTRAINT ""PK_finance_customer_advance"" PRIMARY KEY (""FinanceCustomerAdvanceId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_finance_customer_advance_customer_currency""
    ON public.finance_customer_advance (customer_id, ""Currency"")
    WHERE is_deleted = false;

COMMENT ON TABLE public.finance_customer_advance IS '客户预收余额（按客户+币别）';

CREATE TABLE IF NOT EXISTS public.finance_customer_advance_ledger (
    ""FinanceCustomerAdvanceLedgerId"" character varying(36) NOT NULL,
    finance_customer_advance_id character varying(36) NOT NULL,
    customer_id character varying(36) NOT NULL,
    ""Currency"" smallint NOT NULL DEFAULT 1,
    ledger_type smallint NOT NULL,
    ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
    finance_receipt_id character varying(36) NULL,
    finance_receipt_item_id character varying(36) NULL,
    finance_receivable_id character varying(36) NULL,
    finance_receivable_write_off_id character varying(36) NULL,
    sell_order_id character varying(36) NULL,
    ""Remark"" character varying(500) NULL,
    operator_user_id character varying(36) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    ""ModifyTime"" timestamp with time zone NULL,
    ""CreateByUserId"" character varying(36) NULL,
    ""ModifyByUserId"" character varying(36) NULL,
    CONSTRAINT ""PK_finance_customer_advance_ledger"" PRIMARY KEY (""FinanceCustomerAdvanceLedgerId"")
);

CREATE INDEX IF NOT EXISTS ""IX_finance_customer_advance_ledger_advance_id""
    ON public.finance_customer_advance_ledger (finance_customer_advance_id);

CREATE INDEX IF NOT EXISTS ""IX_finance_customer_advance_ledger_customer_id""
    ON public.finance_customer_advance_ledger (customer_id);

ALTER TABLE financereceiptitem
  ADD COLUMN IF NOT EXISTS receipt_purpose smallint NOT NULL DEFAULT 10,
  ADD COLUMN IF NOT EXISTS advance_sell_order_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS advance_pool_amount numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN financereceiptitem.receipt_purpose IS '10普通 20预收';
COMMENT ON COLUMN financereceiptitem.advance_pool_amount IS '已转入客户预收池金额';

ALTER TABLE finance_receivable_write_off
  ALTER COLUMN finance_receipt_id DROP NOT NULL,
  ALTER COLUMN finance_receipt_item_id DROP NOT NULL;

ALTER TABLE finance_receivable_write_off
  ADD COLUMN IF NOT EXISTS write_off_source smallint NOT NULL DEFAULT 10,
  ADD COLUMN IF NOT EXISTS finance_customer_advance_ledger_id character varying(36) NULL;

COMMENT ON COLUMN finance_receivable_write_off.write_off_source IS '10收款明细 20预收池';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.finance_customer_advance_ledger;
DROP TABLE IF EXISTS public.finance_customer_advance;
ALTER TABLE financereceiptitem
  DROP COLUMN IF EXISTS receipt_purpose,
  DROP COLUMN IF EXISTS advance_sell_order_id,
  DROP COLUMN IF EXISTS advance_pool_amount;
ALTER TABLE finance_receivable_write_off
  DROP COLUMN IF EXISTS write_off_source,
  DROP COLUMN IF EXISTS finance_customer_advance_ledger_id;
");
        }
    }
}

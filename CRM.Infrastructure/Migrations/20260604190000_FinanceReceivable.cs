using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>应收款主表、核销明细表及历史销售出库回填。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604190000_FinanceReceivable")]
    public partial class FinanceReceivable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.finance_receivable (
    ""FinanceReceivableId"" character varying(36) NOT NULL,
    ""ReceivableCode"" character varying(16) NULL,
    stock_out_id character varying(36) NOT NULL,
    ""StockOutCode"" character varying(32) NOT NULL,
    sell_order_id character varying(36) NOT NULL,
    sell_order_code character varying(32) NULL,
    sell_order_item_id character varying(36) NOT NULL,
    customer_id character varying(36) NOT NULL,
    customer_name character varying(200) NULL,
    sales_user_id character varying(36) NULL,
    ""PN"" character varying(200) NULL,
    ""Brand"" character varying(200) NULL,
    outbound_qty numeric(18,4) NOT NULL DEFAULT 0,
    unit_price numeric(18,6) NOT NULL DEFAULT 0,
    ""Currency"" smallint NOT NULL DEFAULT 1,
    ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
    verified_done numeric(18,2) NOT NULL DEFAULT 0,
    verified_to_be numeric(18,2) NOT NULL DEFAULT 0,
    verification_status smallint NOT NULL DEFAULT 0,
    stock_out_date timestamp with time zone NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    ""ModifyTime"" timestamp with time zone NULL,
    ""CreateByUserId"" character varying(36) NULL,
    ""ModifyByUserId"" character varying(36) NULL,
    CONSTRAINT ""PK_finance_receivable"" PRIMARY KEY (""FinanceReceivableId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_finance_receivable_stock_out_id""
    ON public.finance_receivable (stock_out_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_finance_receivable_customer_id""
    ON public.finance_receivable (customer_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_finance_receivable_sell_order_item_id""
    ON public.finance_receivable (sell_order_item_id)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_finance_receivable_verification_status""
    ON public.finance_receivable (verification_status)
    WHERE is_deleted = false;

COMMENT ON TABLE public.finance_receivable IS '应收款（销售出库批次，一出库单头一行）';

CREATE TABLE IF NOT EXISTS public.finance_receivable_write_off (
    ""FinanceReceivableWriteOffId"" character varying(36) NOT NULL,
    finance_receivable_id character varying(36) NOT NULL,
    finance_receipt_id character varying(36) NOT NULL,
    finance_receipt_item_id character varying(36) NOT NULL,
    ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
    operator_user_id character varying(36) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    ""ModifyTime"" timestamp with time zone NULL,
    ""CreateByUserId"" character varying(36) NULL,
    ""ModifyByUserId"" character varying(36) NULL,
    CONSTRAINT ""PK_finance_receivable_write_off"" PRIMARY KEY (""FinanceReceivableWriteOffId"")
);

CREATE INDEX IF NOT EXISTS ""IX_finance_receivable_write_off_receivable_id""
    ON public.finance_receivable_write_off (finance_receivable_id);

CREATE INDEX IF NOT EXISTS ""IX_finance_receivable_write_off_receipt_item_id""
    ON public.finance_receivable_write_off (finance_receipt_item_id);

INSERT INTO sys_serial_number (""ModuleCode"", ""ModuleName"", ""Prefix"", ""CurrentSequence"", ""IsDeleted"", ""CreateTime"")
SELECT 'FinanceReceivable', '应收款', 'ARV', 0, false, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number WHERE ""ModuleCode"" = 'FinanceReceivable');

INSERT INTO public.finance_receivable (
    ""FinanceReceivableId"", stock_out_id, ""StockOutCode"",
    sell_order_id, sell_order_code, sell_order_item_id,
    customer_id, customer_name, sales_user_id,
    ""PN"", ""Brand"", outbound_qty, unit_price, ""Currency"",
    ""Amount"", verified_done, verified_to_be, verification_status,
    stock_out_date, is_deleted, ""CreateTime""
)
SELECT
    gen_random_uuid()::text,
    so.""StockOutId"",
    so.""StockOutCode"",
    soi.sell_order_id,
    s.sell_order_code,
    so.""SellOrderItemId"",
    COALESCE(NULLIF(TRIM(so.""CustomerId""), ''), s.customer_id),
    s.customer_name,
    s.sales_user_id,
    soi.pn,
    soi.brand,
    so.""TotalQuantity"",
    soi.price,
    s.currency,
    CASE
        WHEN so.""TotalAmount"" > 0 THEN ROUND(so.""TotalAmount"", 2)
        ELSE ROUND(so.""TotalQuantity"" * soi.price, 2)
    END,
    0,
    CASE
        WHEN so.""TotalAmount"" > 0 THEN ROUND(so.""TotalAmount"", 2)
        ELSE ROUND(so.""TotalQuantity"" * soi.price, 2)
    END,
    0,
    so.""StockOutDate"",
    false,
    (now() AT TIME ZONE 'utc')
FROM stock_out so
INNER JOIN sellorderitem soi ON soi.""SellOrderItemId"" = so.""SellOrderItemId""
INNER JOIN sellorder s ON s.""SellOrderId"" = soi.sell_order_id
WHERE so.""StockOutType"" = 10
  AND so.""Status"" IN (2, 4)
  AND COALESCE(so.is_deleted, false) = false
  AND NOT EXISTS (
      SELECT 1 FROM finance_receivable fr
      WHERE fr.stock_out_id = so.""StockOutId"" AND fr.is_deleted = false
  )
  AND (
      CASE
          WHEN so.""TotalAmount"" > 0 THEN ROUND(so.""TotalAmount"", 2)
          ELSE ROUND(so.""TotalQuantity"" * soi.price, 2)
      END
  ) > 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.finance_receivable_write_off;
DROP TABLE IF EXISTS public.finance_receivable;
DELETE FROM sys_serial_number WHERE ""ModuleCode"" = 'FinanceReceivable';
");
        }
    }
}

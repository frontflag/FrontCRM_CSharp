using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 进项发票↔入库明细核销：流水表、发票币别/核销余额、入库明细与头匹配缓存。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260830140000_FinancePurchaseInvoiceWriteOff")]
    public partial class FinancePurchaseInvoiceWriteOff : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepurchaseinvoice
  ADD COLUMN IF NOT EXISTS ""Currency"" smallint NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS ""VerifiedDone"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""VerifiedToBe"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""VerificationStatus"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.financepurchaseinvoice.""Currency"" IS '币别 1人民币 2美元 3欧元';
COMMENT ON COLUMN public.financepurchaseinvoice.""VerifiedDone"" IS '已核销到入库金额';
COMMENT ON COLUMN public.financepurchaseinvoice.""VerifiedToBe"" IS '待核销金额';
COMMENT ON COLUMN public.financepurchaseinvoice.""VerificationStatus"" IS '核销状态 0未核销 1部分 2完成';

UPDATE public.financepurchaseinvoice
SET ""VerifiedToBe"" = GREATEST(0, COALESCE(""InvoiceAmount"", 0) - COALESCE(""VerifiedDone"", 0)),
    ""VerificationStatus"" = CASE
      WHEN COALESCE(""VerifiedDone"", 0) <= 0 THEN 0
      WHEN COALESCE(""VerifiedDone"", 0) + 0.0001 >= COALESCE(""InvoiceAmount"", 0) AND COALESCE(""InvoiceAmount"", 0) > 0 THEN 2
      ELSE 1
    END
WHERE COALESCE(""VerifiedToBe"", 0) = 0 AND COALESCE(""VerifiedDone"", 0) = 0;

ALTER TABLE public.stock_in_item_extend
  ADD COLUMN IF NOT EXISTS invoice_match_done numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_to_be numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_status smallint NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_currency smallint NULL;

ALTER TABLE public.stock_in_extend
  ADD COLUMN IF NOT EXISTS invoice_match_done numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_to_be numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_status smallint NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_currency smallint NULL;

-- 已入库明细：待匹配初值 = 行金额 - 已匹配
UPDATE public.stock_in_item_extend e
SET invoice_match_to_be = GREATEST(0, COALESCE(i.""Amount"", 0) - COALESCE(e.invoice_match_done, 0)),
    invoice_match_currency = COALESCE(e.invoice_match_currency, CASE WHEN i.currency IS NULL THEN NULL ELSE i.currency::smallint END),
    invoice_match_status = CASE
      WHEN COALESCE(e.invoice_match_done, 0) <= 0 THEN 0
      WHEN COALESCE(e.invoice_match_done, 0) + 0.0001 >= COALESCE(i.""Amount"", 0) AND COALESCE(i.""Amount"", 0) > 0 THEN 2
      ELSE 1
    END
FROM public.stock_in_item i
JOIN public.stock_in si ON si.""StockInId"" = i.""StockInId""
WHERE e.""StockInItemId"" = i.""ItemId""
  AND COALESCE(e.is_deleted, false) = false
  AND COALESCE(i.is_deleted, false) = false
  AND COALESCE(si.is_deleted, false) = false
  AND si.""Status"" = 2;

-- 头缓存：按已入库单汇总明细
UPDATE public.stock_in_extend he
SET invoice_match_done = x.done_sum,
    invoice_match_to_be = x.to_be_sum,
    invoice_match_status = CASE
      WHEN x.done_sum <= 0 THEN 0
      WHEN x.to_be_sum <= 0.0001 AND x.done_sum > 0 THEN 2
      ELSE 1
    END,
    invoice_match_currency = x.curr
FROM (
  SELECT
    e.""StockInId"" AS sid,
    COALESCE(SUM(e.invoice_match_done), 0) AS done_sum,
    COALESCE(SUM(e.invoice_match_to_be), 0) AS to_be_sum,
    MIN(e.invoice_match_currency) AS curr
  FROM public.stock_in_item_extend e
  JOIN public.stock_in si ON si.""StockInId"" = e.""StockInId""
  WHERE COALESCE(e.is_deleted, false) = false
    AND si.""Status"" = 2
  GROUP BY e.""StockInId""
) x
WHERE he.""StockInId"" = x.sid;

CREATE TABLE IF NOT EXISTS public.finance_purchase_invoice_write_off (
  ""FinancePurchaseInvoiceWriteOffId"" character varying(36) NOT NULL,
  finance_purchase_invoice_id character varying(36) NOT NULL,
  finance_purchase_invoice_item_id character varying(36) NULL,
  stock_in_item_id character varying(36) NOT NULL,
  stock_in_id character varying(36) NOT NULL,
  purchase_order_item_id character varying(36) NULL,
  ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""Currency"" smallint NOT NULL DEFAULT 1,
  operator_user_id character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT ""PK_finance_purchase_invoice_write_off"" PRIMARY KEY (""FinancePurchaseInvoiceWriteOffId"")
);

CREATE INDEX IF NOT EXISTS ""IX_fpinv_wo_invoice""
  ON public.finance_purchase_invoice_write_off (finance_purchase_invoice_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_fpinv_wo_stock_in_item""
  ON public.finance_purchase_invoice_write_off (stock_in_item_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_fpinv_wo_stock_in""
  ON public.finance_purchase_invoice_write_off (stock_in_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_fpinv_wo_po_item""
  ON public.finance_purchase_invoice_write_off (purchase_order_item_id)
  WHERE is_deleted = false AND purchase_order_item_id IS NOT NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.finance_purchase_invoice_write_off;

ALTER TABLE public.stock_in_extend
  DROP COLUMN IF EXISTS invoice_match_done,
  DROP COLUMN IF EXISTS invoice_match_to_be,
  DROP COLUMN IF EXISTS invoice_match_status,
  DROP COLUMN IF EXISTS invoice_match_currency;

ALTER TABLE public.stock_in_item_extend
  DROP COLUMN IF EXISTS invoice_match_done,
  DROP COLUMN IF EXISTS invoice_match_to_be,
  DROP COLUMN IF EXISTS invoice_match_status,
  DROP COLUMN IF EXISTS invoice_match_currency;

ALTER TABLE public.financepurchaseinvoice
  DROP COLUMN IF EXISTS ""Currency"",
  DROP COLUMN IF EXISTS ""VerifiedDone"",
  DROP COLUMN IF EXISTS ""VerifiedToBe"",
  DROP COLUMN IF EXISTS ""VerificationStatus"";
");
        }
    }
}

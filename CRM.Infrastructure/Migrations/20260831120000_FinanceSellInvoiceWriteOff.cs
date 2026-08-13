using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销项发票↔应收匹配：流水表、发票 Match*、应收 invoice_match_*。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260831120000_FinanceSellInvoiceWriteOff")]
    public partial class FinanceSellInvoiceWriteOff : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financesellinvoice
  ADD COLUMN IF NOT EXISTS ""MatchDone"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""MatchToBe"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""MatchStatus"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.financesellinvoice.""MatchDone"" IS '已匹配开票金额（票↔应收）';
COMMENT ON COLUMN public.financesellinvoice.""MatchToBe"" IS '待匹配开票金额';
COMMENT ON COLUMN public.financesellinvoice.""MatchStatus"" IS '匹配状态 0未匹配 1部分 2完成';

UPDATE public.financesellinvoice
SET ""MatchToBe"" = GREATEST(0, COALESCE(""InvoiceTotal"", 0) - COALESCE(""MatchDone"", 0)),
    ""MatchStatus"" = CASE
      WHEN COALESCE(""MatchDone"", 0) <= 0 THEN 0
      WHEN COALESCE(""MatchDone"", 0) + 0.0001 >= COALESCE(""InvoiceTotal"", 0) AND COALESCE(""InvoiceTotal"", 0) > 0 THEN 2
      ELSE 1
    END
WHERE COALESCE(""MatchToBe"", 0) = 0 AND COALESCE(""MatchDone"", 0) = 0;

ALTER TABLE public.finance_receivable
  ADD COLUMN IF NOT EXISTS invoice_match_done numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_to_be numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_status smallint NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS invoice_match_currency smallint NULL;

UPDATE public.finance_receivable
SET invoice_match_to_be = GREATEST(0, COALESCE(""Amount"", 0) - COALESCE(invoice_match_done, 0)),
    invoice_match_currency = COALESCE(invoice_match_currency, ""Currency""),
    invoice_match_status = CASE
      WHEN COALESCE(invoice_match_done, 0) <= 0 THEN 0
      WHEN COALESCE(invoice_match_done, 0) + 0.0001 >= COALESCE(""Amount"", 0) AND COALESCE(""Amount"", 0) > 0 THEN 2
      ELSE 1
    END
WHERE COALESCE(is_deleted, false) = false
  AND COALESCE(invoice_match_to_be, 0) = 0 AND COALESCE(invoice_match_done, 0) = 0;

CREATE TABLE IF NOT EXISTS public.finance_sell_invoice_write_off (
  ""FinanceSellInvoiceWriteOffId"" character varying(36) NOT NULL,
  finance_sell_invoice_id character varying(36) NOT NULL,
  finance_sell_invoice_item_id character varying(36) NULL,
  finance_receivable_id character varying(36) NOT NULL,
  stock_out_id character varying(36) NULL,
  ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""Currency"" smallint NOT NULL DEFAULT 1,
  operator_user_id character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL DEFAULT timezone('utc', now()),
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT ""PK_finance_sell_invoice_write_off"" PRIMARY KEY (""FinanceSellInvoiceWriteOffId"")
);

CREATE INDEX IF NOT EXISTS ""IX_fsinv_wo_invoice""
  ON public.finance_sell_invoice_write_off (finance_sell_invoice_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_fsinv_wo_receivable""
  ON public.finance_sell_invoice_write_off (finance_receivable_id)
  WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_fsinv_wo_stock_out""
  ON public.finance_sell_invoice_write_off (stock_out_id)
  WHERE is_deleted = false AND stock_out_id IS NOT NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.finance_sell_invoice_write_off;

ALTER TABLE public.finance_receivable
  DROP COLUMN IF EXISTS invoice_match_done,
  DROP COLUMN IF EXISTS invoice_match_to_be,
  DROP COLUMN IF EXISTS invoice_match_status,
  DROP COLUMN IF EXISTS invoice_match_currency;

ALTER TABLE public.financesellinvoice
  DROP COLUMN IF EXISTS ""MatchDone"",
  DROP COLUMN IF EXISTS ""MatchToBe"",
  DROP COLUMN IF EXISTS ""MatchStatus"";
");
        }
    }
}

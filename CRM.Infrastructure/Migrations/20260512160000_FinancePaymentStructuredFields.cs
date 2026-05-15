using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 付款单：请款结构化字段（付款银行 ID、请款备注、费用快照、明细行备注）；Remark 仅作通用/审核备注。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512160000_FinancePaymentStructuredFields")]
    public partial class FinancePaymentStructuredFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.financepayment
  ADD COLUMN IF NOT EXISTS ""FinancePaymentBankId"" character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS ""RequestRemark"" character varying(500) NULL,
  ADD COLUMN IF NOT EXISTS ""FeeIntermediateBank"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""FeeBankCharge"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""FeeFreight"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""FeeMisc"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""FeeRounding"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""FeeIntermediateBankPayer"" character varying(20) NULL;

CREATE INDEX IF NOT EXISTS ""IX_financepayment_FinancePaymentBankId""
  ON public.financepayment (""FinancePaymentBankId"");

COMMENT ON COLUMN public.financepayment.""FinancePaymentBankId"" IS '财务参数-付款银行主键';
COMMENT ON COLUMN public.financepayment.""RequestRemark"" IS '请款人填写的申请备注';
COMMENT ON COLUMN public.financepayment.""FeeIntermediateBank"" IS '中转行费用';
COMMENT ON COLUMN public.financepayment.""FeeBankCharge"" IS '银行手续费';
COMMENT ON COLUMN public.financepayment.""FeeFreight"" IS '运费';
COMMENT ON COLUMN public.financepayment.""FeeMisc"" IS '杂费';
COMMENT ON COLUMN public.financepayment.""FeeRounding"" IS '尾差';
COMMENT ON COLUMN public.financepayment.""FeeIntermediateBankPayer"" IS '中转行费用承担方：我方/供应商';

ALTER TABLE IF EXISTS public.financepaymentitem
  ADD COLUMN IF NOT EXISTS ""LineRemark"" character varying(500) NULL;

COMMENT ON COLUMN public.financepaymentitem.""LineRemark"" IS '请款明细行备注';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.financepaymentitem DROP COLUMN IF EXISTS ""LineRemark"";

DROP INDEX IF EXISTS ""IX_financepayment_FinancePaymentBankId"";

ALTER TABLE IF EXISTS public.financepayment
  DROP COLUMN IF EXISTS ""FeeIntermediateBankPayer"",
  DROP COLUMN IF EXISTS ""FeeRounding"",
  DROP COLUMN IF EXISTS ""FeeMisc"",
  DROP COLUMN IF EXISTS ""FeeFreight"",
  DROP COLUMN IF EXISTS ""FeeBankCharge"",
  DROP COLUMN IF EXISTS ""FeeIntermediateBank"",
  DROP COLUMN IF EXISTS ""RequestRemark"",
  DROP COLUMN IF EXISTS ""FinancePaymentBankId"";
");
        }
    }
}

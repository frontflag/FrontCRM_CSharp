using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 货代公司主数据、货代收款账户、货代付款明细；扩展收款单货代打标字段。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260808120000_FreightForwarderPaymentSchema")]
    public partial class FreightForwarderPaymentSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.freight_forwarder_company (
  ""Id"" character varying(36) NOT NULL,
  ""CompanyCode"" character varying(32) NOT NULL,
  ""cname"" character varying(200) NOT NULL,
  ""ename"" character varying(200) NULL,
  ""Status"" smallint NOT NULL DEFAULT 1,
  ""Remark"" character varying(500) NULL,
  ""is_deleted"" boolean NOT NULL DEFAULT false,
  ""deleted_at"" timestamp with time zone NULL,
  ""deleted_by_user_id"" character varying(36) NULL,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_freight_forwarder_company"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_freight_forwarder_company_CompanyCode""
  ON public.freight_forwarder_company (""CompanyCode"")
  WHERE ""is_deleted"" = false;

CREATE TABLE IF NOT EXISTS public.freight_forwarder_company_bank (
  ""Id"" character varying(36) NOT NULL,
  ""FreightForwarderCompanyId"" character varying(36) NOT NULL,
  ""BankName"" character varying(200) NOT NULL,
  ""AccountName"" character varying(200) NULL,
  ""AccountNo"" character varying(64) NULL,
  ""Currency"" smallint NOT NULL DEFAULT 1,
  ""IsDefault"" boolean NOT NULL DEFAULT false,
  ""IsDisabled"" boolean NOT NULL DEFAULT false,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_freight_forwarder_company_bank"" PRIMARY KEY (""Id"")
);
CREATE INDEX IF NOT EXISTS ""IX_freight_forwarder_company_bank_company""
  ON public.freight_forwarder_company_bank (""FreightForwarderCompanyId"");

CREATE TABLE IF NOT EXISTS public.finance_freight_forwarder_payment (
  ""FinanceFfPaymentId"" character varying(36) NOT NULL,
  ""FinanceReceiptId"" character varying(36) NOT NULL,
  ""FreightForwarderCompanyId"" character varying(36) NOT NULL,
  ""PaymentAmount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""PaymentCurrency"" smallint NOT NULL DEFAULT 1,
  ""PaymentMode"" smallint NOT NULL DEFAULT 1,
  ""CompanyBankId"" character varying(36) NULL,
  ""FfCompanyBankId"" character varying(36) NULL,
  ""BankSlipNo"" character varying(100) NULL,
  ""PaymentDate"" timestamp with time zone NULL,
  ""PaymentUserId"" character varying(36) NULL,
  ""Remark"" character varying(500) NULL,
  ""is_deleted"" boolean NOT NULL DEFAULT false,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_finance_freight_forwarder_payment"" PRIMARY KEY (""FinanceFfPaymentId"")
);
CREATE INDEX IF NOT EXISTS ""IX_finance_ff_payment_receipt""
  ON public.finance_freight_forwarder_payment (""FinanceReceiptId"")
  WHERE ""is_deleted"" = false;

ALTER TABLE public.financereceipt
  ADD COLUMN IF NOT EXISTS is_freight_forwarder_payment boolean NOT NULL DEFAULT false;
ALTER TABLE public.financereceipt
  ADD COLUMN IF NOT EXISTS freight_forwarder_company_id character varying(36) NULL;

COMMENT ON TABLE public.freight_forwarder_company IS '货代公司主数据（与客户/供应商无关）';
COMMENT ON TABLE public.freight_forwarder_company_bank IS '货代公司收款银行账户';
COMMENT ON TABLE public.finance_freight_forwarder_payment IS '货代付款明细（收款后转付货代，无审核）';
COMMENT ON COLUMN public.financereceipt.is_freight_forwarder_payment IS '是否货代付款收款';
COMMENT ON COLUMN public.financereceipt.freight_forwarder_company_id IS '货代公司主键（可选，首次付款前必填）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financereceipt DROP COLUMN IF EXISTS freight_forwarder_company_id;
ALTER TABLE public.financereceipt DROP COLUMN IF EXISTS is_freight_forwarder_payment;
DROP TABLE IF EXISTS public.finance_freight_forwarder_payment;
DROP TABLE IF EXISTS public.freight_forwarder_company_bank;
DROP TABLE IF EXISTS public.freight_forwarder_company;
");
        }
    }
}

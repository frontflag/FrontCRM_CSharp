using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260605120000_FinancePaymentCompanyBankId")]
    public partial class FinancePaymentCompanyBankId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepayment
  ADD COLUMN IF NOT EXISTS ""CompanyBankId"" character varying(36) NULL;

CREATE INDEX IF NOT EXISTS ""IX_financepayment_CompanyBankId""
  ON public.financepayment (""CompanyBankId"");

COMMENT ON COLUMN public.financepayment.""CompanyBankId"" IS '公司银行账户主键（company_bankinfo.Id）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_financepayment_CompanyBankId"";
ALTER TABLE public.financepayment DROP COLUMN IF EXISTS ""CompanyBankId"";
");
        }
    }
}

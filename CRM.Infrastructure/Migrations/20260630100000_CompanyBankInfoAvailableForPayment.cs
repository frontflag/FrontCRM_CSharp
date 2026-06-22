using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260630100000_CompanyBankInfoAvailableForPayment")]
    public partial class CompanyBankInfoAvailableForPayment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.company_bankinfo
  ADD COLUMN IF NOT EXISTS available_for_payment boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.company_bankinfo.available_for_payment IS '可用付款：勾选后出现在付款单付款银行下拉';

UPDATE public.company_bankinfo
SET available_for_payment = true
WHERE enabled = true
  AND lower(trim(purpose_type)) = 'payment';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.company_bankinfo DROP COLUMN IF EXISTS available_for_payment;");
        }
    }
}

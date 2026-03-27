using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327173000_AddFinancePaymentBankSlipNo")]
    public partial class AddFinancePaymentBankSlipNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepayment ADD COLUMN IF NOT EXISTS ""BankSlipNo"" character varying(100) NULL;
COMMENT ON COLUMN public.financepayment.""BankSlipNo"" IS '银行水单号码';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.financepayment DROP COLUMN IF EXISTS ""BankSlipNo"";");
        }
    }
}

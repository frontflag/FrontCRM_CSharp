using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260418120000_AddFinanceReceiptBankSlipNo")]
    public partial class AddFinanceReceiptBankSlipNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financereceipt ADD COLUMN IF NOT EXISTS ""BankSlipNo"" character varying(100) NULL;
COMMENT ON COLUMN public.financereceipt.""BankSlipNo"" IS '银行水单号码';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.financereceipt DROP COLUMN IF EXISTS ""BankSlipNo"";");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// financepayment：请款关联供应商银行账户 VendorBankId。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260629150000_FinancePaymentVendorBankId")]
public partial class FinancePaymentVendorBankId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.financepayment
              ADD COLUMN IF NOT EXISTS "VendorBankId" character varying(36) NULL;

            COMMENT ON COLUMN public.financepayment."VendorBankId"
              IS '供应商银行账户 ID（vendorbankinfo.BankId）';

            CREATE INDEX IF NOT EXISTS "IX_financepayment_VendorBankId"
              ON public.financepayment ("VendorBankId")
              WHERE "VendorBankId" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public."IX_financepayment_VendorBankId";
            ALTER TABLE IF EXISTS public.financepayment DROP COLUMN IF EXISTS "VendorBankId";
            """);
    }
}

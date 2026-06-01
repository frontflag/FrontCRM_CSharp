using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// vendorbankinfo：供应商开户银行关联财务参数付款银行（FinancePaymentBankId）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260723120000_AddVendorBankFinancePaymentBankId")]
public partial class AddVendorBankFinancePaymentBankId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.vendorbankinfo
              ADD COLUMN IF NOT EXISTS "FinancePaymentBankId" character varying(36) NULL;

            COMMENT ON COLUMN public.vendorbankinfo."FinancePaymentBankId"
              IS '财务参数-付款银行主键（financepaymentbank.FinancePaymentBankId）；与 BankName 冗余展示，请款时默认供应商银行';

            CREATE INDEX IF NOT EXISTS "IX_vendorbankinfo_FinancePaymentBankId"
              ON public.vendorbankinfo ("FinancePaymentBankId")
              WHERE "FinancePaymentBankId" IS NOT NULL;

            UPDATE public.vendorbankinfo vb
            SET "FinancePaymentBankId" = fp."FinancePaymentBankId"
            FROM public.financepaymentbank fp
            WHERE vb."FinancePaymentBankId" IS NULL
              AND vb."BankName" IS NOT NULL
              AND TRIM(vb."BankName") <> ''
              AND LOWER(TRIM(vb."BankName")) = LOWER(TRIM(fp."BankName"))
              AND COALESCE(fp."IsDisabled", false) = false;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public."IX_vendorbankinfo_FinancePaymentBankId";
            ALTER TABLE IF EXISTS public.vendorbankinfo DROP COLUMN IF EXISTS "FinancePaymentBankId";
            """);
    }
}

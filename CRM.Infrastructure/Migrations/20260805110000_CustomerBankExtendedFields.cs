using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// customerbankinfo：银行地址、联行号/银行代码、SWIFT（与客户银行编辑表单对齐）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805110000_CustomerBankExtendedFields")]
public partial class CustomerBankExtendedFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customerbankinfo
              ADD COLUMN IF NOT EXISTS "BankAddress" character varying(500) NULL,
              ADD COLUMN IF NOT EXISTS "BankCode" character varying(32) NULL,
              ADD COLUMN IF NOT EXISTS "Swift" character varying(64) NULL;

            COMMENT ON COLUMN public.customerbankinfo."BankAddress" IS '银行地址';
            COMMENT ON COLUMN public.customerbankinfo."BankCode" IS '联行号/银行代码';
            COMMENT ON COLUMN public.customerbankinfo."Swift" IS 'SWIFT 国际银行代码';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customerbankinfo
              DROP COLUMN IF EXISTS "BankAddress",
              DROP COLUMN IF EXISTS "BankCode",
              DROP COLUMN IF EXISTS "Swift";
            """);
    }
}

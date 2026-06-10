using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// vendorbankinfo：对齐公司银行字段（地址、SWIFT、IBAN、联行号、国家、账户类型、用途、启用）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260629140000_VendorBankExtendedFields")]
public partial class VendorBankExtendedFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.vendorbankinfo
              ADD COLUMN IF NOT EXISTS "BankAddress" character varying(500) NULL,
              ADD COLUMN IF NOT EXISTS "Swift" character varying(64) NULL,
              ADD COLUMN IF NOT EXISTS "Iban" character varying(64) NULL,
              ADD COLUMN IF NOT EXISTS "BankCode" character varying(32) NULL,
              ADD COLUMN IF NOT EXISTS "Country" character varying(100) NULL,
              ADD COLUMN IF NOT EXISTS "AccountType" character varying(32) NOT NULL DEFAULT 'rmb',
              ADD COLUMN IF NOT EXISTS "PurposeType" character varying(32) NOT NULL DEFAULT 'payment',
              ADD COLUMN IF NOT EXISTS "IsEnabled" boolean NOT NULL DEFAULT true;

            COMMENT ON COLUMN public.vendorbankinfo."BankAddress" IS '银行地址';
            COMMENT ON COLUMN public.vendorbankinfo."Swift" IS 'SWIFT 国际银行代码';
            COMMENT ON COLUMN public.vendorbankinfo."Iban" IS 'IBAN 国际银行账号';
            COMMENT ON COLUMN public.vendorbankinfo."BankCode" IS '联行号/银行号';
            COMMENT ON COLUMN public.vendorbankinfo."Country" IS '所在国家';
            COMMENT ON COLUMN public.vendorbankinfo."AccountType" IS '账户类型：rmb / foreign';
            COMMENT ON COLUMN public.vendorbankinfo."PurposeType" IS '用途：payment / receipt';
            COMMENT ON COLUMN public.vendorbankinfo."IsEnabled" IS '是否启用';

            UPDATE public.vendorbankinfo
            SET "AccountType" = CASE WHEN "Currency" = 1 THEN 'rmb' ELSE 'foreign' END
            WHERE "AccountType" IS NULL OR TRIM("AccountType") = '';

            UPDATE public.vendorbankinfo
            SET "PurposeType" = 'payment'
            WHERE "PurposeType" IS NULL OR TRIM("PurposeType") = '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.vendorbankinfo
              DROP COLUMN IF EXISTS "BankAddress",
              DROP COLUMN IF EXISTS "Swift",
              DROP COLUMN IF EXISTS "Iban",
              DROP COLUMN IF EXISTS "BankCode",
              DROP COLUMN IF EXISTS "Country",
              DROP COLUMN IF EXISTS "AccountType",
              DROP COLUMN IF EXISTS "PurposeType",
              DROP COLUMN IF EXISTS "IsEnabled";
            """);
    }
}

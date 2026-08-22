using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CRM.Infrastructure.Data;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 客户/供应商主表增加企业邮箱后缀（只存 @xxx.com；各自未删除记录内唯一）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260912120000_PartyCompanyEmailSuffix")]
public partial class PartyCompanyEmailSuffix : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.customerinfo
              ADD COLUMN IF NOT EXISTS "CompanyEmailSuffix" character varying(128);

            COMMENT ON COLUMN public.customerinfo."CompanyEmailSuffix" IS '企业邮箱后缀，如 @xxx.com';

            CREATE UNIQUE INDEX IF NOT EXISTS ux_customerinfo_company_email_suffix
              ON public.customerinfo ("CompanyEmailSuffix")
              WHERE "IsDeleted" = false
                AND "CompanyEmailSuffix" IS NOT NULL
                AND btrim("CompanyEmailSuffix") <> '';

            ALTER TABLE public.vendorinfo
              ADD COLUMN IF NOT EXISTS "CompanyEmailSuffix" character varying(128);

            COMMENT ON COLUMN public.vendorinfo."CompanyEmailSuffix" IS '企业邮箱后缀，如 @xxx.com';

            CREATE UNIQUE INDEX IF NOT EXISTS ux_vendorinfo_company_email_suffix
              ON public.vendorinfo ("CompanyEmailSuffix")
              WHERE "IsDeleted" = false
                AND "CompanyEmailSuffix" IS NOT NULL
                AND btrim("CompanyEmailSuffix") <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public.ux_customerinfo_company_email_suffix;
            ALTER TABLE public.customerinfo DROP COLUMN IF EXISTS "CompanyEmailSuffix";
            DROP INDEX IF EXISTS public.ux_vendorinfo_company_email_suffix;
            ALTER TABLE public.vendorinfo DROP COLUMN IF EXISTS "CompanyEmailSuffix";
            """);
    }
}

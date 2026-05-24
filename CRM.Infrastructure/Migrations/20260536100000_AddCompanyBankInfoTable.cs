using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>公司银行信息独立表，并从 sysparam JSON 迁移历史数据。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260536100000_AddCompanyBankInfoTable")]
    public partial class AddCompanyBankInfoTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.company_bankinfo (
    ""Id"" character varying(36) NOT NULL,
    bank_name character varying(200) NOT NULL DEFAULT '',
    bank_address character varying(500) NOT NULL DEFAULT '',
    swift character varying(64) NOT NULL DEFAULT '',
    bank_code character varying(32) NOT NULL DEFAULT '',
    account_type character varying(32) NOT NULL DEFAULT 'rmb',
    remark character varying(500) NOT NULL DEFAULT '',
    account_name character varying(200) NOT NULL DEFAULT '',
    account_number character varying(64) NOT NULL DEFAULT '',
    currency character varying(16) NOT NULL DEFAULT 'RMB',
    country character varying(100) NOT NULL DEFAULT '',
    iban character varying(64) NOT NULL DEFAULT '',
    purpose_type character varying(32) NOT NULL DEFAULT 'payment',
    is_default boolean NOT NULL DEFAULT false,
    enabled boolean NOT NULL DEFAULT true,
    sort_order integer NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_company_bankinfo"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_company_bankinfo_sort_order"" ON public.company_bankinfo (sort_order);
CREATE INDEX IF NOT EXISTS ""IX_company_bankinfo_is_default"" ON public.company_bankinfo (is_default) WHERE is_default = true;

INSERT INTO public.company_bankinfo (
    ""Id"", bank_name, bank_address, swift, bank_code, account_type, remark,
    account_name, account_number, currency, country, iban, purpose_type,
    is_default, enabled, sort_order, ""CreateTime"", ""ModifyTime""
)
SELECT
    COALESCE(NULLIF(trim(elem->>'id'), ''), gen_random_uuid()::text),
    COALESCE(elem->>'bankName', ''),
    COALESCE(elem->>'bankAddress', ''),
    COALESCE(elem->>'swift', ''),
    COALESCE(elem->>'bankCode', ''),
    COALESCE(NULLIF(trim(elem->>'bankType'), ''), 'rmb'),
    COALESCE(elem->>'remark', ''),
    COALESCE(elem->>'accountName', ''),
    COALESCE(NULLIF(trim(elem->>'accountNumber'), ''), COALESCE(elem->>'iban', '')),
    COALESCE(NULLIF(trim(elem->>'currency'), ''), 'RMB'),
    COALESCE(elem->>'country', ''),
    COALESCE(elem->>'iban', ''),
    COALESCE(NULLIF(trim(elem->>'purposeType'), ''), 'payment'),
    COALESCE((elem->>'isDefault')::boolean, false),
    COALESCE((elem->>'enabled')::boolean, true),
    (ord - 1)::integer,
    (NOW() AT TIME ZONE 'UTC'),
    (NOW() AT TIME ZONE 'UTC')
FROM public.sysparam s,
LATERAL jsonb_array_elements(
    CASE
        WHEN s.""ValueJson"" IS NULL OR trim(s.""ValueJson"") = '' OR trim(s.""ValueJson"") = '[]' THEN '[]'::jsonb
        ELSE s.""ValueJson""::jsonb
    END
) WITH ORDINALITY AS t(elem, ord)
WHERE s.""ParamCode"" = 'Company.Profile.BankInfos'
  AND NOT EXISTS (SELECT 1 FROM public.company_bankinfo LIMIT 1);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.company_bankinfo;");
        }
    }
}

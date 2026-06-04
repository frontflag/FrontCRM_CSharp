using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// financepaymentbank：银行英文名称、币别类型（10=人民币银行，20=外币银行）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604150000_FinancePaymentBankEBankNameCurrencyType")]
    public partial class FinancePaymentBankEBankNameCurrencyType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE financepaymentbank
  ADD COLUMN IF NOT EXISTS ""EBankName"" character varying(200) NULL,
  ADD COLUMN IF NOT EXISTS ""CurrencyType"" integer NOT NULL DEFAULT 10;

COMMENT ON COLUMN financepaymentbank.""EBankName"" IS '银行英文名称';
COMMENT ON COLUMN financepaymentbank.""CurrencyType"" IS '币别类型：10=人民币银行，20=外币银行';

UPDATE financepaymentbank SET ""CurrencyType"" = 10 WHERE ""CurrencyType"" IS NULL OR ""CurrencyType"" NOT IN (10, 20);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE financepaymentbank
  DROP COLUMN IF EXISTS ""EBankName"",
  DROP COLUMN IF EXISTS ""CurrencyType"";
");
        }
    }
}

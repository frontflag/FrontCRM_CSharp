using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// financepaymentbank：银行简称 ShortName。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604160000_FinancePaymentBankShortName")]
    public partial class FinancePaymentBankShortName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE financepaymentbank
  ADD COLUMN IF NOT EXISTS ""ShortName"" character varying(100) NULL;

COMMENT ON COLUMN financepaymentbank.""ShortName"" IS '银行简称';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE financepaymentbank DROP COLUMN IF EXISTS ""ShortName"";");
        }
    }
}

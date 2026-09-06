using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 客户报价单流水号从 100 起号：CurrentSequence=99，下一次 GenerateNext 为 100。
    /// 已发出的 CQ00000 / CQ00001 不改写。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260906194500_CustomerQuoteSerialStart100")]
    public partial class CustomerQuoteSerialStart100 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.sys_serial_number
SET ""CurrentSequence"" = GREATEST(""CurrentSequence"", 99),
    ""UpdateTime"" = timezone('utc', now())
WHERE ""ModuleCode"" = 'CustomerQuote';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.sys_serial_number
SET ""CurrentSequence"" = -1,
    ""UpdateTime"" = timezone('utc', now())
WHERE ""ModuleCode"" = 'CustomerQuote'
  AND ""CurrentSequence"" = 99;
");
        }
    }
}

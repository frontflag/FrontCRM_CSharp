using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 客户统一社会信用代码列由 varchar(18) 扩至 varchar(50)，避免带连字符等格式保存时报错。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260429100000_CustomerCreditCodeMaxLength50")]
    public partial class CustomerCreditCodeMaxLength50 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customerinfo
  ALTER COLUMN ""CreditCode"" TYPE character varying(50);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customerinfo
  ALTER COLUMN ""CreditCode"" TYPE character varying(18);
");
        }
    }
}

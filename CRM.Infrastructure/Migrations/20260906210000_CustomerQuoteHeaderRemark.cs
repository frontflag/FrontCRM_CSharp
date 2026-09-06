using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>客户报价单主表增加抬头备注。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260906210000_CustomerQuoteHeaderRemark")]
    public partial class CustomerQuoteHeaderRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.customer_quote
    ADD COLUMN IF NOT EXISTS remark character varying(2000);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.customer_quote
    DROP COLUMN IF EXISTS remark;
");
        }
    }
}

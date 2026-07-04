using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704120000_FinanceReceiptItemRemark")]
    public partial class FinanceReceiptItemRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financereceiptitem
  ADD COLUMN IF NOT EXISTS remark character varying(500) NULL;

COMMENT ON COLUMN public.financereceiptitem.remark IS '备注';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.financereceiptitem DROP COLUMN IF EXISTS remark;");
        }
    }
}

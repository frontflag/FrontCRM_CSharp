using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>收款单取消审核：历史待审核(1)→新建(0)，已审核(2)→确认(3)。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908120000_FinanceReceiptStatusNoAudit")]
public partial class FinanceReceiptStatusNoAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.financereceipt SET "Status" = 0 WHERE "Status" = 1;
            UPDATE public.financereceipt SET "Status" = 3 WHERE "Status" = 2;

            COMMENT ON COLUMN public.financereceipt."Status" IS '收款状态：0新建 3确认 4取消（历史 1 视为新建，2 视为确认）';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            COMMENT ON COLUMN public.financereceipt."Status" IS '收款状态：0草稿 1待审核 2已审核 3已收款 4已取消';
            """);
    }
}

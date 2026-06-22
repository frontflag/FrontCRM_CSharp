using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 更新 RFQ 轮询分配说明：按明细（RFQItem）轮询。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803170000_RfqRoundRobinPerItemDescription")]
public partial class RfqRoundRobinPerItemDescription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.sysparam
            SET "Description" = '每条 RFQItem 从报价员池连续取 N 人（1 或 2），按明细轮询。'
            WHERE "ParamCode" = 'System.RFQ.RoundRobinAssigneeCount';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.sysparam
            SET "Description" = '每条 RFQ 从报价员池连续取 N 人（1 或 2）。'
            WHERE "ParamCode" = 'System.RFQ.RoundRobinAssigneeCount';
            """);
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CRM.Infrastructure.Data;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 删除供应商等级历史字典项（5～13）。已执行过 20260909 仅停用、未删除的库补跑本条。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260910120000_VendorLevelDeleteLegacyDict")]
public partial class VendorLevelDeleteLegacyDict : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM public.sys_dict_item
              WHERE "Category" = 'VendorLevel' AND "ItemCode" NOT IN ('1','2','3','4');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

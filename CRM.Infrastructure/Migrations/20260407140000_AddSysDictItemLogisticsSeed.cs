using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>物流「来货方式」「快递方式」字典（sys_dict_item）</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260407140000_AddSysDictItemLogisticsSeed")]
    public partial class AddSysDictItemLogisticsSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var row in SysDictLogisticsSeedRows.All)
            {
                var cat = SqlEsc(row.Category);
                var code = SqlEsc(row.ItemCode);
                var zh = SqlEsc(row.NameZh);
                var en = SqlEsc(row.NameEn);
                migrationBuilder.Sql($@"
INSERT INTO public.sys_dict_item (""Id"",""Category"",""ItemCode"",""NameZh"",""NameEn"",""SortOrder"",""IsActive"",""CreateTime"")
SELECT gen_random_uuid()::text, '{cat}', '{code}', '{zh}', '{en}', {row.SortOrder}, true, NOW() AT TIME ZONE 'utc'
WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d.""Category"" = '{cat}' AND d.""ItemCode"" = '{code}');");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM public.sys_dict_item WHERE ""Category"" IN ('LogisticsArrivalMethod','LogisticsExpressMethod');");
        }

        private static string SqlEsc(string s) => (s ?? string.Empty).Replace("'", "''");
    }
}

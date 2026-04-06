using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 客户类型/等级/行业/税率/发票类型 字典种子（表 sys_dict_item 已由供应商迁移创建）
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404130000_AddSysDictItemCustomerSeed")]
    public partial class AddSysDictItemCustomerSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var row in SysDictCustomerSeedRows.All)
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
            migrationBuilder.Sql(@"
DELETE FROM public.sys_dict_item WHERE ""Category"" IN (
  'CustomerType','CustomerLevel','CustomerIndustry','CustomerTaxRate','CustomerInvoiceType'
);");
        }

        private static string SqlEsc(string s) => (s ?? string.Empty).Replace("'", "''");
    }
}

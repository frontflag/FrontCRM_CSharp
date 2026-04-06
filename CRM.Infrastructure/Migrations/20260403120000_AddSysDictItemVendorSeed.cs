using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 数据字典表 sys_dict_item + 供应商行业/等级/身份/付款方式种子
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260403120000_AddSysDictItemVendorSeed")]
    public partial class AddSysDictItemVendorSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sys_dict_item (
    ""Id"" character varying(36) NOT NULL,
    ""Category"" character varying(64) NOT NULL,
    ""ItemCode"" character varying(64) NOT NULL,
    ""NameZh"" character varying(200) NOT NULL,
    ""NameEn"" character varying(200) NULL,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""IsActive"" boolean NOT NULL DEFAULT true,
    ""CreateTime"" timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    CONSTRAINT ""PK_sys_dict_item"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_sys_dict_item_Category_ItemCode"" ON public.sys_dict_item (""Category"", ""ItemCode"");
");

            foreach (var row in SysDictVendorSeedRows.All)
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
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.sys_dict_item;");
        }

        private static string SqlEsc(string s) => (s ?? string.Empty).Replace("'", "''");
    }
}

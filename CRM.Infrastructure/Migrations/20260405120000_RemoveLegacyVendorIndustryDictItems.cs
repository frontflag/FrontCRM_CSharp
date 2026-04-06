using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 移除供应商行业字典中的历史编码（与当前 VENDOR_INDUSTRY_VALUES 对齐）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260405120000_RemoveLegacyVendorIndustryDictItems")]
    public partial class RemoveLegacyVendorIndustryDictItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM public.sys_dict_item
WHERE ""Category"" = 'VendorIndustry'
  AND ""ItemCode"" IN (
    'Electronics','Machinery','Chemical','Textile','Food',
    'Construction','Trading','Technology','Healthcare','Other'
);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不回插历史项；若需恢复请重新执行含历史种子的迁移或手工插入
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>为常用品牌初始化搜索短码别名（可在品牌管理中继续维护）。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260803120000_BizBrandSearchShortCodeAliases")]
    public partial class BizBrandSearchShortCodeAliases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 仅在别名为空时写入默认短码，避免覆盖已有配置
UPDATE public.biz_brand SET alias = 'TI'
WHERE is_deleted = false
  AND standard_brand ILIKE 'TI/德州仪器%'
  AND (alias IS NULL OR TRIM(alias) = '');

UPDATE public.biz_brand SET alias = 'ON'
WHERE is_deleted = false
  AND standard_brand ILIKE 'ONSEMI/安森美%'
  AND (alias IS NULL OR TRIM(alias) = '');

UPDATE public.biz_brand SET alias = 'ST'
WHERE is_deleted = false
  AND standard_brand ILIKE 'ST/意法%'
  AND (alias IS NULL OR TRIM(alias) = '');

UPDATE public.biz_brand SET alias = 'WD'
WHERE is_deleted = false
  AND standard_brand ILIKE 'WD/西数数据%'
  AND (alias IS NULL OR TRIM(alias) = '');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 数据种子不回滚，避免误删用户已维护的别名
        }
    }
}

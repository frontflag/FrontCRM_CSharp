using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CRM.Infrastructure.Data;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 供应商等级改为 S/A/B/C（1/2/3/4）；存量全部落 C；字典同步。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260909120000_VendorLevelSABC")]
public partial class VendorLevelSABC : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.vendorinfo SET "Level" = 4;

            COMMENT ON COLUMN public.vendorinfo."Level" IS '供应商等级 VendorLevelCode：1=S 2=A 3=B 4=C，默认 4=C';

            UPDATE public.sys_dict_item
              SET "NameZh" = 'S', "NameEn" = 'S', "SortOrder" = 1, "IsActive" = true
              WHERE "Category" = 'VendorLevel' AND "ItemCode" = '1';
            UPDATE public.sys_dict_item
              SET "NameZh" = 'A', "NameEn" = 'A', "SortOrder" = 2, "IsActive" = true
              WHERE "Category" = 'VendorLevel' AND "ItemCode" = '2';
            UPDATE public.sys_dict_item
              SET "NameZh" = 'B', "NameEn" = 'B', "SortOrder" = 3, "IsActive" = true
              WHERE "Category" = 'VendorLevel' AND "ItemCode" = '3';
            UPDATE public.sys_dict_item
              SET "NameZh" = 'C', "NameEn" = 'C', "SortOrder" = 4, "IsActive" = true
              WHERE "Category" = 'VendorLevel' AND "ItemCode" = '4';

            INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
            SELECT gen_random_uuid()::text, 'VendorLevel', '1', 'S', 'S', 1, true, NOW() AT TIME ZONE 'utc'
            WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '1');
            INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
            SELECT gen_random_uuid()::text, 'VendorLevel', '2', 'A', 'A', 2, true, NOW() AT TIME ZONE 'utc'
            WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '2');
            INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
            SELECT gen_random_uuid()::text, 'VendorLevel', '3', 'B', 'B', 3, true, NOW() AT TIME ZONE 'utc'
            WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '3');
            INSERT INTO public.sys_dict_item ("Id","Category","ItemCode","NameZh","NameEn","SortOrder","IsActive","CreateTime")
            SELECT gen_random_uuid()::text, 'VendorLevel', '4', 'C', 'C', 4, true, NOW() AT TIME ZONE 'utc'
            WHERE NOT EXISTS (SELECT 1 FROM public.sys_dict_item d WHERE d."Category" = 'VendorLevel' AND d."ItemCode" = '4');

            DELETE FROM public.sys_dict_item
              WHERE "Category" = 'VendorLevel' AND "ItemCode" NOT IN ('1','2','3','4');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            COMMENT ON COLUMN public.vendorinfo."Level" IS '供应商等级（VendorLevelCode 数据字典编码）';

            UPDATE public.sys_dict_item SET "NameZh" = '1-', "NameEn" = '1-', "SortOrder" = 1 WHERE "Category" = 'VendorLevel' AND "ItemCode" = '1';
            UPDATE public.sys_dict_item SET "NameZh" = '1', "NameEn" = '1', "SortOrder" = 2 WHERE "Category" = 'VendorLevel' AND "ItemCode" = '2';
            UPDATE public.sys_dict_item SET "NameZh" = '1+', "NameEn" = '1+', "SortOrder" = 3 WHERE "Category" = 'VendorLevel' AND "ItemCode" = '3';
            UPDATE public.sys_dict_item SET "NameZh" = '2-', "NameEn" = '2-', "SortOrder" = 4 WHERE "Category" = 'VendorLevel' AND "ItemCode" = '4';
            """);
    }
}

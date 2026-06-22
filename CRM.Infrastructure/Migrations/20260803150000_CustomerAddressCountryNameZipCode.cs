using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// customeraddress：国家/地区名称、邮政编码，支持海外地址录入（方案 A）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803150000_CustomerAddressCountryNameZipCode")]
public partial class CustomerAddressCountryNameZipCode : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customeraddress
              ADD COLUMN IF NOT EXISTS "CountryName" character varying(100) NULL,
              ADD COLUMN IF NOT EXISTS "ZipCode" character varying(20) NULL;

            COMMENT ON COLUMN public.customeraddress."CountryName" IS '国家/地区名称（如 中国、United States）';
            COMMENT ON COLUMN public.customeraddress."ZipCode" IS '邮政编码';
            COMMENT ON COLUMN public.customeraddress."Country" IS '国家/地区代码：1=中国（含大陆/港/台），2=海外';

            UPDATE public.customeraddress
            SET "CountryName" = '中国',
                "Country" = 1
            WHERE "CountryName" IS NULL
              AND ("Province" IS NOT NULL OR "City" IS NOT NULL OR "Area" IS NOT NULL);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customeraddress
              DROP COLUMN IF EXISTS "ZipCode",
              DROP COLUMN IF EXISTS "CountryName";
            """);
    }
}

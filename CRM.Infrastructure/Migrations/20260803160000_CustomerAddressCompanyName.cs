using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// customeraddress：地址公司名称（可与客户主档名称不同，如收货方/开票方）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260803160000_CustomerAddressCompanyName")]
public partial class CustomerAddressCompanyName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customeraddress
              ADD COLUMN IF NOT EXISTS "CompanyName" character varying(200) NULL;

            COMMENT ON COLUMN public.customeraddress."CompanyName" IS '地址公司名称';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.customeraddress
              DROP COLUMN IF EXISTS "CompanyName";
            """);
    }
}

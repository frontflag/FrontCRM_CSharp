using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using CRM.Infrastructure.Data;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 供应商主表增加邓白氏编码（与 customerinfo.DUNS 对齐）。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260911120000_VendorDuns")]
public partial class VendorDuns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.vendorinfo
              ADD COLUMN IF NOT EXISTS "DUNS" character varying(20);

            COMMENT ON COLUMN public.vendorinfo."DUNS" IS '邓白氏码（D-U-N-S Number）';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.vendorinfo DROP COLUMN IF EXISTS "DUNS";
            """);
    }
}

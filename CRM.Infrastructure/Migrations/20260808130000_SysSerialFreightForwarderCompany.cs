using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260808130000_SysSerialFreightForwarderCompany")]
    public partial class SysSerialFreightForwarderCompany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'FreightForwarderCompany') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'FreightForwarderCompany', '货代公司', 'FFC', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" = 'FreightForwarderCompany';");
        }
    }
}

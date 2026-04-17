using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// <c>sys_serial_number</c> 增加报关公司模块（前缀 CBR），供创建报关公司时自动生成编号。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260616100000_SysSerialCustomsBroker")]
    public partial class SysSerialCustomsBroker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomsBroker') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'CustomsBroker', '报关公司', 'CBR', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomsBroker';");
        }
    }
}

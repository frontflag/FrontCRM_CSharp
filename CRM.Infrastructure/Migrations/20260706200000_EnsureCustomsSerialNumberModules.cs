using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 补齐报关相关 <c>sys_serial_number</c> 模块行（报关单 CDS、移库单 STF），避免确认报关装箱时生成报关单失败。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260706200000_EnsureCustomsSerialNumberModules")]
    public partial class EnsureCustomsSerialNumberModules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'StockTransfer') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'StockTransfer', '移库单', 'STF', 5, -1, false, false, timezone('utc', now()));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomsDeclaration') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'CustomsDeclaration', '报关单', 'CDS', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" IN ('StockTransfer', 'CustomsDeclaration');");
        }
    }
}

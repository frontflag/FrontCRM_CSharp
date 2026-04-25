using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 手工移库单流水号模块 <c>StockTransferManual</c>（前缀 STM）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424120000_AddStockTransferManualSerialNumber")]
    public partial class AddStockTransferManualSerialNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'StockTransferManual') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'StockTransferManual', '手工移库单', 'STM', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" = 'StockTransferManual';");
        }
    }
}

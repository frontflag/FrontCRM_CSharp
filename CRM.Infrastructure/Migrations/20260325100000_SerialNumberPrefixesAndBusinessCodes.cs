using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 统一业务编号前缀与流水模块；放宽收款单号长度以容纳 前缀+YYMMDD+4位。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325100000_SerialNumberPrefixesAndBusinessCodes")]
    public partial class SerialNumberPrefixesAndBusinessCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS financereceipt
    ALTER COLUMN ""FinanceReceiptCode"" TYPE character varying(32);

UPDATE sys_serial_number SET ""Prefix"" = 'QT' WHERE ""ModuleCode"" = 'Quotation';
UPDATE sys_serial_number SET ""Prefix"" = 'SI' WHERE ""ModuleCode"" = 'StockIn';
UPDATE sys_serial_number SET ""Prefix"" = 'SOUT' WHERE ""ModuleCode"" = 'StockOut';

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PurchaseRequisition', '采购申请', 'PR', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'PurchaseRequisition');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'StockOutRequest', '出库申请', 'SON', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'StockOutRequest');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PickingTask', '拣货任务', 'PK', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'PickingTask');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'ArrivalNotice', '到货通知', 'AN', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'ArrivalNotice');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'QcRecord', '质检', 'QC', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'QcRecord');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'PaymentRequest', '请款', 'PRQ', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'PaymentRequest');

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""LastResetMonth"", ""LastResetYear"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""Remark"", ""ResetByMonth"", ""ResetByYear"", ""SequenceLength"", ""UpdateTime"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number), '2026-01-01 00:00:00+00'::timestamptz, 0, NULL, NULL, 'FinancePayment', '财务付款', 'FPY', NULL, false, false, 4, NULL
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number s WHERE s.""ModuleCode"" = 'FinancePayment');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE sys_serial_number SET ""Prefix"" = 'QUO' WHERE ""ModuleCode"" = 'Quotation';
UPDATE sys_serial_number SET ""Prefix"" = 'STI' WHERE ""ModuleCode"" = 'StockIn';
UPDATE sys_serial_number SET ""Prefix"" = 'STO' WHERE ""ModuleCode"" = 'StockOut';

DELETE FROM sys_serial_number WHERE ""ModuleCode"" IN (
    'PurchaseRequisition','StockOutRequest','PickingTask','ArrivalNotice','QcRecord','PaymentRequest','FinancePayment');

ALTER TABLE IF EXISTS financereceipt
    ALTER COLUMN ""FinanceReceiptCode"" TYPE character varying(10);
");
        }
    }
}

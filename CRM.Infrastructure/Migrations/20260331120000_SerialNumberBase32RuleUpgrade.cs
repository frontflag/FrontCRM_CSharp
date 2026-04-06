using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 升级业务流水号到新规则：3/4位业务标识 + 5位32进制流水号。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260331120000_SerialNumberBase32RuleUpgrade")]
    public partial class SerialNumberBase32RuleUpgrade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 须先 UPDATE 再收缩 Prefix 列，否则现网已有 PAY_DEL/STOR 等 >4 字符时会 22001
UPDATE sys_serial_number SET ""Prefix"" = 'CUS'  WHERE ""ModuleCode"" = 'Customer';
UPDATE sys_serial_number SET ""Prefix"" = 'VEN'  WHERE ""ModuleCode"" = 'Vendor';
UPDATE sys_serial_number SET ""Prefix"" = 'RFQ'  WHERE ""ModuleCode"" = 'RFQ';
UPDATE sys_serial_number SET ""Prefix"" = 'QUO'  WHERE ""ModuleCode"" = 'Quotation';
UPDATE sys_serial_number SET ""Prefix"" = 'SOR'  WHERE ""ModuleCode"" = 'SalesOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'PUR'  WHERE ""ModuleCode"" = 'PurchaseOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'STI'  WHERE ""ModuleCode"" = 'StockIn';
UPDATE sys_serial_number SET ""Prefix"" = 'SOUT' WHERE ""ModuleCode"" = 'StockOut';
UPDATE sys_serial_number SET ""Prefix"" = 'REC'  WHERE ""ModuleCode"" = 'Receipt';
UPDATE sys_serial_number SET ""Prefix"" = 'PAY'  WHERE ""ModuleCode"" = 'Payment';
UPDATE sys_serial_number SET ""Prefix"" = 'INVI' WHERE ""ModuleCode"" = 'InputInvoice';
UPDATE sys_serial_number SET ""Prefix"" = 'OUTI' WHERE ""ModuleCode"" = 'OutputInvoice';
UPDATE sys_serial_number SET ""Prefix"" = 'STK'  WHERE ""ModuleCode"" = 'Stock';
UPDATE sys_serial_number SET ""Prefix"" = 'PRQ'  WHERE ""ModuleCode"" = 'PurchaseRequisition';
UPDATE sys_serial_number SET ""Prefix"" = 'SORQ' WHERE ""ModuleCode"" = 'StockOutRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'PKT'  WHERE ""ModuleCode"" = 'PickingTask';
UPDATE sys_serial_number SET ""Prefix"" = 'ARN'  WHERE ""ModuleCode"" = 'ArrivalNotice';
UPDATE sys_serial_number SET ""Prefix"" = 'QCR'  WHERE ""ModuleCode"" = 'QcRecord';
UPDATE sys_serial_number SET ""Prefix"" = 'PMR'  WHERE ""ModuleCode"" = 'PaymentRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'FNP'  WHERE ""ModuleCode"" = 'FinancePayment';

ALTER TABLE IF EXISTS sys_serial_number
    ALTER COLUMN ""Prefix"" TYPE character varying(4);

-- 新规则固定5位数值位，不再按年月重置
UPDATE sys_serial_number SET
    ""SequenceLength"" = 5,
    ""ResetByYear"" = FALSE,
    ""ResetByMonth"" = FALSE,
    ""LastResetYear"" = NULL,
    ""LastResetMonth"" = NULL;

-- 客户/供应商从0开始（CurrentSequence = -1 后首次生成即 00000）
UPDATE sys_serial_number
SET ""CurrentSequence"" = -1
WHERE ""ModuleCode"" IN ('Customer', 'Vendor') AND ""CurrentSequence"" < 0;

-- 其他业务从十进制2026开始（CurrentSequence = 2025 后首次生成即 2026）
UPDATE sys_serial_number
SET ""CurrentSequence"" = 2025
WHERE ""ModuleCode"" NOT IN ('Customer', 'Vendor') AND ""CurrentSequence"" < 2025;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_sys_serial_number_Prefix'
    ) THEN
        CREATE UNIQUE INDEX ""IX_sys_serial_number_Prefix"" ON sys_serial_number (""Prefix"");
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_sys_serial_number_Prefix"";

ALTER TABLE IF EXISTS sys_serial_number
    ALTER COLUMN ""Prefix"" TYPE character varying(10);

UPDATE sys_serial_number SET ""Prefix"" = 'QT'   WHERE ""ModuleCode"" = 'Quotation';
UPDATE sys_serial_number SET ""Prefix"" = 'SO'   WHERE ""ModuleCode"" = 'SalesOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'PO'   WHERE ""ModuleCode"" = 'PurchaseOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'SI'   WHERE ""ModuleCode"" = 'StockIn';
UPDATE sys_serial_number SET ""Prefix"" = 'INVO' WHERE ""ModuleCode"" = 'OutputInvoice';
UPDATE sys_serial_number SET ""Prefix"" = 'PR'   WHERE ""ModuleCode"" = 'PurchaseRequisition';
UPDATE sys_serial_number SET ""Prefix"" = 'SON'  WHERE ""ModuleCode"" = 'StockOutRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'PK'   WHERE ""ModuleCode"" = 'PickingTask';
UPDATE sys_serial_number SET ""Prefix"" = 'AN'   WHERE ""ModuleCode"" = 'ArrivalNotice';
UPDATE sys_serial_number SET ""Prefix"" = 'QC'   WHERE ""ModuleCode"" = 'QcRecord';
UPDATE sys_serial_number SET ""Prefix"" = 'PRQ'  WHERE ""ModuleCode"" = 'PaymentRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'FPY'  WHERE ""ModuleCode"" = 'FinancePayment';
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 业务流水前缀与产品命名对齐；Prefix 列扩至 16 以支持 PAY_DEL 等。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260401130000_UpdateSysSerialPrefixes202604")]
    public partial class UpdateSysSerialPrefixes202604 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS sys_serial_number
    ALTER COLUMN ""Prefix"" TYPE character varying(16);

UPDATE sys_serial_number SET ""Prefix"" = 'CUS'     WHERE ""ModuleCode"" = 'Customer';
UPDATE sys_serial_number SET ""Prefix"" = 'VEN'     WHERE ""ModuleCode"" = 'Vendor';
UPDATE sys_serial_number SET ""Prefix"" = 'RFQ'     WHERE ""ModuleCode"" = 'RFQ';
UPDATE sys_serial_number SET ""Prefix"" = 'QUO'     WHERE ""ModuleCode"" = 'Quotation';
UPDATE sys_serial_number SET ""Prefix"" = 'SO'      WHERE ""ModuleCode"" = 'SalesOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'PO'      WHERE ""ModuleCode"" = 'PurchaseOrder';
UPDATE sys_serial_number SET ""Prefix"" = 'STI'     WHERE ""ModuleCode"" = 'StockIn';
UPDATE sys_serial_number SET ""Prefix"" = 'STO'     WHERE ""ModuleCode"" = 'StockOut';
UPDATE sys_serial_number SET ""Prefix"" = 'REC'     WHERE ""ModuleCode"" = 'Receipt';
UPDATE sys_serial_number SET ""Prefix"" = 'PAY_DEL' WHERE ""ModuleCode"" = 'Payment';
UPDATE sys_serial_number SET ""Prefix"" = 'INVI'    WHERE ""ModuleCode"" = 'InputInvoice';
UPDATE sys_serial_number SET ""Prefix"" = 'INVO'    WHERE ""ModuleCode"" = 'OutputInvoice';
UPDATE sys_serial_number SET ""Prefix"" = 'STK'     WHERE ""ModuleCode"" = 'Stock';
UPDATE sys_serial_number SET ""Prefix"" = 'POR'     WHERE ""ModuleCode"" = 'PurchaseRequisition';
UPDATE sys_serial_number SET ""Prefix"" = 'STOR'    WHERE ""ModuleCode"" = 'StockOutRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'PAK'     WHERE ""ModuleCode"" = 'PickingTask';
UPDATE sys_serial_number SET ""Prefix"" = 'STIR'    WHERE ""ModuleCode"" = 'ArrivalNotice';
UPDATE sys_serial_number SET ""Prefix"" = 'QC'      WHERE ""ModuleCode"" = 'QcRecord';
UPDATE sys_serial_number SET ""Prefix"" = 'PAYR'    WHERE ""ModuleCode"" = 'PaymentRequest';
UPDATE sys_serial_number SET ""Prefix"" = 'PAY'     WHERE ""ModuleCode"" = 'FinancePayment';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
");
        }
    }
}

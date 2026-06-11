using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 采购订单明细：生产日期/DC 要求（与 sellorderitem.date_code 对齐）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260608200000_PurchaseOrderItemDateCode")]
    public partial class PurchaseOrderItemDateCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE purchaseorderitem
  ADD COLUMN IF NOT EXISTS date_code character varying(100) NULL;

COMMENT ON COLUMN purchaseorderitem.date_code IS '生产日期/DC 要求（字典 ItemCode，如 26+）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE purchaseorderitem DROP COLUMN IF EXISTS date_code;
");
        }
    }
}

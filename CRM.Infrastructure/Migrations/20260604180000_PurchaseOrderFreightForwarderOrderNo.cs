using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 采购订单主表：货代单号（与外部货代系统 1:1 对应）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604180000_PurchaseOrderFreightForwarderOrderNo")]
    public partial class PurchaseOrderFreightForwarderOrderNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE purchaseorder
  ADD COLUMN IF NOT EXISTS freight_forwarder_order_no character varying(64) NULL;

COMMENT ON COLUMN purchaseorder.freight_forwarder_order_no IS '货代系统订单号（与外部仓库一一对应）';

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_purchaseorder_freight_forwarder_order_no""
  ON purchaseorder (freight_forwarder_order_no)
  WHERE freight_forwarder_order_no IS NOT NULL
    AND btrim(freight_forwarder_order_no) <> '';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_purchaseorder_freight_forwarder_order_no"";
ALTER TABLE purchaseorder DROP COLUMN IF EXISTS freight_forwarder_order_no;
");
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 在库明细表 <c>stockitem</c>（与 stockinitem 1:1）；出库明细 <c>StockItemId</c>。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260511120000_StockItemLayer")]
    public partial class StockItemLayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockitem (
  ""StockItemId"" character varying(36) NOT NULL,
  ""StockInItemId"" character varying(36) NOT NULL,
  ""StockInId"" character varying(36) NOT NULL,
  ""StockAggregateId"" character varying(36) NOT NULL,
  ""MaterialId"" character varying(36) NOT NULL,
  ""WarehouseId"" character varying(36) NOT NULL,
  ""LocationId"" character varying(36) NULL,
  ""BatchNo"" character varying(50) NULL,
  ""ProductionDate"" timestamp with time zone NULL,
  ""ExpiryDate"" timestamp with time zone NULL,
  ""Type"" smallint NOT NULL DEFAULT 1,
  ""RegionType"" smallint NOT NULL DEFAULT 10,
  ""purchase_pn"" character varying(200) NULL,
  ""purchase_brand"" character varying(200) NULL,
  ""sell_order_item_id"" character varying(36) NULL,
  ""sell_order_item_code"" character varying(64) NULL,
  ""purchase_order_item_id"" character varying(36) NULL,
  ""purchase_order_item_code"" character varying(64) NULL,
  ""VendorId"" character varying(36) NULL,
  ""VendorName"" character varying(200) NULL,
  ""PurchaserId"" character varying(36) NULL,
  ""PurchaserName"" character varying(100) NULL,
  ""PurchasePrice"" numeric(18,6) NOT NULL DEFAULT 0,
  ""PurchaseAmount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""CustomerId"" character varying(36) NULL,
  ""CustomerName"" character varying(200) NULL,
  ""SalespersonId"" character varying(36) NULL,
  ""SalespersonName"" character varying(100) NULL,
  ""SalesPrice"" numeric(18,6) NULL,
  ""QtyInbound"" integer NOT NULL DEFAULT 0,
  ""QtyStockOut"" integer NOT NULL DEFAULT 0,
  ""QtyOccupy"" integer NOT NULL DEFAULT 0,
  ""QtySales"" integer NOT NULL DEFAULT 0,
  ""QtyRepertory"" integer NOT NULL DEFAULT 0,
  ""QtyRepertoryAvailable"" integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_stockitem"" PRIMARY KEY (""StockItemId""),
  CONSTRAINT ""FK_stockitem_stock"" FOREIGN KEY (""StockAggregateId"") REFERENCES public.stock (""StockId"") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockitem_StockInItemId"" ON public.stockitem (""StockInItemId"");
CREATE INDEX IF NOT EXISTS ""IX_stockitem_StockAggregateId"" ON public.stockitem (""StockAggregateId"");
CREATE INDEX IF NOT EXISTS ""IX_stockitem_pick"" ON public.stockitem (""WarehouseId"", ""sell_order_item_id"", ""MaterialId"");
COMMENT ON TABLE public.stockitem IS '在库明细层，与 stockinitem 1:1；汇总至 stock.StockId';
");

            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutitem ADD COLUMN IF NOT EXISTS ""StockItemId"" character varying(36) NULL;
COMMENT ON COLUMN public.stockoutitem.""StockItemId"" IS '拣货出库绑定的 stockitem 主键';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutitem DROP COLUMN IF EXISTS ""StockItemId"";
DROP TABLE IF EXISTS public.stockitem;
");
        }
    }
}

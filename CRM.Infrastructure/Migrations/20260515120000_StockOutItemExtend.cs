using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 出库明细扩展表：与 stockoutitem 一对一，快照拣货层、采销价与出库业务利润。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260515120000_StockOutItemExtend")]
    public partial class StockOutItemExtend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockoutitemextend (
    ""StockOutItemId"" character varying(36) NOT NULL,
    ""StockItemId"" character varying(36) NULL,
    ""Type"" smallint NOT NULL DEFAULT 1,
    ""sell_order_item_id"" character varying(36) NULL,
    ""sell_order_item_code"" character varying(64) NULL,
    ""purchase_order_item_id"" character varying(36) NULL,
    ""purchase_order_item_code"" character varying(64) NULL,
    ""QtyStockOut"" integer NOT NULL DEFAULT 0,
    ""PurchasePrice"" numeric(18,6) NOT NULL DEFAULT 0,
    ""PurchaseCurrency"" smallint NOT NULL DEFAULT 1,
    ""PurchasePriceUsd"" numeric(18,6) NOT NULL DEFAULT 0,
    ""SalesPrice"" numeric(18,6) NULL,
    ""SalesCurrency"" smallint NULL,
    ""SalesPriceUsd"" numeric(18,6) NULL,
    ""ProfitOutBizUsd"" numeric(18,2) NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_stockoutitemextend"" PRIMARY KEY (""StockOutItemId""),
    CONSTRAINT ""FK_stockoutitemextend_stockoutitem_StockOutItemId"" FOREIGN KEY (""StockOutItemId"") REFERENCES public.stockoutitem (""ItemId"") ON DELETE CASCADE
);

COMMENT ON TABLE public.stockoutitemextend IS '出库明细扩展：拣货层、订单行关联、价税快照与出库业务 USD 利润';
COMMENT ON COLUMN public.stockoutitemextend.""StockItemId"" IS '拣货绑定的 stockitem 主键';
COMMENT ON COLUMN public.stockoutitemextend.""Type"" IS '库存层类型，与 stockitem.Type 一致';
COMMENT ON COLUMN public.stockoutitemextend.""PurchaseCurrency"" IS '采购单价币别';
COMMENT ON COLUMN public.stockoutitemextend.""PurchasePriceUsd"" IS '采购单价折合 USD（过账时按财务基准汇率计算）';
COMMENT ON COLUMN public.stockoutitemextend.""SalesCurrency"" IS '销售单价币别';
COMMENT ON COLUMN public.stockoutitemextend.""SalesPriceUsd"" IS '销售单价折合 USD';
COMMENT ON COLUMN public.stockoutitemextend.""ProfitOutBizUsd"" IS '出库业务 USD 利润快照';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.stockoutitemextend;");
        }
    }
}

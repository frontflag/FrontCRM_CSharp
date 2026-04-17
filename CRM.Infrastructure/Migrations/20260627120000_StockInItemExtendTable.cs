using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 入库明细扩展表：与 stockinitem 一对一，冗余 StockInId 及采销订单行主键/业务编号。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260627120000_StockInItemExtendTable")]
    public partial class StockInItemExtendTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockinitemextend (
    ""StockInItemId"" character varying(36) NOT NULL,
    ""StockInId"" character varying(36) NOT NULL,
    ""sell_order_item_id"" character varying(36) NULL,
    ""sell_order_item_code"" character varying(64) NULL,
    ""purchase_order_item_id"" character varying(36) NULL,
    ""purchase_order_item_code"" character varying(64) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_stockinitemextend"" PRIMARY KEY (""StockInItemId""),
    CONSTRAINT ""FK_stockinitemextend_stockinitem"" FOREIGN KEY (""StockInItemId"")
        REFERENCES public.stockinitem (""ItemId"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_stockinitemextend_StockInId""
    ON public.stockinitemextend (""StockInId"");

COMMENT ON TABLE public.stockinitemextend IS '入库明细扩展：与 stockinitem 一对一（StockInItemId = ItemId）；冗余 StockInId 及采销订单行关联';
COMMENT ON COLUMN public.stockinitemextend.""StockInId"" IS '入库单主键（与 stockinitem.StockInId 一致）';
COMMENT ON COLUMN public.stockinitemextend.""sell_order_item_id"" IS '销售订单明细主键';
COMMENT ON COLUMN public.stockinitemextend.""sell_order_item_code"" IS '销售订单明细业务编号';
COMMENT ON COLUMN public.stockinitemextend.""purchase_order_item_id"" IS '采购订单明细主键';
COMMENT ON COLUMN public.stockinitemextend.""purchase_order_item_code"" IS '采购订单明细业务编号';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.stockinitemextend;");
        }
    }
}

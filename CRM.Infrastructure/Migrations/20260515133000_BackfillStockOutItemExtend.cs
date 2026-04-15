using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 历史 stockoutitem 补全 1:1 扩展行（新出库由 StockOutService 执行出库时写入）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260515133000_BackfillStockOutItemExtend")]
    public partial class BackfillStockOutItemExtend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO public.stockoutitemextend (
    ""StockOutItemId"", ""StockItemId"", ""Type"",
    ""sell_order_item_id"", ""sell_order_item_code"", ""purchase_order_item_id"", ""purchase_order_item_code"",
    ""QtyStockOut"", ""PurchasePrice"", ""PurchaseCurrency"", ""PurchasePriceUsd"",
    ""SalesPrice"", ""SalesCurrency"", ""SalesPriceUsd"", ""ProfitOutBizUsd"",
    ""CreateTime"", ""CreateUserId"", ""ModifyTime"", ""ModifyUserId""
)
SELECT
    i.""ItemId"",
    i.""StockItemId"",
    COALESCE(si.""Type"", stk.""Type"", 1::smallint),
    si.""sell_order_item_id"",
    si.""sell_order_item_code"",
    si.""purchase_order_item_id"",
    si.""purchase_order_item_code"",
    CASE WHEN COALESCE(i.""ActualQty"", 0) > 0 THEN i.""ActualQty"" ELSE i.""Quantity"" END,
    COALESCE(si.""PurchasePrice"", 0),
    COALESCE(si.""PurchaseCurrency"", 1::smallint),
    COALESCE(si.""PurchasePriceUsd"", 0),
    si.""SalesPrice"",
    si.""SalesCurrency"",
    si.""SalesPriceUsd"",
    CASE
        WHEN si.""StockItemId"" IS NOT NULL
             AND si.""sell_order_item_id"" IS NOT NULL
             AND TRIM(si.""sell_order_item_id"") <> ''
             AND si.""SalesPriceUsd"" IS NOT NULL
             AND (CASE WHEN COALESCE(i.""ActualQty"", 0) > 0 THEN i.""ActualQty"" ELSE i.""Quantity"" END) > 0
        THEN ROUND(
            (si.""SalesPriceUsd"" - si.""PurchasePriceUsd"")
            * (CASE WHEN COALESCE(i.""ActualQty"", 0) > 0 THEN i.""ActualQty"" ELSE i.""Quantity"" END)::numeric,
            2)
        ELSE 0
    END,
    i.""CreateTime"",
    i.""CreateUserId"",
    NULL,
    NULL
FROM public.stockoutitem i
LEFT JOIN public.stockitem si ON si.""StockItemId"" = i.""StockItemId""
LEFT JOIN public.stock stk ON stk.""StockId"" = i.""StockId""
WHERE NOT EXISTS (SELECT 1 FROM public.stockoutitemextend e WHERE e.""StockOutItemId"" = i.""ItemId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 更新 sellorderitemextend.PurchasedStock_AvailableQty 列注释：三刷新节点 + 前端申请出库放宽语义。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260510130000_SellOrderItemExtendPurchasedStockAvailableQtyComment")]
    public partial class SellOrderItemExtendPurchasedStockAvailableQtyComment : Migration
    {
        private const string NewComment =
            "同PN+品牌下备货库存(StockType=2)在库可用量之和；入库单过账完成(备货采购)、销售出库扣减备货后、销售明细新建或整单替换后重算；前端大于0时放宽申请出库的采购门槛限制";

        private const string PreviousComment =
            "同PN+品牌下备货库存(StockType=2)在库可用量之和；由入库/出库过账后重算";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $@"COMMENT ON COLUMN public.sellorderitemextend.""PurchasedStock_AvailableQty"" IS '{NewComment}';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $@"COMMENT ON COLUMN public.sellorderitemextend.""PurchasedStock_AvailableQty"" IS '{PreviousComment}';");
        }
    }
}

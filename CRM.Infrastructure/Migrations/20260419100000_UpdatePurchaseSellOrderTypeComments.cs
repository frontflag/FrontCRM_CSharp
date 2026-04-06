using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 与模型一致：订单类型 1=客单采购 2=备货采购 3=样品采购（覆盖早期迁移中的字段注释文案）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260419100000_UpdatePurchaseSellOrderTypeComments")]
    public partial class UpdatePurchaseSellOrderTypeComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
COMMENT ON COLUMN public.purchaseorder.""type"" IS '订单类型：1客单采购 2备货采购 3样品采购';
COMMENT ON COLUMN public.sellorder.""type"" IS '订单类型：1客单采购 2备货采购 3样品采购';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 注释为文档元数据，不回滚
        }
    }
}

using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售明细表增加 <c>customer_pn</c>（客户物料型号），与前端从 <c>comment</c> 解析的语义一致。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260716120000_SellOrderItemCustomerPnColumn")]
    public partial class SellOrderItemCustomerPnColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorderitem
                  ADD COLUMN IF NOT EXISTS customer_pn character varying(200) NULL;
                COMMENT ON COLUMN public.sellorderitem.customer_pn IS '客户物料型号（可与行 comment 中「客户物料型号：」前缀同步；亦可手工维护）';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorderitem DROP COLUMN IF EXISTS customer_pn;
                """);
        }
    }
}

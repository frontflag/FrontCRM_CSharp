using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关明细增加客户、业务员、销售订单明细编号与 <c>sellorderitem</c> 主键快照列。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260615120000_CustomsDeclarationItemSalesSnapshot")]
    public partial class CustomsDeclarationItemSalesSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""customer_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""sales_user_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""sell_order_item_code"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.customs_declaration_item
  ADD COLUMN IF NOT EXISTS ""sell_order_item_id"" character varying(36) NULL;
COMMENT ON COLUMN public.customs_declaration_item.""customer_id"" IS '客户 ID 快照';
COMMENT ON COLUMN public.customs_declaration_item.""sales_user_id"" IS '业务员用户 ID 快照';
COMMENT ON COLUMN public.customs_declaration_item.""sell_order_item_code"" IS '销售订单明细业务编号快照';
COMMENT ON COLUMN public.customs_declaration_item.""sell_order_item_id"" IS '销售订单明细主键 sellorderitem.SellOrderItemId';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""sell_order_item_id"";
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""sell_order_item_code"";
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""sales_user_id"";
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS ""customer_id"";
");
        }
    }
}

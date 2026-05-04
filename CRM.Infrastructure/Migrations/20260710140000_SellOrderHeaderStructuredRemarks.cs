using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售订单主表：订单信息区拆分为独立列，<c>comment</c> 仅保留兼容旧数据（由应用迁移后清空）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260710140000_SellOrderHeaderStructuredRemarks")]
    public partial class SellOrderHeaderStructuredRemarks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorder
                  ADD COLUMN IF NOT EXISTS product_kind character varying(64) NULL,
                  ADD COLUMN IF NOT EXISTS customer_contact_name character varying(200) NULL,
                  ADD COLUMN IF NOT EXISTS invoice_info character varying(500) NULL,
                  ADD COLUMN IF NOT EXISTS payment_terms_text character varying(500) NULL,
                  ADD COLUMN IF NOT EXISTS order_remark character varying(500) NULL;

                COMMENT ON COLUMN public.sellorder.product_kind IS '产品类型（现货/期货/排单/样品等）';
                COMMENT ON COLUMN public.sellorder.customer_contact_name IS '客户联系人（展示名或手工填写）';
                COMMENT ON COLUMN public.sellorder.invoice_info IS '发票信息（公司、税号等）';
                COMMENT ON COLUMN public.sellorder.payment_terms_text IS '账期/付款条款（展示文案）';
                COMMENT ON COLUMN public.sellorder.order_remark IS '订单自由备注（不含结构化前缀行）';
                COMMENT ON COLUMN public.sellorder.comment IS '已弃用：历史多行拼接订单信息；新单不再写入，由应用解析后写入独立列';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS order_remark;
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS payment_terms_text;
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS invoice_info;
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS customer_contact_name;
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS product_kind;
                """);
        }
    }
}

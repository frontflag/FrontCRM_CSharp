using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>装箱单明细扩展：客户订单号、客户型号、客户品牌。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518120000_PackingItemExtendCustomerFields")]
    public partial class PackingItemExtendCustomerFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.packing_item_extend
    ADD COLUMN IF NOT EXISTS customer_so character varying(200) NULL,
    ADD COLUMN IF NOT EXISTS customer_pn character varying(200) NULL,
    ADD COLUMN IF NOT EXISTS customer_brand character varying(200) NULL;

COMMENT ON COLUMN public.packing_item_extend.customer_so IS '客户订单号';
COMMENT ON COLUMN public.packing_item_extend.customer_pn IS '客户型号';
COMMENT ON COLUMN public.packing_item_extend.customer_brand IS '客户品牌';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.packing_item_extend DROP COLUMN IF EXISTS customer_brand;
ALTER TABLE IF EXISTS public.packing_item_extend DROP COLUMN IF EXISTS customer_pn;
ALTER TABLE IF EXISTS public.packing_item_extend DROP COLUMN IF EXISTS customer_so;
");
        }
    }
}

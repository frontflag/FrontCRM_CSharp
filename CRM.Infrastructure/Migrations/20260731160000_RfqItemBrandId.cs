using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260731160000_RfqItemBrandId")]
    public partial class RfqItemBrandId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.rfqitem
  ADD COLUMN IF NOT EXISTS brand_id BIGINT NULL;

COMMENT ON COLUMN public.rfqitem.brand_id IS '供应品牌ID（关联 biz_brand.id）';

CREATE INDEX IF NOT EXISTS ""IX_rfqitem_brand_id""
  ON public.rfqitem (brand_id);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_rfqitem_brand_id"";
ALTER TABLE public.rfqitem DROP COLUMN IF EXISTS brand_id;
");
        }
    }
}

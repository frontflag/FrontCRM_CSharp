using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260731170000_BizBrandSoftDelete")]
    public partial class BizBrandSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.biz_brand
  ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS deleted_at timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL;

COMMENT ON COLUMN public.biz_brand.is_deleted IS '是否已删除（软删除）';
COMMENT ON COLUMN public.biz_brand.deleted_at IS '删除时间';
COMMENT ON COLUMN public.biz_brand.deleted_by_user_id IS '删除操作人用户ID（关联 user.UserId）';

CREATE INDEX IF NOT EXISTS ""IX_biz_brand_is_deleted""
  ON public.biz_brand (is_deleted);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_biz_brand_is_deleted"";
ALTER TABLE public.biz_brand
  DROP COLUMN IF EXISTS deleted_by_user_id,
  DROP COLUMN IF EXISTS deleted_at,
  DROP COLUMN IF EXISTS is_deleted;
");
        }
    }
}

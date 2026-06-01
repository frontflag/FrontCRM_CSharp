using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>销售/采购订单明细软删除时记录操作人（删除日志展示）。</summary>
    public partial class SellOrderItemPurchaseOrderItemDeletedByUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
ALTER TABLE IF EXISTS public.sellorderitem
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_name character varying(100) NULL;
COMMENT ON COLUMN public.sellorderitem.deleted_by_user_id IS '软删除操作人 GUID（与 user 表、JWT 一致）';
COMMENT ON COLUMN public.sellorderitem.deleted_by_user_name IS '软删除操作人登录名（冗余，供删除日志展示）';

ALTER TABLE IF EXISTS public.purchaseorderitem
  ADD COLUMN IF NOT EXISTS deleted_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS deleted_by_user_name character varying(100) NULL;
COMMENT ON COLUMN public.purchaseorderitem.deleted_by_user_id IS '软删除操作人 GUID';
COMMENT ON COLUMN public.purchaseorderitem.deleted_by_user_name IS '软删除操作人登录名（冗余，供删除日志展示）';
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
ALTER TABLE IF EXISTS public.sellorderitem
  DROP COLUMN IF EXISTS deleted_by_user_name,
  DROP COLUMN IF EXISTS deleted_by_user_id;
ALTER TABLE IF EXISTS public.purchaseorderitem
  DROP COLUMN IF EXISTS deleted_by_user_name,
  DROP COLUMN IF EXISTS deleted_by_user_id;
""");
        }
    }
}

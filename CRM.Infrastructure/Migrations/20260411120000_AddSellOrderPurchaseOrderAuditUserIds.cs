using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 销售/采购订单主表增加 create_by_user_id、modify_by_user_id（GUID），与 JWT 用户主键一致；基类 CreateUserId(bigint) 无法表示当前用户体系。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260411120000_AddSellOrderPurchaseOrderAuditUserIds")]
    public partial class AddSellOrderPurchaseOrderAuditUserIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 与 scripts/ensure_master_business_audit_user_ids_202604.sql 可并存：列已存在时不报错
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.sellorder ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.sellorder ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.purchaseorder ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.purchaseorder ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.purchaseorder DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.purchaseorder DROP COLUMN IF EXISTS create_by_user_id;
");
        }
    }
}

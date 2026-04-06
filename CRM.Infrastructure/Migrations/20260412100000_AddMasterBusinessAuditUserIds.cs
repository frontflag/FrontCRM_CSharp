using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 主业务表统一增加 create_by_user_id / modify_by_user_id（GUID）。rfq 仅补 modify；sellorder/purchaseorder 已有列跳过；paymentrequest 表若不存在则跳过。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260412100000_AddMasterBusinessAuditUserIds")]
    public partial class AddMasterBusinessAuditUserIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 与 scripts/ensure_master_business_audit_user_ids_202604.sql 可并存；rfq 仅补 modify（create 见 20260410120000）
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'customerinfo' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.customerinfo ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'customerinfo' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.customerinfo ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'vendorinfo' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.vendorinfo ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'vendorinfo' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.vendorinfo ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'rfq' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.rfq ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'quote' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.quote ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'quote' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.quote ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'purchaserequisition' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.purchaserequisition ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'purchaserequisition' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.purchaserequisition ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockoutrequest' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.stockoutrequest ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockoutrequest' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.stockoutrequest ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'qcinfo' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.qcinfo ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'qcinfo' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.qcinfo ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockin' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.stockin ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockin' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.stockin ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockout' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.stockout ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'stockout' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.stockout ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financereceipt' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.financereceipt ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financereceipt' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.financereceipt ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financepayment' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.financepayment ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financepayment' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.financepayment ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoice' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.financepurchaseinvoice ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoice' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.financepurchaseinvoice ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financesellinvoice' AND column_name = 'create_by_user_id') THEN
    ALTER TABLE public.financesellinvoice ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'financesellinvoice' AND column_name = 'modify_by_user_id') THEN
    ALTER TABLE public.financesellinvoice ADD COLUMN modify_by_user_id character varying(36) NULL;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF to_regclass('public.paymentrequest') IS NOT NULL THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'create_by_user_id') THEN
      ALTER TABLE public.paymentrequest ADD COLUMN create_by_user_id character varying(36) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'modify_by_user_id') THEN
      ALTER TABLE public.paymentrequest ADD COLUMN modify_by_user_id character varying(36) NULL;
    END IF;
  END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF to_regclass('public.paymentrequest') IS NOT NULL THEN
    ALTER TABLE public.paymentrequest DROP COLUMN IF EXISTS modify_by_user_id;
    ALTER TABLE public.paymentrequest DROP COLUMN IF EXISTS create_by_user_id;
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.financesellinvoice DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.financesellinvoice DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.financepurchaseinvoice DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.financepurchaseinvoice DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.financepayment DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.financepayment DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.financereceipt DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.financereceipt DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.purchaserequisition DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.purchaserequisition DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.quote DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.quote DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.rfq DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.vendorinfo DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.vendorinfo DROP COLUMN IF EXISTS create_by_user_id;
ALTER TABLE IF EXISTS public.customerinfo DROP COLUMN IF EXISTS modify_by_user_id;
ALTER TABLE IF EXISTS public.customerinfo DROP COLUMN IF EXISTS create_by_user_id;
");
        }
    }
}

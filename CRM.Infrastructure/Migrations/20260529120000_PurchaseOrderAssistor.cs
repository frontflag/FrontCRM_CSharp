using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>采购订单主表增加采购助理 <c>assistor</c>（用户 GUID）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260529120000_PurchaseOrderAssistor")]
public partial class PurchaseOrderAssistor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'assistor'
  ) THEN
    ALTER TABLE public.purchaseorder ADD COLUMN assistor character varying(36) NULL;
  END IF;
END $$;

COMMENT ON COLUMN public.purchaseorder.assistor IS '采购助理用户ID（sys_user.UserId），负责跟进本采购订单';

CREATE INDEX IF NOT EXISTS ""IX_purchaseorder_assistor""
  ON public.purchaseorder (assistor)
  WHERE assistor IS NOT NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_purchaseorder_assistor"";
ALTER TABLE IF EXISTS public.purchaseorder DROP COLUMN IF EXISTS assistor;
");
    }
}

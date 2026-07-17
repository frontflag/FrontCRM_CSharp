using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>采购订单主表增加后付款标记 <c>is_pay_later</c>。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717120000_PurchaseOrderIsPayLater")]
public partial class PurchaseOrderIsPayLater : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'is_pay_later'
  ) THEN
    ALTER TABLE public.purchaseorder
      ADD COLUMN is_pay_later boolean NOT NULL DEFAULT false;
  END IF;
END $$;

COMMENT ON COLUMN public.purchaseorder.is_pay_later IS
  '后付款：客户付款后再给供应商付款（仅标记提醒，不拦截申请付款）';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.purchaseorder DROP COLUMN IF EXISTS is_pay_later;
");
    }
}

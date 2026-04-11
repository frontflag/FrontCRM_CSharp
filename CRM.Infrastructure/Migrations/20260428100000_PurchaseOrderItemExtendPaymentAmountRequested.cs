using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260428100000_PurchaseOrderItemExtendPaymentAmountRequested")]
    public partial class PurchaseOrderItemExtendPaymentAmountRequested : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.purchaseorderitemextend
  ADD COLUMN IF NOT EXISTS ""PaymentAmountRequested"" numeric(18,2) NOT NULL DEFAULT 0;
COMMENT ON COLUMN public.purchaseorderitemextend.""PaymentAmountRequested"" IS '累计请款金额(有效付款单明细PaymentAmountToBe之和，含待审核)';

-- 按现有付款明细回填（与 PurchaseOrderItemExtendSyncService 口径一致：排除取消、审核失败）
UPDATE public.purchaseorderitemextend e
SET ""PaymentAmountRequested"" = COALESCE(agg.s, 0)
FROM (
  SELECT TRIM(BOTH FROM pi.""PurchaseOrderItemId"") AS pid,
         SUM(pi.""PaymentAmountToBe"") AS s
  FROM public.financepaymentitem pi
  INNER JOIN public.financepayment p ON p.""FinancePaymentId"" = pi.""FinancePaymentId""
  WHERE pi.""PurchaseOrderItemId"" IS NOT NULL
    AND TRIM(BOTH FROM pi.""PurchaseOrderItemId"") <> ''
    AND p.""Status"" NOT IN (-1, -2)
  GROUP BY TRIM(BOTH FROM pi.""PurchaseOrderItemId"")
) agg
WHERE e.""PurchaseOrderItemId"" = agg.pid;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE public.purchaseorderitemextend DROP COLUMN IF EXISTS ""PaymentAmountRequested"";");
        }
    }
}

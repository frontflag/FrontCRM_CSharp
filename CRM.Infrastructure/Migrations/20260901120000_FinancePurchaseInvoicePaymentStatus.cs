using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>进项发票列表付款缓存：PaymentDone / PaymentToBe / PaymentStatus，相对已核销金额。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260901120000_FinancePurchaseInvoicePaymentStatus")]
    public partial class FinancePurchaseInvoicePaymentStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepurchaseinvoice
  ADD COLUMN IF NOT EXISTS ""PaymentDone"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""PaymentToBe"" numeric(18,2) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS ""PaymentStatus"" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.financepurchaseinvoice.""PaymentDone"" IS '已付款金额（相对已核销，派生缓存）';
COMMENT ON COLUMN public.financepurchaseinvoice.""PaymentToBe"" IS '待付款金额 = VerifiedDone - PaymentDone';
COMMENT ON COLUMN public.financepurchaseinvoice.""PaymentStatus"" IS '付款状态 0未付款 1部分 2完成（相对 VerifiedDone）';

WITH wo AS (
  SELECT finance_purchase_invoice_id AS inv_id,
         purchase_order_item_id AS poi,
         SUM(""Amount"") AS link_amt
  FROM public.finance_purchase_invoice_write_off
  WHERE COALESCE(is_deleted, false) = false
    AND purchase_order_item_id IS NOT NULL
    AND length(btrim(purchase_order_item_id)) > 0
  GROUP BY finance_purchase_invoice_id, purchase_order_item_id
),
pay AS (
  SELECT w.inv_id,
         SUM(LEAST(w.link_amt, COALESCE(e.""PaymentAmountFinish"", 0))) AS done
  FROM wo w
  LEFT JOIN public.purchaseorderitemextend e ON e.""PurchaseOrderItemId"" = w.poi
  GROUP BY w.inv_id
)
UPDATE public.financepurchaseinvoice i
SET ""PaymentDone"" = ROUND(COALESCE(p.done, 0), 2),
    ""PaymentToBe"" = GREATEST(0, ROUND(COALESCE(i.""VerifiedDone"", 0) - COALESCE(p.done, 0), 2)),
    ""PaymentStatus"" = CASE
      WHEN COALESCE(p.done, 0) <= 0 THEN 0
      WHEN COALESCE(i.""VerifiedDone"", 0) > 0 AND COALESCE(p.done, 0) + 0.0001 >= COALESCE(i.""VerifiedDone"", 0) THEN 2
      ELSE 1
    END
FROM pay p
WHERE i.""FinancePurchaseInvoiceId"" = p.inv_id;

UPDATE public.financepurchaseinvoice
SET ""PaymentToBe"" = GREATEST(0, ROUND(COALESCE(""VerifiedDone"", 0) - COALESCE(""PaymentDone"", 0), 2))
WHERE COALESCE(""PaymentToBe"", 0) <> GREATEST(0, ROUND(COALESCE(""VerifiedDone"", 0) - COALESCE(""PaymentDone"", 0), 2));
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepurchaseinvoice
  DROP COLUMN IF EXISTS ""PaymentDone"",
  DROP COLUMN IF EXISTS ""PaymentToBe"",
  DROP COLUMN IF EXISTS ""PaymentStatus"";
");
        }
    }
}

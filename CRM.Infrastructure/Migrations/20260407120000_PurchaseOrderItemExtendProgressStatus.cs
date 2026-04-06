using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260407120000_PurchaseOrderItemExtendProgressStatus")]
    public partial class PurchaseOrderItemExtendProgressStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS ""PurchaseProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS ""PurchaseProgressQty"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS ""StockInProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS ""PaymentProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.purchaseorderitemextend ADD COLUMN IF NOT EXISTS ""InvoiceProgressStatus"" smallint NOT NULL DEFAULT 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS ""InvoiceProgressStatus"";
ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS ""PaymentProgressStatus"";
ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS ""StockInProgressStatus"";
ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS ""PurchaseProgressQty"";
ALTER TABLE IF EXISTS public.purchaseorderitemextend DROP COLUMN IF EXISTS ""PurchaseProgressStatus"";
");
        }
    }
}

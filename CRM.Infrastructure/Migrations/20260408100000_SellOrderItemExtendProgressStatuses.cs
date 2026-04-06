using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260408100000_SellOrderItemExtendProgressStatuses")]
    public partial class SellOrderItemExtendProgressStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""PurchaseProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""StockInProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""StockOutProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""ReceiptProgressStatus"" smallint NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS public.sellorderitemextend
    ADD COLUMN IF NOT EXISTS ""InvoiceProgressStatus"" smallint NOT NULL DEFAULT 0;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""InvoiceProgressStatus"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""ReceiptProgressStatus"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""StockOutProgressStatus"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""StockInProgressStatus"";
ALTER TABLE IF EXISTS public.sellorderitemextend DROP COLUMN IF EXISTS ""PurchaseProgressStatus"";
");
        }
    }
}

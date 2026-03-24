using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260323193000_AlignPurchaseOrderStatusWorkflow")]
    public partial class AlignPurchaseOrderStatusWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.purchaseorder
SET status = CASE status
    WHEN 0 THEN 20
    WHEN 1 THEN 30
    WHEN 2 THEN 50
    WHEN 3 THEN 50
    WHEN 4 THEN 50
    WHEN 5 THEN 100
    WHEN -1 THEN -2
    ELSE status
END;

UPDATE public.purchaseorderitem i
SET status = CASE o.status
    WHEN 1 THEN 1
    WHEN 2 THEN 2
    WHEN 10 THEN 10
    WHEN 20 THEN 20
    WHEN 30 THEN 30
    WHEN 50 THEN 50
    WHEN 100 THEN 100
    WHEN -1 THEN -1
    WHEN -2 THEN -2
    ELSE i.status
END
FROM public.purchaseorder o
WHERE i.purchase_order_id = o.""PurchaseOrderId"";

ALTER TABLE public.purchaseorder
ALTER COLUMN status SET DEFAULT 1;

ALTER TABLE public.purchaseorderitem
ALTER COLUMN status SET DEFAULT 1;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE public.purchaseorder
SET status = CASE status
    WHEN 20 THEN 0
    WHEN 30 THEN 1
    WHEN 50 THEN 2
    WHEN 100 THEN 5
    WHEN -2 THEN -1
    ELSE status
END;

UPDATE public.purchaseorderitem i
SET status = CASE o.status
    WHEN 0 THEN 0
    WHEN 1 THEN 0
    WHEN 2 THEN 0
    WHEN 5 THEN 0
    WHEN -1 THEN 1
    ELSE i.status
END
FROM public.purchaseorder o
WHERE i.purchase_order_id = o.""PurchaseOrderId"";

ALTER TABLE public.purchaseorder
ALTER COLUMN status SET DEFAULT 0;

ALTER TABLE public.purchaseorderitem
ALTER COLUMN status SET DEFAULT 0;
");
        }
    }
}

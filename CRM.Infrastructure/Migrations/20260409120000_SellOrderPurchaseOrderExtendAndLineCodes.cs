using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260409120000_SellOrderPurchaseOrderExtendAndLineCodes")]
    public partial class SellOrderPurchaseOrderExtendAndLineCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sellorderextend (
  ""SellOrderId"" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_sellorderextend"" PRIMARY KEY (""SellOrderId""),
  CONSTRAINT ""FK_sellorderextend_sellorder"" FOREIGN KEY (""SellOrderId"")
    REFERENCES public.sellorder (""SellOrderId"") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS public.purchaseorderextend (
  ""PurchaseOrderId"" character varying(36) NOT NULL,
  last_item_line_seq integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  CONSTRAINT ""PK_purchaseorderextend"" PRIMARY KEY (""PurchaseOrderId""),
  CONSTRAINT ""FK_purchaseorderextend_purchaseorder"" FOREIGN KEY (""PurchaseOrderId"")
    REFERENCES public.purchaseorder (""PurchaseOrderId"") ON DELETE CASCADE
);

ALTER TABLE IF EXISTS public.sellorderitem
    ADD COLUMN IF NOT EXISTS sell_order_item_code character varying(64) NULL;
ALTER TABLE IF EXISTS public.purchaseorderitem
    ADD COLUMN IF NOT EXISTS purchase_order_item_code character varying(64) NULL;

INSERT INTO public.sellorderextend (""SellOrderId"", last_item_line_seq, ""CreateTime"")
SELECT so.""SellOrderId"", 0, NOW()
FROM public.sellorder so
WHERE NOT EXISTS (
  SELECT 1 FROM public.sellorderextend x WHERE x.""SellOrderId"" = so.""SellOrderId"");

INSERT INTO public.purchaseorderextend (""PurchaseOrderId"", last_item_line_seq, ""CreateTime"")
SELECT po.""PurchaseOrderId"", 0, NOW()
FROM public.purchaseorder po
WHERE NOT EXISTS (
  SELECT 1 FROM public.purchaseorderextend x WHERE x.""PurchaseOrderId"" = po.""PurchaseOrderId"");

WITH numbered AS (
  SELECT si.""SellOrderItemId"",
    so.sell_order_code,
    ROW_NUMBER() OVER (PARTITION BY si.sell_order_id ORDER BY si.""CreateTime"", si.""SellOrderItemId"") AS rn
  FROM public.sellorderitem si
  INNER JOIN public.sellorder so ON so.""SellOrderId"" = si.sell_order_id
)
UPDATE public.sellorderitem u
SET sell_order_item_code = n.sell_order_code || '-' || n.rn::text
FROM numbered n
WHERE u.""SellOrderItemId"" = n.""SellOrderItemId"";

WITH numbered AS (
  SELECT pi.""PurchaseOrderItemId"",
    po.purchase_order_code,
    ROW_NUMBER() OVER (PARTITION BY pi.purchase_order_id ORDER BY pi.""CreateTime"", pi.""PurchaseOrderItemId"") AS rn
  FROM public.purchaseorderitem pi
  INNER JOIN public.purchaseorder po ON po.""PurchaseOrderId"" = pi.purchase_order_id
)
UPDATE public.purchaseorderitem u
SET purchase_order_item_code = n.purchase_order_code || '-' || n.rn::text
FROM numbered n
WHERE u.""PurchaseOrderItemId"" = n.""PurchaseOrderItemId"";

UPDATE public.sellorderextend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.sellorderitem si WHERE si.sell_order_id = e.""SellOrderId""), 0);

UPDATE public.purchaseorderextend e
SET last_item_line_seq = COALESCE((
  SELECT COUNT(*)::integer FROM public.purchaseorderitem pi WHERE pi.purchase_order_id = e.""PurchaseOrderId""), 0);

ALTER TABLE public.sellorderitem ALTER COLUMN sell_order_item_code SET NOT NULL;
ALTER TABLE public.purchaseorderitem ALTER COLUMN purchase_order_item_code SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_sellorderitem_order_linecode""
  ON public.sellorderitem (sell_order_id, sell_order_item_code);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_purchaseorderitem_order_linecode""
  ON public.purchaseorderitem (purchase_order_id, purchase_order_item_code);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_purchaseorderitem_order_linecode"";
DROP INDEX IF EXISTS public.""IX_sellorderitem_order_linecode"";
ALTER TABLE IF EXISTS public.purchaseorderitem DROP COLUMN IF EXISTS purchase_order_item_code;
ALTER TABLE IF EXISTS public.sellorderitem DROP COLUMN IF EXISTS sell_order_item_code;
DROP TABLE IF EXISTS public.purchaseorderextend;
DROP TABLE IF EXISTS public.sellorderextend;
");
        }
    }
}

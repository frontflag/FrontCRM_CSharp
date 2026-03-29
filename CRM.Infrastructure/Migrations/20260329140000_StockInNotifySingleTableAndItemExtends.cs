using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260329140000_StockInNotifySingleTableAndItemExtends")]
    public partial class StockInNotifySingleTableAndItemExtends : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 物流单表重构：清空旧主从数据（到货/质检）
TRUNCATE TABLE public.qcitem RESTART IDENTITY CASCADE;
TRUNCATE TABLE public.qcinfo RESTART IDENTITY CASCADE;
TRUNCATE TABLE public.stockinnotifyitem RESTART IDENTITY CASCADE;
TRUNCATE TABLE public.stockinnotify RESTART IDENTITY CASCADE;

-- 到货通知：行级字段
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NOT NULL DEFAULT '';
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""Pn"" character varying(128) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""Brand"" character varying(64) NULL;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""ExpectQty"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""ReceiveQty"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""PassedQty"" numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""Cost"" numeric(18,6) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""ExpectTotal"" numeric(18,2) NOT NULL DEFAULT 0;
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""ReceiveTotal"" numeric(18,2) NOT NULL DEFAULT 0;

ALTER TABLE public.stockinnotify ALTER COLUMN ""PurchaseOrderItemId"" DROP DEFAULT;

DROP TABLE IF EXISTS public.stockinnotifyitem;

-- 质检明细：关联到货单行
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema='public' AND table_name='qcitem' AND column_name='StockInNotifyItemId'
  ) AND NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema='public' AND table_name='qcitem' AND column_name='ArrivalStockInNotifyId'
  ) THEN
    ALTER TABLE public.qcitem RENAME COLUMN ""StockInNotifyItemId"" TO ""ArrivalStockInNotifyId"";
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.purchaseorderitemextend (
    ""PurchaseOrderItemId"" character varying(36) NOT NULL,
    ""QtyStockInNotifyNot"" numeric(18,4) NOT NULL DEFAULT 0,
    ""QtyStockInNotifyExpectSum"" numeric(18,4) NOT NULL DEFAULT 0,
    ""QtyReceiveTotal"" numeric(18,4) NOT NULL DEFAULT 0,
    ""PurchaseInvoiceAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PurchaseInvoiceDone"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PurchaseInvoiceToBe"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmountNot"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmountFinish"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmountNot"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmountFinish"" numeric(18,2) NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_purchaseorderitemextend"" PRIMARY KEY (""PurchaseOrderItemId"")
);

CREATE TABLE IF NOT EXISTS public.sellorderitemextend (
    ""SellOrderItemId"" character varying(36) NOT NULL,
    ""QtyStockOutNotify"" numeric(18,4) NOT NULL DEFAULT 0,
    ""QtyStockOutNotifyNot"" numeric(18,4) NOT NULL DEFAULT 0,
    ""InvoiceAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""InvoiceAmountNot"" numeric(18,2) NOT NULL DEFAULT 0,
    ""InvoiceAmountFinish"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmountNot"" numeric(18,2) NOT NULL DEFAULT 0,
    ""ReceiptAmountFinish"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmountDone"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PaymentAmountToBe"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PurchaseInvoiceAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""PurchaseInvoiceDone"" numeric(18,2) NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_sellorderitemextend"" PRIMARY KEY (""SellOrderItemId"")
);

-- 已有采购/销售明细：补扩展行
INSERT INTO public.purchaseorderitemextend (
  ""PurchaseOrderItemId"", ""QtyStockInNotifyNot"", ""QtyStockInNotifyExpectSum"", ""QtyReceiveTotal"",
  ""PurchaseInvoiceAmount"", ""PurchaseInvoiceDone"", ""PurchaseInvoiceToBe"",
  ""PaymentAmount"", ""PaymentAmountNot"", ""PaymentAmountFinish"",
  ""ReceiptAmount"", ""ReceiptAmountNot"", ""ReceiptAmountFinish"",
  ""CreateTime""
)
SELECT
  i.""PurchaseOrderItemId"",
  i.qty,
  0,
  0,
  ROUND((i.qty * i.cost)::numeric, 2),
  0,
  ROUND((i.qty * i.cost)::numeric, 2),
  ROUND((i.qty * i.cost)::numeric, 2),
  ROUND((i.qty * i.cost)::numeric, 2),
  0,
  0,
  0,
  0,
  NOW() AT TIME ZONE 'utc'
FROM public.purchaseorderitem i
WHERE NOT EXISTS (
  SELECT 1 FROM public.purchaseorderitemextend e WHERE e.""PurchaseOrderItemId"" = i.""PurchaseOrderItemId""
);

INSERT INTO public.sellorderitemextend (
  ""SellOrderItemId"", ""QtyStockOutNotify"", ""QtyStockOutNotifyNot"",
  ""InvoiceAmount"", ""InvoiceAmountNot"", ""InvoiceAmountFinish"",
  ""ReceiptAmount"", ""ReceiptAmountNot"", ""ReceiptAmountFinish"",
  ""PaymentAmount"", ""PaymentAmountDone"", ""PaymentAmountToBe"",
  ""PurchaseInvoiceAmount"", ""PurchaseInvoiceDone"",
  ""CreateTime""
)
SELECT
  s.""SellOrderItemId"",
  0,
  s.qty,
  ROUND((s.qty * s.price)::numeric, 2),
  ROUND((s.qty * s.price)::numeric, 2),
  0,
  ROUND((s.qty * s.price)::numeric, 2),
  ROUND((s.qty * s.price)::numeric, 2),
  0,
  0,
  0,
  0,
  0,
  0,
  NOW() AT TIME ZONE 'utc'
FROM public.sellorderitem s
WHERE NOT EXISTS (
  SELECT 1 FROM public.sellorderitemextend e WHERE e.""SellOrderItemId"" = s.""SellOrderItemId""
);
");

            migrationBuilder.Sql(@"
CREATE INDEX IF NOT EXISTS ""IX_stockinnotify_PurchaseOrderItemId"" ON public.stockinnotify (""PurchaseOrderItemId"");
CREATE INDEX IF NOT EXISTS ""IX_stockinnotify_PurchaseOrderId"" ON public.stockinnotify (""PurchaseOrderId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ""IX_stockinnotify_PurchaseOrderItemId"";
DROP INDEX IF EXISTS ""IX_stockinnotify_PurchaseOrderId"";
DROP TABLE IF EXISTS public.sellorderitemextend;
DROP TABLE IF EXISTS public.purchaseorderitemextend;
-- 无法自动恢复 stockinnotifyitem 旧结构，需自备份还原
");
        }
    }
}

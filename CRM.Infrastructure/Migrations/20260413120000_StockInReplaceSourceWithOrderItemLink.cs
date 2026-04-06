using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockin：去掉 SourceCode/SourceId，改为采购/销售明细行关联字段；迁移前按旧 SourceId 尽力回填。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260413120000_StockInReplaceSourceWithOrderItemLink")]
    public partial class StockInReplaceSourceWithOrderItemLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""PurchaseOrderItemId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SellOrderItemCode"" character varying(64) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SellOrderItemId"" character varying(36) NULL;

-- 质检链路：SourceId 曾为质检单主键（qcinfo.UserId）
UPDATE public.stockin s
SET
  ""PurchaseOrderItemId"" = n.""PurchaseOrderItemId"",
  ""SellOrderItemId"" = NULLIF(TRIM(n.""SellOrderItemId""), '')
FROM public.qcinfo q
INNER JOIN public.stockinnotify n ON n.""UserId"" = q.""StockInNotifyId""
WHERE s.""StockInType"" = 1
  AND s.""SourceId"" IS NOT NULL
  AND TRIM(s.""SourceId"") = q.""UserId"";

-- 采购单主键：按入库明细 MaterialId 对齐采购行
UPDATE public.stockin s
SET
  ""PurchaseOrderItemId"" = m.""PurchaseOrderItemId"",
  ""PurchaseOrderItemCode"" = m.purchase_order_item_code,
  ""SellOrderItemId"" = NULLIF(TRIM(m.sell_order_item_id), ''),
  ""SellOrderItemCode"" = m.sell_order_item_code
FROM (
  SELECT DISTINCT ON (sin.""StockInId"")
    sin.""StockInId"" AS sid,
    pi.""PurchaseOrderItemId"",
    pi.purchase_order_item_code,
    pi.sell_order_item_id,
    soi.sell_order_item_code
  FROM public.stockin sin
  INNER JOIN public.stockinitem si ON si.""StockInId"" = sin.""StockInId""
  INNER JOIN public.purchaseorder po ON po.""PurchaseOrderId"" = NULLIF(TRIM(sin.""SourceId""), '')
  INNER JOIN public.purchaseorderitem pi ON pi.purchase_order_id = po.""PurchaseOrderId""
    AND (
      pi.""PurchaseOrderItemId"" = si.""MaterialId""
      OR pi.product_id = si.""MaterialId""
      OR (pi.pn IS NOT NULL AND LOWER(TRIM(pi.pn)) = LOWER(TRIM(si.""MaterialId"")))
    )
  LEFT JOIN public.sellorderitem soi ON soi.""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), '')
  WHERE sin.""StockInType"" = 1
    AND (sin.""PurchaseOrderItemId"" IS NULL OR TRIM(sin.""PurchaseOrderItemId"") = '')
  ORDER BY sin.""StockInId"", pi.""PurchaseOrderItemId""
) m
WHERE s.""StockInId"" = m.sid;

-- 历史曾把采购单号写入 SourceId
UPDATE public.stockin s
SET
  ""PurchaseOrderItemId"" = m.""PurchaseOrderItemId"",
  ""PurchaseOrderItemCode"" = m.purchase_order_item_code,
  ""SellOrderItemId"" = NULLIF(TRIM(m.sell_order_item_id), ''),
  ""SellOrderItemCode"" = m.sell_order_item_code
FROM (
  SELECT DISTINCT ON (sin.""StockInId"")
    sin.""StockInId"" AS sid,
    pi.""PurchaseOrderItemId"",
    pi.purchase_order_item_code,
    pi.sell_order_item_id,
    soi.sell_order_item_code
  FROM public.stockin sin
  INNER JOIN public.stockinitem si ON si.""StockInId"" = sin.""StockInId""
  INNER JOIN public.purchaseorder po ON po.purchase_order_code = TRIM(sin.""SourceId"")
  INNER JOIN public.purchaseorderitem pi ON pi.purchase_order_id = po.""PurchaseOrderId""
    AND (
      pi.""PurchaseOrderItemId"" = si.""MaterialId""
      OR pi.product_id = si.""MaterialId""
      OR (pi.pn IS NOT NULL AND LOWER(TRIM(pi.pn)) = LOWER(TRIM(si.""MaterialId"")))
    )
  LEFT JOIN public.sellorderitem soi ON soi.""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), '')
  WHERE sin.""StockInType"" = 1
    AND (sin.""PurchaseOrderItemId"" IS NULL OR TRIM(sin.""PurchaseOrderItemId"") = '')
  ORDER BY sin.""StockInId"", pi.""PurchaseOrderItemId""
) m
WHERE s.""StockInId"" = m.sid;

-- 单明细采购单：仍无行级关联时整单落到唯一一行
UPDATE public.stockin s
SET
  ""PurchaseOrderItemId"" = pi.""PurchaseOrderItemId"",
  ""PurchaseOrderItemCode"" = pi.purchase_order_item_code,
  ""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), ''),
  ""SellOrderItemCode"" = soi.sell_order_item_code
FROM public.purchaseorderitem pi
LEFT JOIN public.sellorderitem soi ON soi.""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), '')
WHERE s.""StockInType"" = 1
  AND s.""SourceId"" IS NOT NULL
  AND TRIM(s.""SourceId"") = pi.purchase_order_id
  AND (s.""PurchaseOrderItemId"" IS NULL OR TRIM(s.""PurchaseOrderItemId"") = '')
  AND (SELECT COUNT(*)::integer FROM public.purchaseorderitem cx WHERE cx.purchase_order_id = pi.purchase_order_id) = 1;

UPDATE public.stockin s
SET
  ""PurchaseOrderItemId"" = pi.""PurchaseOrderItemId"",
  ""PurchaseOrderItemCode"" = pi.purchase_order_item_code,
  ""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), ''),
  ""SellOrderItemCode"" = soi.sell_order_item_code
FROM public.purchaseorder po
INNER JOIN public.purchaseorderitem pi ON pi.purchase_order_id = po.""PurchaseOrderId""
LEFT JOIN public.sellorderitem soi ON soi.""SellOrderItemId"" = NULLIF(TRIM(pi.sell_order_item_id), '')
WHERE s.""StockInType"" = 1
  AND s.""SourceId"" IS NOT NULL
  AND TRIM(s.""SourceId"") = po.purchase_order_code
  AND (s.""PurchaseOrderItemId"" IS NULL OR TRIM(s.""PurchaseOrderItemId"") = '')
  AND (SELECT COUNT(*)::integer FROM public.purchaseorderitem cx WHERE cx.purchase_order_id = po.""PurchaseOrderId"") = 1;

UPDATE public.stockin s
SET ""PurchaseOrderItemCode"" = pi.purchase_order_item_code
FROM public.purchaseorderitem pi
WHERE s.""PurchaseOrderItemId"" = pi.""PurchaseOrderItemId""
  AND (s.""PurchaseOrderItemCode"" IS NULL OR TRIM(s.""PurchaseOrderItemCode"") = '');

UPDATE public.stockin s
SET ""SellOrderItemCode"" = soi.sell_order_item_code
FROM public.sellorderitem soi
WHERE NULLIF(TRIM(s.""SellOrderItemId""), '') IS NOT NULL
  AND soi.""SellOrderItemId"" = NULLIF(TRIM(s.""SellOrderItemId""), '')
  AND (s.""SellOrderItemCode"" IS NULL OR TRIM(s.""SellOrderItemCode"") = '');

ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SourceCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SourceId"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SourceCode"" character varying(32) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SourceId"" character varying(36) NULL;

UPDATE public.stockin s
SET ""SourceId"" = pi.purchase_order_id
FROM public.purchaseorderitem pi
WHERE s.""PurchaseOrderItemId"" = pi.""PurchaseOrderItemId"";

UPDATE public.stockin s
SET ""SourceCode"" = po.purchase_order_code
FROM public.purchaseorderitem pi
INNER JOIN public.purchaseorder po ON po.""PurchaseOrderId"" = pi.purchase_order_id
WHERE s.""PurchaseOrderItemId"" = pi.""PurchaseOrderItemId"";

ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""PurchaseOrderItemCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""PurchaseOrderItemId"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SellOrderItemCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SellOrderItemId"";
");
        }
    }
}

-- 单价字段扩展为 numeric(18,6)（与迁移 20260401140000_UnitPriceNumeric18Scale6 一致）
-- 在 PostgreSQL 上执行；总额类字段保持 numeric(18,2) 不变。

DO $BODY$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockinitem') THEN
    ALTER TABLE public.stock_in_item ALTER COLUMN "Price" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutitem') THEN
    ALTER TABLE public.stock_out_item ALTER COLUMN "Price" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'rfqitem') THEN
    ALTER TABLE public.rfqitem ALTER COLUMN target_price TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoiceitem') THEN
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN "StockInCost" TYPE numeric(18,6);
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN "BillCost" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'sellinvoiceitem') THEN
    ALTER TABLE public.sellinvoiceitem ALTER COLUMN "Price" TYPE numeric(18,6);
  END IF;
END
$BODY$;


-- stock_out_item.packing_id ← packing."Id"
ALTER TABLE IF EXISTS public.stock_out_item
    ADD COLUMN IF NOT EXISTS packing_id character varying(36) NULL;

COMMENT ON COLUMN public.stock_out_item.packing_id IS '装箱单主键，对应 packing."Id"';

CREATE INDEX IF NOT EXISTS "IX_stock_out_item_packing_id"
    ON public.stock_out_item (packing_id)
    WHERE COALESCE(is_deleted, false) = false
      AND packing_id IS NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_stock_out_item_packing_packing_id'
    ) THEN
        ALTER TABLE public.stock_out_item
            ADD CONSTRAINT "FK_stock_out_item_packing_packing_id"
            FOREIGN KEY (packing_id) REFERENCES public.packing ("Id")
            ON DELETE SET NULL;
    END IF;
END $$;

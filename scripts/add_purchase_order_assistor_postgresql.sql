-- purchaseorder：采购助理（跟进人）
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'purchaseorder' AND column_name = 'assistor'
  ) THEN
    ALTER TABLE public.purchaseorder ADD COLUMN assistor character varying(36) NULL;
  END IF;
END $$;

COMMENT ON COLUMN public.purchaseorder.assistor IS '采购助理用户ID（sys_user.UserId），负责跟进本采购订单';

CREATE INDEX IF NOT EXISTS "IX_purchaseorder_assistor"
  ON public.purchaseorder (assistor)
  WHERE assistor IS NOT NULL;

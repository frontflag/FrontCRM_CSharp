-- 拣货任务以装箱单为主（迁移 20260525100000）

ALTER TABLE IF EXISTS public.pickingtask
    DROP COLUMN IF EXISTS "StockOutRequestId";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_pickingtask_packing_id_active"
    ON public.pickingtask (packing_id)
    WHERE COALESCE(is_deleted, false) = false
      AND packing_id IS NOT NULL
      AND TRIM(packing_id) <> ''
      AND "Status" <> -1;

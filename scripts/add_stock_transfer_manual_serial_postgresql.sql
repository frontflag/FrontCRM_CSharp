-- 手工移库单流水号（与迁移 20260424120000_AddStockTransferManualSerialNumber 等价，可单独执行）
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'StockTransferManual') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'StockTransferManual', '手工移库单', 'STM', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;

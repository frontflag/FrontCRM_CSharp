-- 报关公司流水号（与迁移 20260616100000_SysSerialCustomsBroker 等价，可单独执行）
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'CustomsBroker') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'CustomsBroker', '报关公司', 'CBR', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;

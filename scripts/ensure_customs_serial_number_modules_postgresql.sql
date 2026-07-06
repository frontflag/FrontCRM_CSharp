-- =============================================================================
-- 补齐报关相关 sys_serial_number 模块行（可重复执行）
-- 用途：确认报关装箱 / 生成报关单时报错
--   「未找到业务模块 'CustomsDeclaration' 的流水号配置，请先初始化。」
-- =============================================================================

DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'CustomsBroker') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'CustomsBroker', '报关公司', 'CBR', 5, -1, false, false, timezone('utc', now()));
  END IF;

  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'StockTransfer') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'StockTransfer', '移库单', 'STF', 5, -1, false, false, timezone('utc', now()));
  END IF;

  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'CustomsDeclaration') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'CustomsDeclaration', '报关单', 'CDS', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;

SELECT "ModuleCode", "ModuleName", "Prefix", "CurrentSequence"
FROM public.sys_serial_number
WHERE "ModuleCode" IN ('CustomsBroker', 'StockTransfer', 'CustomsDeclaration')
ORDER BY "ModuleCode";

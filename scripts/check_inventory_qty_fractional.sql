-- Phase 0: 上线前体检 — 库存域数量列是否存在非整数（应在改 int 前跑一遍，计数为 0 再执行迁移）。
-- 规则：与迁移 20260511120000 一致，使用 round 后与 trunc 比较可发现半整数差异；此处用 fract_part <> 0 检测。

SELECT 'stock' AS tbl, COUNT(*) AS non_integer_rows
FROM public.stock
WHERE ("Qty" - trunc("Qty")) <> 0
   OR ("QtyStockOut" - trunc("QtyStockOut")) <> 0
   OR ("QtyOccupy" - trunc("QtyOccupy")) <> 0
   OR ("QtySales" - trunc("QtySales")) <> 0
   OR ("QtyRepertory" - trunc("QtyRepertory")) <> 0
   OR ("QtyRepertoryAvailable" - trunc("QtyRepertoryAvailable")) <> 0

UNION ALL SELECT 'stockin', COUNT(*) FROM public.stock_in
WHERE ("TotalQuantity" - trunc("TotalQuantity")) <> 0

UNION ALL SELECT 'stockinitem', COUNT(*) FROM public.stock_in_item
WHERE ("Quantity" - trunc("Quantity")) <> 0
   OR ("OrderQty" - trunc("OrderQty")) <> 0
   OR ("QtyReceived" - trunc("QtyReceived")) <> 0

UNION ALL SELECT 'stockout', COUNT(*) FROM public.stock_out
WHERE ("TotalQuantity" - trunc("TotalQuantity")) <> 0

UNION ALL SELECT 'stockoutitem', COUNT(*) FROM public.stock_out_item
WHERE ("Quantity" - trunc("Quantity")) <> 0
   OR ("OrderQty" - trunc("OrderQty")) <> 0
   OR ("PlanQty" - trunc("PlanQty")) <> 0
   OR ("PickQty" - trunc("PickQty")) <> 0
   OR ("ActualQty" - trunc("ActualQty")) <> 0

UNION ALL SELECT 'stockledger', COUNT(*) FROM public.stockledger
WHERE ("QtyIn" - trunc("QtyIn")) <> 0 OR ("QtyOut" - trunc("QtyOut")) <> 0

UNION ALL SELECT 'stockoutrequest', COUNT(*) FROM public.stockoutrequest
WHERE ("Quantity" - trunc("Quantity")) <> 0

UNION ALL SELECT 'pickingtaskitem', COUNT(*) FROM public.pickingtaskitem
WHERE ("PlanQty" - trunc("PlanQty")) <> 0 OR ("PickedQty" - trunc("PickedQty")) <> 0

UNION ALL SELECT 'stockinnotify', COUNT(*) FROM public.stockinnotify
WHERE ("ExpectQty" - trunc("ExpectQty")) <> 0
   OR ("ReceiveQty" - trunc("ReceiveQty")) <> 0
   OR ("PassedQty" - trunc("PassedQty")) <> 0

UNION ALL SELECT 'qcinfo', COUNT(*) FROM public.qcinfo
WHERE ("PassQty" - trunc("PassQty")) <> 0 OR ("RejectQty" - trunc("RejectQty")) <> 0

UNION ALL SELECT 'qcitem', COUNT(*) FROM public.qcitem
WHERE ("ArrivedQty" - trunc("ArrivedQty")) <> 0
   OR ("PassedQty" - trunc("PassedQty")) <> 0
   OR ("RejectQty" - trunc("RejectQty")) <> 0

UNION ALL SELECT 'inventorycountitem', COUNT(*) FROM public.inventorycountitem
WHERE ("BookQty" - trunc("BookQty")) <> 0
   OR ("CountQty" - trunc("CountQty")) <> 0
   OR ("DiffQty" - trunc("DiffQty")) <> 0;


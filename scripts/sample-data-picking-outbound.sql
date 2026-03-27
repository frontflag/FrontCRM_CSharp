-- =============================================================================
-- 样例数据：执行出库 / 生成拣货任务（ProductId 与库存 MaterialId 对齐）
-- 数据库：PostgreSQL（与 prod-migration.sql 表结构一致）
-- 说明：
--   1. 库存 stock."MaterialId" = 销售订单明细 sellorderitem.product_id（GUID）
--   2. 出库通知 stockoutrequest."MaterialCode" 为 PN 字符串亦可；后端用订单行 ProductId 匹配库存
--   3. 请在开发/测试库执行；主键为固定 UUID，与生产冲突时请改 ID 后再执行
--   4. 【重要】「生成拣货任务」按所选仓库 ID 过滤库存。界面「test (01)」对应 warehouseinfo."WarehouseCode"='01'。
--      本脚本将库存写入编码为 01 的仓库；若无该仓库则写入 SAMPLE01 样例仓库（此时请在界面选「样例仓库」）。
-- =============================================================================

BEGIN;

-- 固定主键（可按需替换整组）
-- 共 5 套：a1000001 … a1000005（同一后缀：002=stock，003=sellorder，004=item，005=stockoutrequest）
-- 物料 ProductId：b2000001 … b2000005（与对应 stock.MaterialId 一致）

DELETE FROM stockoutrequest WHERE "UserId" IN (
    'a1000001-0000-4000-8000-000000000005',
    'a1000002-0000-4000-8000-000000000005',
    'a1000003-0000-4000-8000-000000000005',
    'a1000004-0000-4000-8000-000000000005',
    'a1000005-0000-4000-8000-000000000005'
);
DELETE FROM stock WHERE "StockId" IN (
    'a1000001-0000-4000-8000-000000000002',
    'a1000002-0000-4000-8000-000000000002',
    'a1000003-0000-4000-8000-000000000002',
    'a1000004-0000-4000-8000-000000000002',
    'a1000005-0000-4000-8000-000000000002'
);
DELETE FROM sellorder WHERE "SellOrderId" IN (
    'a1000001-0000-4000-8000-000000000003',
    'a1000002-0000-4000-8000-000000000003',
    'a1000003-0000-4000-8000-000000000003',
    'a1000004-0000-4000-8000-000000000003',
    'a1000005-0000-4000-8000-000000000003'
); -- CASCADE sellorderitem

-- 可选：仅当本脚本曾插入过样例仓库时清理（忽略不存在）
DELETE FROM warehouseinfo WHERE "Id" = 'a1000001-0000-4000-8000-000000000001' AND "WarehouseCode" = 'SAMPLE01';

-- 无「01」仓库时插入备用样例仓（与历史脚本 Id 一致，避免重复执行冲突）
INSERT INTO warehouseinfo (
    "Id", "WarehouseCode", "WarehouseName", "Address", "Status",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000001-0000-4000-8000-000000000001',
    'SAMPLE01',
    '样例仓库（拣货联调）',
    NULL,
    1,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE NOT EXISTS (SELECT 1 FROM warehouseinfo WHERE "WarehouseCode" = 'SAMPLE01');

-- 可用库存：QtyRepertoryAvailable = QtyRepertory - QtyOccupy - QtySales；QtyRepertory = Qty - QtyStockOut
-- 优先挂到「编码 01」（界面 xxx（01））；否则挂到 SAMPLE01
INSERT INTO stock (
    "StockId", "MaterialId", "WarehouseId", "LocationId", "Unit", "BatchNo",
    "ProductionDate", "ExpiryDate",
    "Quantity", "AvailableQuantity", "LockedQuantity",
    "Qty", "QtyStockOut", "QtyOccupy", "QtySales", "QtyRepertory", "QtyRepertoryAvailable",
    "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000001-0000-4000-8000-000000000002',
    'b2000001-0000-4000-8000-000000000001',
    COALESCE(
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
    ),
    NULL,
    'PCS',
    'BATCH-SAMPLE-001',
    NULL,
    NULL,
    100.0000, 100.0000, 0.0000,
    100.0000, 0.0000, 0.0000, 0.0000, 100.0000, 100.0000,
    1,
    '样例库存：用于生成拣货任务（与仓库编码 01 或 SAMPLE01 对齐）',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE COALESCE(
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
) IS NOT NULL;

-- 样例 2：另一物料 + 出库通知（数量 5）
INSERT INTO stock (
    "StockId", "MaterialId", "WarehouseId", "LocationId", "Unit", "BatchNo",
    "ProductionDate", "ExpiryDate",
    "Quantity", "AvailableQuantity", "LockedQuantity",
    "Qty", "QtyStockOut", "QtyOccupy", "QtySales", "QtyRepertory", "QtyRepertoryAvailable",
    "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000002-0000-4000-8000-000000000002',
    'b2000002-0000-4000-8000-000000000001',
    COALESCE(
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
    ),
    NULL,
    'PCS',
    'BATCH-SAMPLE-002',
    NULL,
    NULL,
    100.0000, 100.0000, 0.0000,
    100.0000, 0.0000, 0.0000, 0.0000, 100.0000, 100.0000,
    1,
    '样例库存 2',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE COALESCE(
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
) IS NOT NULL;

-- 样例 3：再一条物料 + 出库通知（数量 8）
INSERT INTO stock (
    "StockId", "MaterialId", "WarehouseId", "LocationId", "Unit", "BatchNo",
    "ProductionDate", "ExpiryDate",
    "Quantity", "AvailableQuantity", "LockedQuantity",
    "Qty", "QtyStockOut", "QtyOccupy", "QtySales", "QtyRepertory", "QtyRepertoryAvailable",
    "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000003-0000-4000-8000-000000000002',
    'b2000003-0000-4000-8000-000000000001',
    COALESCE(
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
    ),
    NULL,
    'PCS',
    'BATCH-SAMPLE-003',
    NULL,
    NULL,
    100.0000, 100.0000, 0.0000,
    100.0000, 0.0000, 0.0000, 0.0000, 100.0000, 100.0000,
    1,
    '样例库存 3',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE COALESCE(
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
) IS NOT NULL;

-- 样例 4 / 5：库存行（物料 D/E，数量联调 12 / 6）
INSERT INTO stock (
    "StockId", "MaterialId", "WarehouseId", "LocationId", "Unit", "BatchNo",
    "ProductionDate", "ExpiryDate",
    "Quantity", "AvailableQuantity", "LockedQuantity",
    "Qty", "QtyStockOut", "QtyOccupy", "QtySales", "QtyRepertory", "QtyRepertoryAvailable",
    "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000004-0000-4000-8000-000000000002',
    'b2000004-0000-4000-8000-000000000001',
    COALESCE(
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
    ),
    NULL,
    'PCS',
    'BATCH-SAMPLE-004',
    NULL,
    NULL,
    100.0000, 100.0000, 0.0000,
    100.0000, 0.0000, 0.0000, 0.0000, 100.0000, 100.0000,
    1,
    '样例库存 4',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE COALESCE(
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
) IS NOT NULL;

INSERT INTO stock (
    "StockId", "MaterialId", "WarehouseId", "LocationId", "Unit", "BatchNo",
    "ProductionDate", "ExpiryDate",
    "Quantity", "AvailableQuantity", "LockedQuantity",
    "Qty", "QtyStockOut", "QtyOccupy", "QtySales", "QtyRepertory", "QtyRepertoryAvailable",
    "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
)
SELECT
    'a1000005-0000-4000-8000-000000000002',
    'b2000005-0000-4000-8000-000000000001',
    COALESCE(
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
        (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
    ),
    NULL,
    'PCS',
    'BATCH-SAMPLE-005',
    NULL,
    NULL,
    100.0000, 100.0000, 0.0000,
    100.0000, 0.0000, 0.0000, 0.0000, 100.0000, 100.0000,
    1,
    '样例库存 5',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
WHERE COALESCE(
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = '01' LIMIT 1),
    (SELECT w."Id" FROM warehouseinfo w WHERE w."WarehouseCode" = 'SAMPLE01' LIMIT 1)
) IS NOT NULL;

INSERT INTO sellorder (
    "SellOrderId", sell_order_code, customer_id, customer_name, sales_user_id, sales_user_name,
    purchase_group_id, status, err_status, type, currency, total, convert_total, item_rows,
    purchase_order_status, stock_out_status, stock_in_status,
    finance_receipt_status, finance_payment_status, invoice_status,
    delivery_address, delivery_date, comment, audit_remark,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000001-0000-4000-8000-000000000003',
    'SO-SAMPLE-PICK-001',
    'c3000001-0000-4000-8000-000000000001',
    '样例客户',
    NULL,
    NULL,
    NULL,
    1,
    0,
    1,
    1,
    1000.00,
    1000.00,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO sellorderitem (
    "SellOrderItemId", sell_order_id, quote_id, product_id, pn, brand, customer_pn_no,
    qty, purchased_qty, price, currency, date_code, delivery_date, status, comment,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000001-0000-4000-8000-000000000004',
    'a1000001-0000-4000-8000-000000000003',
    NULL,
    'b2000001-0000-4000-8000-000000000001',
    'DEBUG-MPK-SAMPLE-001',
    'SampleBrand',
    NULL,
    10.0000,
    0.0000,
    100.000000,
    1,
    NULL,
    NULL,
    0,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO stockoutrequest (
    "UserId", "RequestCode", "SalesOrderId", "SalesOrderItemId", "MaterialCode", "MaterialName",
    "Quantity", "CustomerId", "RequestUserId", "RequestDate", "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000001-0000-4000-8000-000000000005',
    'SON-SAMPLE-PICK-001',
    'a1000001-0000-4000-8000-000000000003',
    'a1000001-0000-4000-8000-000000000004',
    'DEBUG-MPK-SAMPLE-001',
    '样例物料',
    10.0000,
    'c3000001-0000-4000-8000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    0,
    '样例出库通知',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

-- 样例 2：销售订单 + 明细 + 出库通知（PN DEBUG-MPK-SAMPLE-002，数量 5）
INSERT INTO sellorder (
    "SellOrderId", sell_order_code, customer_id, customer_name, sales_user_id, sales_user_name,
    purchase_group_id, status, err_status, type, currency, total, convert_total, item_rows,
    purchase_order_status, stock_out_status, stock_in_status,
    finance_receipt_status, finance_payment_status, invoice_status,
    delivery_address, delivery_date, comment, audit_remark,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000002-0000-4000-8000-000000000003',
    'SO-SAMPLE-PICK-002',
    'c3000001-0000-4000-8000-000000000001',
    '样例客户',
    NULL,
    NULL,
    NULL,
    1,
    0,
    1,
    1,
    500.00,
    500.00,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO sellorderitem (
    "SellOrderItemId", sell_order_id, quote_id, product_id, pn, brand, customer_pn_no,
    qty, purchased_qty, price, currency, date_code, delivery_date, status, comment,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000002-0000-4000-8000-000000000004',
    'a1000002-0000-4000-8000-000000000003',
    NULL,
    'b2000002-0000-4000-8000-000000000001',
    'DEBUG-MPK-SAMPLE-002',
    'SampleBrand-B',
    NULL,
    5.0000,
    0.0000,
    100.000000,
    1,
    NULL,
    NULL,
    0,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO stockoutrequest (
    "UserId", "RequestCode", "SalesOrderId", "SalesOrderItemId", "MaterialCode", "MaterialName",
    "Quantity", "CustomerId", "RequestUserId", "RequestDate", "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000002-0000-4000-8000-000000000005',
    'SON-SAMPLE-PICK-002',
    'a1000002-0000-4000-8000-000000000003',
    'a1000002-0000-4000-8000-000000000004',
    'DEBUG-MPK-SAMPLE-002',
    '样例物料-B',
    5.0000,
    'c3000001-0000-4000-8000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    0,
    '样例出库通知 2',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

-- 样例 3：销售订单 + 明细 + 出库通知（PN DEBUG-MPK-SAMPLE-003，数量 8）
INSERT INTO sellorder (
    "SellOrderId", sell_order_code, customer_id, customer_name, sales_user_id, sales_user_name,
    purchase_group_id, status, err_status, type, currency, total, convert_total, item_rows,
    purchase_order_status, stock_out_status, stock_in_status,
    finance_receipt_status, finance_payment_status, invoice_status,
    delivery_address, delivery_date, comment, audit_remark,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000003-0000-4000-8000-000000000003',
    'SO-SAMPLE-PICK-003',
    'c3000001-0000-4000-8000-000000000001',
    '样例客户',
    NULL,
    NULL,
    NULL,
    1,
    0,
    1,
    1,
    800.00,
    800.00,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO sellorderitem (
    "SellOrderItemId", sell_order_id, quote_id, product_id, pn, brand, customer_pn_no,
    qty, purchased_qty, price, currency, date_code, delivery_date, status, comment,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000003-0000-4000-8000-000000000004',
    'a1000003-0000-4000-8000-000000000003',
    NULL,
    'b2000003-0000-4000-8000-000000000001',
    'DEBUG-MPK-SAMPLE-003',
    'SampleBrand-C',
    NULL,
    8.0000,
    0.0000,
    100.000000,
    1,
    NULL,
    NULL,
    0,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO stockoutrequest (
    "UserId", "RequestCode", "SalesOrderId", "SalesOrderItemId", "MaterialCode", "MaterialName",
    "Quantity", "CustomerId", "RequestUserId", "RequestDate", "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000003-0000-4000-8000-000000000005',
    'SON-SAMPLE-PICK-003',
    'a1000003-0000-4000-8000-000000000003',
    'a1000003-0000-4000-8000-000000000004',
    'DEBUG-MPK-SAMPLE-003',
    '样例物料-C',
    8.0000,
    'c3000001-0000-4000-8000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    0,
    '样例出库通知 3',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

-- 样例 4：销售订单 + 明细 + 出库通知（PN DEBUG-MPK-SAMPLE-004，数量 12）
INSERT INTO sellorder (
    "SellOrderId", sell_order_code, customer_id, customer_name, sales_user_id, sales_user_name,
    purchase_group_id, status, err_status, type, currency, total, convert_total, item_rows,
    purchase_order_status, stock_out_status, stock_in_status,
    finance_receipt_status, finance_payment_status, invoice_status,
    delivery_address, delivery_date, comment, audit_remark,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000004-0000-4000-8000-000000000003',
    'SO-SAMPLE-PICK-004',
    'c3000001-0000-4000-8000-000000000001',
    '样例客户',
    NULL,
    NULL,
    NULL,
    1,
    0,
    1,
    1,
    1200.00,
    1200.00,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO sellorderitem (
    "SellOrderItemId", sell_order_id, quote_id, product_id, pn, brand, customer_pn_no,
    qty, purchased_qty, price, currency, date_code, delivery_date, status, comment,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000004-0000-4000-8000-000000000004',
    'a1000004-0000-4000-8000-000000000003',
    NULL,
    'b2000004-0000-4000-8000-000000000001',
    'DEBUG-MPK-SAMPLE-004',
    'SampleBrand-D',
    NULL,
    12.0000,
    0.0000,
    100.000000,
    1,
    NULL,
    NULL,
    0,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO stockoutrequest (
    "UserId", "RequestCode", "SalesOrderId", "SalesOrderItemId", "MaterialCode", "MaterialName",
    "Quantity", "CustomerId", "RequestUserId", "RequestDate", "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000004-0000-4000-8000-000000000005',
    'SON-SAMPLE-PICK-004',
    'a1000004-0000-4000-8000-000000000003',
    'a1000004-0000-4000-8000-000000000004',
    'DEBUG-MPK-SAMPLE-004',
    '样例物料-D',
    12.0000,
    'c3000001-0000-4000-8000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    0,
    '样例出库通知 4',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

-- 样例 5：销售订单 + 明细 + 出库通知（PN DEBUG-MPK-SAMPLE-005，数量 6）
INSERT INTO sellorder (
    "SellOrderId", sell_order_code, customer_id, customer_name, sales_user_id, sales_user_name,
    purchase_group_id, status, err_status, type, currency, total, convert_total, item_rows,
    purchase_order_status, stock_out_status, stock_in_status,
    finance_receipt_status, finance_payment_status, invoice_status,
    delivery_address, delivery_date, comment, audit_remark,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000005-0000-4000-8000-000000000003',
    'SO-SAMPLE-PICK-005',
    'c3000001-0000-4000-8000-000000000001',
    '样例客户',
    NULL,
    NULL,
    NULL,
    1,
    0,
    1,
    1,
    600.00,
    600.00,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    NULL,
    NULL,
    NULL,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO sellorderitem (
    "SellOrderItemId", sell_order_id, quote_id, product_id, pn, brand, customer_pn_no,
    qty, purchased_qty, price, currency, date_code, delivery_date, status, comment,
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000005-0000-4000-8000-000000000004',
    'a1000005-0000-4000-8000-000000000003',
    NULL,
    'b2000005-0000-4000-8000-000000000001',
    'DEBUG-MPK-SAMPLE-005',
    'SampleBrand-E',
    NULL,
    6.0000,
    0.0000,
    100.000000,
    1,
    NULL,
    NULL,
    0,
    NULL,
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

INSERT INTO stockoutrequest (
    "UserId", "RequestCode", "SalesOrderId", "SalesOrderItemId", "MaterialCode", "MaterialName",
    "Quantity", "CustomerId", "RequestUserId", "RequestDate", "Status", "Remark",
    "CreateTime", "CreateUserId", "ModifyTime", "ModifyUserId"
) VALUES (
    'a1000005-0000-4000-8000-000000000005',
    'SON-SAMPLE-PICK-005',
    'a1000005-0000-4000-8000-000000000003',
    'a1000005-0000-4000-8000-000000000004',
    'DEBUG-MPK-SAMPLE-005',
    '样例物料-E',
    6.0000,
    'c3000001-0000-4000-8000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    0,
    '样例出库通知 5',
    TIMESTAMPTZ '2026-03-27T00:00:00Z',
    NULL,
    NULL,
    NULL
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM stock WHERE "StockId" = 'a1000001-0000-4000-8000-000000000002') THEN
    RAISE EXCEPTION '未写入库存：请确认存在 warehouseinfo."WarehouseCode"=''01''（界面 test（01））或 SAMPLE01。可先插入其一再重跑脚本。';
  END IF;
END $$;

COMMIT;

-- -----------------------------------------------------------------------------
-- 前端/API 联调时可用的关键字段（请按实际接口字段名调整）
-- -----------------------------------------------------------------------------
-- 五套独立数据（出库通知 ID / 通知单号 / PN / 数量）：
--   1) a1000001-...005 / SON-SAMPLE-PICK-001 / DEBUG-MPK-SAMPLE-001 / 10
--   2) a1000002-...005 / SON-SAMPLE-PICK-002 / DEBUG-MPK-SAMPLE-002 / 5
--   3) a1000003-...005 / SON-SAMPLE-PICK-003 / DEBUG-MPK-SAMPLE-003 / 8
--   4) a1000004-...005 / SON-SAMPLE-PICK-004 / DEBUG-MPK-SAMPLE-004 / 12
--   5) a1000005-...005 / SON-SAMPLE-PICK-005 / DEBUG-MPK-SAMPLE-005 / 6
-- 仓库：选「test（01）」或「样例仓库（SAMPLE01）」，须与各 stock 行 WarehouseId 一致
-- -----------------------------------------------------------------------------

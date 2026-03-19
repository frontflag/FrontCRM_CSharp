-- 创建 sys_serial_number 表（如果不存在）
CREATE TABLE IF NOT EXISTS sys_serial_number (
    "Id" SERIAL PRIMARY KEY,
    "ModuleCode" VARCHAR(50) NOT NULL,
    "ModuleName" VARCHAR(100) NOT NULL,
    "Prefix" VARCHAR(10) NOT NULL,
    "SequenceLength" INTEGER NOT NULL DEFAULT 4,
    "CurrentSequence" INTEGER NOT NULL DEFAULT 0,
    "ResetByYear" BOOLEAN NOT NULL DEFAULT FALSE,
    "ResetByMonth" BOOLEAN NOT NULL DEFAULT FALSE,
    "LastResetYear" INTEGER,
    "LastResetMonth" INTEGER,
    "Remark" VARCHAR(200),
    "CreateTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdateTime" TIMESTAMP WITH TIME ZONE
);

-- 创建唯一索引
CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_serial_number_ModuleCode" 
ON sys_serial_number ("ModuleCode");

-- 插入初始数据
INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
VALUES 
    (1, 'Customer', '客户', 'Cus', 4, 0, FALSE, FALSE, NOW()),
    (2, 'Vendor', '供应商', 'Ven', 4, 0, FALSE, FALSE, NOW()),
    (3, 'Inquiry', '询价/需求', 'INQ', 4, 0, FALSE, FALSE, NOW()),
    (4, 'Quotation', '报价', 'QUO', 4, 0, FALSE, FALSE, NOW()),
    (5, 'SalesOrder', '销售订单', 'SO', 4, 0, FALSE, FALSE, NOW()),
    (6, 'PurchaseOrder', '采购订单', 'PO', 4, 0, FALSE, FALSE, NOW()),
    (7, 'StockIn', '入库', 'SIN', 4, 0, FALSE, FALSE, NOW()),
    (8, 'StockOut', '出库', 'SOUT', 4, 0, FALSE, FALSE, NOW()),
    (9, 'Inventory', '库存调整', 'INV', 4, 0, FALSE, FALSE, NOW()),
    (10, 'Receipt', '收款', 'REC', 4, 0, FALSE, FALSE, NOW()),
    (11, 'Payment', '付款', 'PAY', 4, 0, FALSE, FALSE, NOW()),
    (12, 'InputInvoice', '进项发票', 'VINV', 4, 0, FALSE, FALSE, NOW()),
    (13, 'OutputInvoice', '销项发票', 'SINV', 4, 0, FALSE, FALSE, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 查询验证
SELECT * FROM sys_serial_number ORDER BY "Id";

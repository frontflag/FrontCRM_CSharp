-- 创建 sys_serial_number 表（如果不存在）
CREATE TABLE IF NOT EXISTS sys_serial_number (
    "Id" SERIAL PRIMARY KEY,
    "ModuleCode" VARCHAR(50) NOT NULL,
    "ModuleName" VARCHAR(100) NOT NULL,
    "Prefix" VARCHAR(4) NOT NULL,
    "SequenceLength" INTEGER NOT NULL DEFAULT 5,
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
    (1, 'Customer', '客户', 'CUS', 5, -1, FALSE, FALSE, NOW()),
    (2, 'Vendor', '供应商', 'VEN', 5, -1, FALSE, FALSE, NOW()),
    (3, 'RFQ', '询价/需求', 'RFQ', 5, 2025, FALSE, FALSE, NOW()),
    (4, 'Quotation', '报价', 'QUO', 5, 2025, FALSE, FALSE, NOW()),
    (5, 'SalesOrder', '销售订单', 'SOR', 5, 2025, FALSE, FALSE, NOW()),
    (6, 'PurchaseOrder', '采购订单', 'PUR', 5, 2025, FALSE, FALSE, NOW()),
    (7, 'StockIn', '入库', 'STI', 5, 2025, FALSE, FALSE, NOW()),
    (8, 'StockOut', '出库', 'SOUT', 5, 2025, FALSE, FALSE, NOW()),
    (9, 'Stock', '库存调整', 'STK', 5, 2025, FALSE, FALSE, NOW()),
    (10, 'Receipt', '收款', 'REC', 5, 2025, FALSE, FALSE, NOW()),
    (11, 'Payment', '付款', 'PAY', 5, 2025, FALSE, FALSE, NOW()),
    (12, 'InputInvoice', '进项发票', 'INVI', 5, 2025, FALSE, FALSE, NOW()),
    (13, 'OutputInvoice', '销项发票', 'OUTI', 5, 2025, FALSE, FALSE, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 查询验证
SELECT * FROM sys_serial_number ORDER BY "Id";

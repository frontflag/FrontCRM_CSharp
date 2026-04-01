-- 创建 sys_serial_number 表（如果不存在）
CREATE TABLE IF NOT EXISTS sys_serial_number (
    "Id" SERIAL PRIMARY KEY,
    "ModuleCode" VARCHAR(50) NOT NULL,
    "ModuleName" VARCHAR(100) NOT NULL,
    "Prefix" VARCHAR(16) NOT NULL,
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

CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_serial_number_ModuleCode"
ON sys_serial_number ("ModuleCode");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_sys_serial_number_Prefix"
ON sys_serial_number ("Prefix");

-- 与 ApplicationDbContext 种子 / 20260401130000 迁移一致
INSERT INTO sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
VALUES
    (1, 'Customer', '客户', 'CUS', 5, -1, FALSE, FALSE, NOW()),
    (2, 'Vendor', '供应商', 'VEN', 5, -1, FALSE, FALSE, NOW()),
    (3, 'RFQ', '询价/需求', 'RFQ', 5, 2025, FALSE, FALSE, NOW()),
    (4, 'Quotation', '报价', 'QUO', 5, 2025, FALSE, FALSE, NOW()),
    (5, 'SalesOrder', '销售订单', 'SO', 5, 2025, FALSE, FALSE, NOW()),
    (6, 'PurchaseOrder', '采购订单', 'PO', 5, 2025, FALSE, FALSE, NOW()),
    (7, 'StockIn', '入库', 'STI', 5, 2025, FALSE, FALSE, NOW()),
    (8, 'StockOut', '出库', 'STO', 5, 2025, FALSE, FALSE, NOW()),
    (9, 'Receipt', '收款', 'REC', 5, 2025, FALSE, FALSE, NOW()),
    (11, 'Payment', '付款', 'PAY_DEL', 5, 2025, FALSE, FALSE, NOW()),
    (12, 'InputInvoice', '进项发票', 'INVI', 5, 2025, FALSE, FALSE, NOW()),
    (13, 'OutputInvoice', '销项发票', 'INVO', 5, 2025, FALSE, FALSE, NOW()),
    (14, 'Stock', '库存', 'STK', 5, 2025, FALSE, FALSE, NOW()),
    (15, 'PurchaseRequisition', '采购申请', 'POR', 5, 2025, FALSE, FALSE, NOW()),
    (16, 'StockOutRequest', '出库申请', 'STOR', 5, 2025, FALSE, FALSE, NOW()),
    (17, 'PickingTask', '拣货任务', 'PAK', 5, 2025, FALSE, FALSE, NOW()),
    (18, 'ArrivalNotice', '到货通知', 'STIR', 5, 2025, FALSE, FALSE, NOW()),
    (19, 'QcRecord', '质检', 'QC', 5, 2025, FALSE, FALSE, NOW()),
    (20, 'PaymentRequest', '请款', 'PAYR', 5, 2025, FALSE, FALSE, NOW()),
    (21, 'FinancePayment', '财务付款', 'PAY', 5, 2025, FALSE, FALSE, NOW())
ON CONFLICT ("Id") DO NOTHING;

SELECT * FROM sys_serial_number ORDER BY "Id";

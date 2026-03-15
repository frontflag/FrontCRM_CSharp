-- ============================================================
-- EBS 新增业务表 - PostgreSQL
-- 包含：财务表(发票、付款、收款) + 系统日志表
-- ============================================================

SET search_path TO public;

-- ============================================================
-- 一、财务管理模块
-- ============================================================

-- 1. 发票主表
DROP TABLE IF EXISTS "invoiceitem" CASCADE;
DROP TABLE IF EXISTS "invoice" CASCADE;

CREATE TABLE IF NOT EXISTS "invoice" (
    "InvoiceId" char(36) NOT NULL,
    "InvoiceNo" varchar(50) NOT NULL,
    "InvoiceCode" varchar(50),
    "InvoiceType" smallint NOT NULL,
    "InvoiceCategory" smallint DEFAULT 1,
    "OrderType" varchar(50),
    "OrderId" char(36),
    "OrderCode" varchar(50),
    "CustomerId" char(36),
    "CustomerName" varchar(200),
    "VendorId" char(36),
    "VendorName" varchar(200),
    "InvoiceDate" timestamp NOT NULL,
    "Amount" numeric(18,2) DEFAULT 0.00,
    "TaxRate" numeric(5,2) DEFAULT 13.00,
    "TaxAmount" numeric(18,2) DEFAULT 0.00,
    "TotalAmount" numeric(18,2) DEFAULT 0.00,
    "Currency" smallint DEFAULT 1,
    "ExchangeRate" numeric(18,6) DEFAULT 1.000000,
    "SellerName" varchar(200),
    "SellerTaxNo" varchar(50),
    "SellerAddressPhone" varchar(200),
    "SellerBankAccount" varchar(200),
    "BuyerName" varchar(200),
    "BuyerTaxNo" varchar(50),
    "BuyerAddressPhone" varchar(200),
    "BuyerBankAccount" varchar(200),
    "Status" smallint DEFAULT 0,
    "CertificationDate" timestamp,
    "IsDeducted" boolean DEFAULT false,
    "DeductionDate" timestamp,
    "ScanFilePath" varchar(500),
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("InvoiceId")
);

-- 2. 发票明细表
CREATE TABLE IF NOT EXISTS "invoiceitem" (
    "ItemId" char(36) NOT NULL,
    "InvoiceId" char(36) NOT NULL,
    "LineNo" integer DEFAULT 1,
    "GoodsName" varchar(200),
    "Specification" varchar(100),
    "Unit" varchar(20),
    "Quantity" numeric(18,4) DEFAULT 0.0000,
    "UnitPrice" numeric(18,4) DEFAULT 0.0000,
    "Amount" numeric(18,2) DEFAULT 0.00,
    "TaxRate" numeric(5,2) DEFAULT 13.00,
    "TaxAmount" numeric(18,2) DEFAULT 0.00,
    "OrderItemId" char(36),
    "Remark" varchar(200),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("ItemId"),
    CONSTRAINT "FK_InvoiceItem_Invoice" FOREIGN KEY ("InvoiceId") REFERENCES "invoice"("InvoiceId") ON DELETE CASCADE
);

-- 3. 付款单主表
DROP TABLE IF EXISTS "paymentitem" CASCADE;
DROP TABLE IF EXISTS "payment" CASCADE;

CREATE TABLE IF NOT EXISTS "payment" (
    "PaymentId" char(36) NOT NULL,
    "PaymentCode" varchar(32) NOT NULL,
    "PaymentType" smallint DEFAULT 1,
    "VendorId" char(36) NOT NULL,
    "VendorName" varchar(200),
    "PurchaseOrderId" char(36),
    "PurchaseOrderCode" varchar(50),
    "InvoiceId" char(36),
    "InvoiceNo" varchar(50),
    "ApplyDate" timestamp DEFAULT CURRENT_TIMESTAMP,
    "PaymentDate" timestamp,
    "ApplyAmount" numeric(18,2) DEFAULT 0.00,
    "PaymentAmount" numeric(18,2) DEFAULT 0.00,
    "Currency" smallint DEFAULT 1,
    "ExchangeRate" numeric(18,6) DEFAULT 1.000000,
    "PaymentMethod" smallint DEFAULT 1,
    "PaymentAccountId" char(36),
    "PaymentAccountName" varchar(100),
    "PaymentBank" varchar(100),
    "ReceiveAccount" varchar(50),
    "ReceiveBank" varchar(100),
    "Payee" varchar(50),
    "Status" smallint DEFAULT 0,
    "ApplicantId" char(36),
    "ApplicantName" varchar(50),
    "ApproverId" char(36),
    "ApproverName" varchar(50),
    "ApproveTime" timestamp,
    "ApproveRemark" varchar(500),
    "PayerId" char(36),
    "PayerName" varchar(50),
    "BankSerialNo" varchar(100),
    "IsAdvancePayment" boolean DEFAULT false,
    "AdvanceRate" numeric(5,2),
    "Purpose" varchar(200),
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("PaymentId")
);

-- 4. 付款单明细表
CREATE TABLE IF NOT EXISTS "paymentitem" (
    "ItemId" char(36) NOT NULL,
    "PaymentId" char(36) NOT NULL,
    "LineNo" integer DEFAULT 1,
    "PurchaseOrderId" char(36),
    "PurchaseOrderCode" varchar(50),
    "PaymentAmount" numeric(18,2) DEFAULT 0.00,
    "OrderTotalAmount" numeric(18,2) DEFAULT 0.00,
    "PaidAmount" numeric(18,2) DEFAULT 0.00,
    "UnpaidAmount" numeric(18,2) DEFAULT 0.00,
    "Remark" varchar(200),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("ItemId"),
    CONSTRAINT "FK_PaymentItem_Payment" FOREIGN KEY ("PaymentId") REFERENCES "payment"("PaymentId") ON DELETE CASCADE
);

-- 5. 收款单主表
DROP TABLE IF EXISTS "receiptitem" CASCADE;
DROP TABLE IF EXISTS "receipt" CASCADE;

CREATE TABLE IF NOT EXISTS "receipt" (
    "ReceiptId" char(36) NOT NULL,
    "ReceiptCode" varchar(32) NOT NULL,
    "ReceiptType" smallint DEFAULT 1,
    "CustomerId" char(36) NOT NULL,
    "CustomerName" varchar(200),
    "SellOrderId" char(36),
    "SellOrderCode" varchar(50),
    "InvoiceId" char(36),
    "InvoiceNo" varchar(50),
    "ReceiptDate" timestamp DEFAULT CURRENT_TIMESTAMP,
    "ReceivableAmount" numeric(18,2) DEFAULT 0.00,
    "ReceiptAmount" numeric(18,2) DEFAULT 0.00,
    "DiscountAmount" numeric(18,2) DEFAULT 0.00,
    "Currency" smallint DEFAULT 1,
    "ExchangeRate" numeric(18,6) DEFAULT 1.000000,
    "ReceiptMethod" smallint DEFAULT 1,
    "ReceiptAccountId" char(36),
    "ReceiptAccountName" varchar(100),
    "ReceiptBank" varchar(100),
    "PayerAccount" varchar(50),
    "PayerBank" varchar(100),
    "Payer" varchar(50),
    "Status" smallint DEFAULT 0,
    "HandlerId" char(36),
    "HandlerName" varchar(50),
    "ApproverId" char(36),
    "ApproverName" varchar(50),
    "ApproveTime" timestamp,
    "ApproveRemark" varchar(500),
    "ConfirmerId" char(36),
    "ConfirmerName" varchar(50),
    "BankSerialNo" varchar(100),
    "IsAdvanceReceipt" boolean DEFAULT false,
    "AdvanceRate" numeric(5,2),
    "Purpose" varchar(200),
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("ReceiptId")
);

-- 6. 收款单明细表
CREATE TABLE IF NOT EXISTS "receiptitem" (
    "ItemId" char(36) NOT NULL,
    "ReceiptId" char(36) NOT NULL,
    "LineNo" integer DEFAULT 1,
    "SellOrderId" char(36),
    "SellOrderCode" varchar(50),
    "ReceiptAmount" numeric(18,2) DEFAULT 0.00,
    "OrderTotalAmount" numeric(18,2) DEFAULT 0.00,
    "ReceivedAmount" numeric(18,2) DEFAULT 0.00,
    "UnreceivedAmount" numeric(18,2) DEFAULT 0.00,
    "Remark" varchar(200),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("ItemId"),
    CONSTRAINT "FK_ReceiptItem_Receipt" FOREIGN KEY ("ReceiptId") REFERENCES "receipt"("ReceiptId") ON DELETE CASCADE
);

-- ============================================================
-- 二、系统管理模块
-- ============================================================

-- 7. 业务操作日志主表
DROP TABLE IF EXISTS "businesslogdetail" CASCADE;
DROP TABLE IF EXISTS "businesslog" CASCADE;

CREATE TABLE IF NOT EXISTS "businesslog" (
    "LogId" char(36) NOT NULL,
    "BusinessModule" smallint NOT NULL,
    "ActionType" smallint NOT NULL,
    "DocumentType" varchar(50) NOT NULL,
    "BusinessDataId" char(36) NOT NULL,
    "DocumentCode" varchar(50),
    "OperatorId" char(36) NOT NULL,
    "OperatorName" varchar(50),
    "OperationTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "IpAddress" varchar(50),
    "DeviceInfo" varchar(200),
    "OperationResult" boolean DEFAULT true,
    "ResultMessage" varchar(200),
    "OperationDescription" varchar(500),
    "OperationSummary" varchar(1000),
    "RelatedDocumentId" char(36),
    "RelatedDocumentType" varchar(50),
    "ApprovalFlowId" char(36),
    "ApprovalNodeId" char(36),
    "DataSource" smallint DEFAULT 1,
    "TenantId" char(36),
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("LogId")
);

-- 8. 业务操作日志明细表
CREATE TABLE IF NOT EXISTS "businesslogdetail" (
    "DetailId" char(36) NOT NULL,
    "LogId" char(36) NOT NULL,
    "FieldName" varchar(50) NOT NULL,
    "FieldDescription" varchar(100),
    "OldValue" varchar(1000),
    "NewValue" varchar(1000),
    "ChangeType" smallint DEFAULT 2,
    "DataType" varchar(20),
    "IsKeyField" boolean DEFAULT false,
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("DetailId"),
    CONSTRAINT "FK_BusinessLogDetail_BusinessLog" FOREIGN KEY ("LogId") REFERENCES "businesslog"("LogId") ON DELETE CASCADE
);

-- ============================================================
-- 三、创建索引
-- ============================================================

-- 发票表索引
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Type" ON "invoice" ("InvoiceType");
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Order" ON "invoice" ("OrderType", "OrderId");
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Customer" ON "invoice" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Vendor" ON "invoice" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Date" ON "invoice" ("InvoiceDate");
CREATE INDEX IF NOT EXISTS "IDX_Invoice_Status" ON "invoice" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_InvoiceItem_Invoice" ON "invoiceitem" ("InvoiceId");

-- 付款表索引
CREATE INDEX IF NOT EXISTS "IDX_Payment_Vendor" ON "payment" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_Payment_Order" ON "payment" ("PurchaseOrderId");
CREATE INDEX IF NOT EXISTS "IDX_Payment_Status" ON "payment" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_Payment_Date" ON "payment" ("PaymentDate");
CREATE INDEX IF NOT EXISTS "IDX_PaymentItem_Payment" ON "paymentitem" ("PaymentId");

-- 收款表索引
CREATE INDEX IF NOT EXISTS "IDX_Receipt_Customer" ON "receipt" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_Receipt_Order" ON "receipt" ("SellOrderId");
CREATE INDEX IF NOT EXISTS "IDX_Receipt_Status" ON "receipt" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_Receipt_Date" ON "receipt" ("ReceiptDate");
CREATE INDEX IF NOT EXISTS "IDX_ReceiptItem_Receipt" ON "receiptitem" ("ReceiptId");

-- 日志表索引
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Module" ON "businesslog" ("BusinessModule");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Action" ON "businesslog" ("ActionType");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_DocType" ON "businesslog" ("DocumentType");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_DataId" ON "businesslog" ("BusinessDataId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Operator" ON "businesslog" ("OperatorId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Time" ON "businesslog" ("OperationTime");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLogDetail_Log" ON "businesslogdetail" ("LogId");

-- ============================================================
-- 四、验证表创建
-- ============================================================

SELECT '新增表创建完成!' AS message;

SELECT 
    table_name,
    '已创建' AS status
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN (
        'invoice', 'invoiceitem',
        'payment', 'paymentitem',
        'receipt', 'receiptitem',
        'businesslog', 'businesslogdetail'
    )
ORDER BY table_name;

-- 统计新表数量
SELECT 
    COUNT(*) as new_table_count,
    '财务表6个 + 日志表2个 = 8个新表' as description
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN (
        'invoice', 'invoiceitem',
        'payment', 'paymentitem',
        'receipt', 'receiptitem',
        'businesslog', 'businesslogdetail'
    );

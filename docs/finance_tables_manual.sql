-- =====================================================
-- 财务模块建表脚本 (FrontCRM)
-- 生成时间: 2026-03-18
-- 包含: 付款、收款、进项发票、销项发票 主表+明细表
-- =====================================================

-- 付款单主表
CREATE TABLE IF NOT EXISTS financepayment (
    "FinancePaymentId"     VARCHAR(36)      NOT NULL PRIMARY KEY,
    "FinancePaymentCode"   VARCHAR(16)      NOT NULL,
    "VendorId"             VARCHAR(36)      NOT NULL,
    "VendorName"           VARCHAR(200),
    "Status"               SMALLINT         NOT NULL DEFAULT 0,
    "PaymentAmountToBe"    NUMERIC(18,2)    NOT NULL DEFAULT 0,
    "PaymentAmount"        NUMERIC(18,2)    NOT NULL DEFAULT 0,
    "PaymentTotalAmount"   NUMERIC(18,2)    NOT NULL DEFAULT 0,
    "PaymentCurrency"      SMALLINT         NOT NULL DEFAULT 1,
    "PaymentDate"          TIMESTAMPTZ,
    "PaymentUserId"        VARCHAR(36),
    "PaymentMode"          SMALLINT         NOT NULL DEFAULT 1,
    "Remark"               VARCHAR(500),
    "CreateTime"           TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
    "ModifyTime"           TIMESTAMPTZ,
    "CreateUserId"         BIGINT,
    "ModifyUserId"         BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_financepayment_FinancePaymentCode" ON financepayment ("FinancePaymentCode");

-- 付款单明细表
CREATE TABLE IF NOT EXISTS financepaymentitem (
    "FinancePaymentItemId"   VARCHAR(36)    NOT NULL PRIMARY KEY,
    "FinancePaymentId"       VARCHAR(36)    NOT NULL,
    "PurchaseOrderId"        VARCHAR(36),
    "PurchaseOrderItemId"    VARCHAR(36),
    "PaymentAmountToBe"      NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "PaymentAmount"          NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "ProductId"              VARCHAR(36),
    "PN"                     VARCHAR(200),
    "Brand"                  VARCHAR(100),
    "VerifyStatus"           SMALLINT       NOT NULL DEFAULT 0,
    "CreateTime"             TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"             TIMESTAMPTZ,
    "CreateUserId"           BIGINT,
    "ModifyUserId"           BIGINT,
    CONSTRAINT "FK_financepaymentitem_financepayment"
        FOREIGN KEY ("FinancePaymentId") REFERENCES financepayment ("FinancePaymentId") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_financepaymentitem_FinancePaymentId" ON financepaymentitem ("FinancePaymentId");

-- 收款单主表
CREATE TABLE IF NOT EXISTS financereceipt (
    "FinanceReceiptId"     VARCHAR(36)      NOT NULL PRIMARY KEY,
    "FinanceReceiptCode"   VARCHAR(16)      NOT NULL,
    "CustomerId"           VARCHAR(36)      NOT NULL,
    "CustomerName"         VARCHAR(200),
    "SalesUserId"          VARCHAR(36),
    "Status"               SMALLINT         NOT NULL DEFAULT 0,
    "ReceiptAmount"        NUMERIC(18,2)    NOT NULL DEFAULT 0,
    "ReceiptCurrency"      SMALLINT         NOT NULL DEFAULT 1,
    "ReceiptDate"          TIMESTAMPTZ,
    "ReceiptUserId"        VARCHAR(36),
    "ReceiptMode"          SMALLINT         NOT NULL DEFAULT 1,
    "ReceiptBankId"        VARCHAR(36),
    "Remark"               VARCHAR(500),
    "CreateTime"           TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
    "ModifyTime"           TIMESTAMPTZ,
    "CreateUserId"         BIGINT,
    "ModifyUserId"         BIGINT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_financereceipt_FinanceReceiptCode" ON financereceipt ("FinanceReceiptCode");

-- 收款单明细表
CREATE TABLE IF NOT EXISTS financereceiptitem (
    "FinanceReceiptItemId"     VARCHAR(36)    NOT NULL PRIMARY KEY,
    "FinanceReceiptId"         VARCHAR(36)    NOT NULL,
    "SellOrderId"              VARCHAR(36),
    "SellOrderItemId"          VARCHAR(36),
    "FinanceSellInvoiceId"     VARCHAR(36),
    "FinanceSellInvoiceItemId" VARCHAR(36),
    "ReceiptAmount"            NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "StockOutItemId"           VARCHAR(36),
    "ProductId"                VARCHAR(36),
    "PN"                       VARCHAR(200),
    "Brand"                    VARCHAR(100),
    "VerifyStatus"             SMALLINT       NOT NULL DEFAULT 0,
    "CreateTime"               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"               TIMESTAMPTZ,
    "CreateUserId"             BIGINT,
    "ModifyUserId"             BIGINT,
    CONSTRAINT "FK_financereceiptitem_financereceipt"
        FOREIGN KEY ("FinanceReceiptId") REFERENCES financereceipt ("FinanceReceiptId") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_financereceiptitem_FinanceReceiptId" ON financereceiptitem ("FinanceReceiptId");

-- 进项发票主表
CREATE TABLE IF NOT EXISTS financepurchaseinvoice (
    "FinancePurchaseInvoiceId"   VARCHAR(36)    NOT NULL PRIMARY KEY,
    "VendorId"                   VARCHAR(36)    NOT NULL,
    "VendorName"                 VARCHAR(200),
    "InvoiceNo"                  VARCHAR(32),
    "InvoiceAmount"              NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "BillAmount"                 NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "TaxAmount"                  NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "ExcludTaxAmount"            NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "InvoiceDate"                TIMESTAMPTZ,
    "ConfirmDate"                TIMESTAMPTZ,
    "ConfirmStatus"              SMALLINT       NOT NULL DEFAULT 0,
    "RedInvoiceStatus"           SMALLINT       NOT NULL DEFAULT 0,
    "Remark"                     VARCHAR(500),
    "CreateTime"                 TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"                 TIMESTAMPTZ,
    "CreateUserId"               BIGINT,
    "ModifyUserId"               BIGINT
);

-- 进项发票明细表
CREATE TABLE IF NOT EXISTS financepurchaseinvoiceitem (
    "FinancePurchaseInvoiceItemId"  VARCHAR(36)    NOT NULL PRIMARY KEY,
    "FinancePurchaseInvoiceId"      VARCHAR(36)    NOT NULL,
    "StockInId"                     VARCHAR(36),
    "StockInCode"                   VARCHAR(32),
    "PurchaseOrderCode"             VARCHAR(32),
    "StockInCost"                   NUMERIC(18,4)  NOT NULL DEFAULT 0,
    "BillCost"                      NUMERIC(18,4)  NOT NULL DEFAULT 0,
    "BillQty"                       BIGINT         NOT NULL DEFAULT 0,
    "BillAmount"                    NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "TaxRate"                       NUMERIC(18,4)  NOT NULL DEFAULT 0,
    "TaxAmount"                     NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "ExcludTaxAmount"               NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "CreateTime"                    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"                    TIMESTAMPTZ,
    "CreateUserId"                  BIGINT,
    "ModifyUserId"                  BIGINT,
    CONSTRAINT "FK_financepurchaseinvoiceitem_financepurchaseinvoice"
        FOREIGN KEY ("FinancePurchaseInvoiceId") REFERENCES financepurchaseinvoice ("FinancePurchaseInvoiceId") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId" ON financepurchaseinvoiceitem ("FinancePurchaseInvoiceId");

-- 销项发票主表
CREATE TABLE IF NOT EXISTS financesellinvoice (
    "FinanceSellInvoiceId"   VARCHAR(36)    NOT NULL PRIMARY KEY,
    "CustomerId"             VARCHAR(36)    NOT NULL,
    "CustomerName"           VARCHAR(200),
    "InvoiceCode"            VARCHAR(32),
    "InvoiceNo"              VARCHAR(64),
    "InvoiceTotal"           NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "MakeInvoiceDate"        TIMESTAMPTZ,
    "ReceiveStatus"          SMALLINT       NOT NULL DEFAULT 0,
    "ReceiveDone"            NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "ReceiveToBe"            NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "Currency"               SMALLINT       NOT NULL DEFAULT 1,
    "Type"                   SMALLINT       NOT NULL DEFAULT 10,
    "InvoiceStatus"          SMALLINT       NOT NULL DEFAULT 1,
    "SellInvoiceType"        SMALLINT       NOT NULL DEFAULT 100,
    "Remark"                 VARCHAR(500),
    "CreateTime"             TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"             TIMESTAMPTZ,
    "CreateUserId"           BIGINT,
    "ModifyUserId"           BIGINT
);

-- 销项发票明细表
CREATE TABLE IF NOT EXISTS sellinvoiceitem (
    "SellInvoiceItemId"      VARCHAR(36)    NOT NULL PRIMARY KEY,
    "FinanceSellInvoiceId"   VARCHAR(36)    NOT NULL,
    "InvoiceTotal"           NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "TaxRate"                NUMERIC(18,4)  NOT NULL DEFAULT 0,
    "ValueAddedTax"          NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "TaxFreeTotal"           NUMERIC(18,2)  NOT NULL DEFAULT 0,
    "Price"                  NUMERIC(18,4)  NOT NULL DEFAULT 0,
    "Qty"                    BIGINT         NOT NULL DEFAULT 0,
    "StockOutItemId"         VARCHAR(36),
    "Currency"               SMALLINT       NOT NULL DEFAULT 1,
    "ReceiveStatus"          SMALLINT       NOT NULL DEFAULT 0,
    "CreateTime"             TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "ModifyTime"             TIMESTAMPTZ,
    "CreateUserId"           BIGINT,
    "ModifyUserId"           BIGINT,
    CONSTRAINT "FK_sellinvoiceitem_financesellinvoice"
        FOREIGN KEY ("FinanceSellInvoiceId") REFERENCES financesellinvoice ("FinanceSellInvoiceId") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_sellinvoiceitem_FinanceSellInvoiceId" ON sellinvoiceitem ("FinanceSellInvoiceId");

-- 在 EF Core 迁移历史表中记录此次迁移（避免 EF Core 重复执行）
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260318035017_AddFinanceTables', '9.0.3')
ON CONFLICT ("MigrationId") DO NOTHING;

-- ============================================================
-- EBS 核心业务表 PostgreSQL 版本
-- 在 localhost FrontCRM 数据库中创建
-- ============================================================

-- 1. 客户主表
CREATE TABLE IF NOT EXISTS "customerinfo" (
    "CustomerId" char(36) NOT NULL,
    "CustomerCode" varchar(16) NOT NULL,
    "OfficialName" varchar(128),
    "StandardOfficialName" varchar(128),
    "NickName" varchar(64),
    "Level" smallint DEFAULT 1,
    "Type" smallint,
    "Scale" smallint,
    "Background" smallint,
    "DealMode" smallint,
    "CompanyNature" smallint,
    "Country" smallint,
    "Industry" varchar(50),
    "Product" varchar(200),
    "Product2" varchar(200),
    "Application" varchar(200),
    "TradeCurrency" smallint,
    "TradeType" smallint,
    "Payment" smallint,
    "ExternalNumber" varchar(50),
    "CreditLine" numeric(18,2) DEFAULT 0.00,
    "CreditLineRemain" numeric(18,2) DEFAULT 0.00,
    "AscriptionType" smallint DEFAULT 1,
    "ProtectStatus" boolean DEFAULT false,
    "ProtectFromUserId" char(36),
    "ProtectTime" timestamp,
    "Status" smallint DEFAULT 0,
    "BlackList" boolean DEFAULT false,
    "DisenableStatus" boolean DEFAULT false,
    "DisenableType" smallint,
    "CommonSeaAuditStatus" smallint,
    "Longitude" numeric(10,6),
    "Latitude" numeric(10,6),
    "CompanyInfo" text,
    "Remark" varchar(500),
    "DUNS" varchar(20),
    "IsControl" boolean DEFAULT false,
    "CreditCode" varchar(50),
    "IdentityType" smallint,
    "SalesUserId" char(36),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("CustomerId"),
    CONSTRAINT "UK_CustomerCode" UNIQUE ("CustomerCode")
);

-- 2. 客户联系人表
CREATE TABLE IF NOT EXISTS "customercontactinfo" (
    "ContactId" char(36) NOT NULL,
    "CustomerId" char(36) NOT NULL,
    "CName" varchar(50),
    "EName" varchar(100),
    "Title" varchar(50),
    "Department" varchar(50),
    "Mobile" varchar(20),
    "Tel" varchar(30),
    "Email" varchar(100),
    "QQ" varchar(20),
    "WeChat" varchar(50),
    "Address" varchar(200),
    "IsMain" boolean DEFAULT false,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("ContactId")
);

-- 3. 客户地址表
CREATE TABLE IF NOT EXISTS "customeraddress" (
    "AddressId" char(36) NOT NULL,
    "CustomerId" char(36) NOT NULL,
    "AddressType" smallint DEFAULT 1,
    "Country" smallint,
    "Province" varchar(50),
    "City" varchar(50),
    "Area" varchar(50),
    "Address" varchar(200),
    "ContactName" varchar(50),
    "ContactPhone" varchar(20),
    "IsDefault" boolean DEFAULT false,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY ("AddressId")
);

-- 4. 供应商主表
CREATE TABLE IF NOT EXISTS "vendorinfo" (
    "VendorId" char(36) NOT NULL,
    "Code" varchar(16) NOT NULL,
    "OfficialName" varchar(64),
    "NickName" varchar(64),
    "VendorIdCrm" varchar(50),
    "Level" smallint,
    "Scale" smallint,
    "Background" smallint,
    "CompanyClass" smallint,
    "Country" smallint,
    "LocationType" smallint,
    "Industry" varchar(50),
    "Product" varchar(200),
    "OfficeAddress" varchar(200),
    "TradeCurrency" smallint,
    "TradeType" smallint,
    "Payment" smallint,
    "ExternalNumber" varchar(50),
    "Credit" smallint,
    "QualityPrejudgement" smallint,
    "Traceability" smallint,
    "AfterSalesService" smallint,
    "DegreeAdaptability" smallint,
    "ISCPFlag" boolean DEFAULT false,
    "Strategy" smallint,
    "SelfSupport" boolean DEFAULT false,
    "BlackList" boolean DEFAULT false,
    "IsDisenable" boolean DEFAULT false,
    "Longitude" numeric(10,6),
    "Latitude" numeric(10,6),
    "CompanyInfo" text,
    "ListingCode" varchar(50),
    "VendorScope" varchar(200),
    "IsControl" boolean DEFAULT false,
    "CreditCode" varchar(50),
    "AscriptionType" smallint DEFAULT 1,
    "PurchaseUserId" char(36),
    "PurchaseGroupId" char(36),
    "Status" smallint DEFAULT 0,
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("VendorId"),
    CONSTRAINT "UK_Code" UNIQUE ("Code")
);

-- 5. 供应商联系人表
CREATE TABLE IF NOT EXISTS "vendorcontactinfo" (
    "ContactId" char(36) NOT NULL,
    "VendorId" char(36) NOT NULL,
    "CName" varchar(50),
    "EName" varchar(100),
    "Title" varchar(50),
    "Department" varchar(50),
    "Mobile" varchar(20),
    "Tel" varchar(30),
    "Email" varchar(100),
    "QQ" varchar(20),
    "WeChat" varchar(50),
    "Address" varchar(200),
    "IsMain" boolean DEFAULT false,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("ContactId")
);

-- 6. 销售订单主表
CREATE TABLE IF NOT EXISTS "sellorder" (
    "SellOrderId" char(36) NOT NULL,
    "SellOrderCode" varchar(32) NOT NULL,
    "CustomerId" char(36) NOT NULL,
    "CustomerContactId" char(36),
    "SalesUserId" char(36),
    "PurchaseGroupId" char(36),
    "Status" smallint DEFAULT 0,
    "Type" smallint,
    "Currency" smallint,
    "Total" numeric(18,2) DEFAULT 0.00,
    "ConvertTotal" numeric(18,2) DEFAULT 0.00,
    "ItemRows" integer DEFAULT 0,
    "PurchaseOrderStatus" smallint DEFAULT 0,
    "StockOutStatus" smallint DEFAULT 0,
    "StockInStatus" smallint DEFAULT 0,
    "FinanceReceiptStatus" smallint DEFAULT 0,
    "FinancePaymentStatus" smallint DEFAULT 0,
    "InvoiceStatus" smallint DEFAULT 0,
    "PurchaseInvoiceProgress" numeric(5,2) DEFAULT 0.00,
    "DeliveryAddress" varchar(200),
    "DeliveryDate" timestamp,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("SellOrderId"),
    CONSTRAINT "UK_SellOrderCode" UNIQUE ("SellOrderCode")
);

-- 7. 采购订单主表
CREATE TABLE IF NOT EXISTS "purchaseorder" (
    "PurchaseOrderId" char(36) NOT NULL,
    "PurchaseOrderCode" varchar(32) NOT NULL,
    "VendorId" char(36) NOT NULL,
    "VendorContactId" char(36),
    "PurchaseUserId" char(36),
    "PurchaseGroupId" char(36),
    "SalesGroupId" char(36),
    "Status" smallint DEFAULT 0,
    "Type" smallint,
    "Currency" smallint,
    "Total" numeric(18,2) DEFAULT 0.00,
    "ConvertTotal" numeric(18,2) DEFAULT 0.00,
    "ItemRows" integer DEFAULT 0,
    "StockStatus" smallint DEFAULT 0,
    "FinanceStatus" smallint DEFAULT 0,
    "StockOutStatus" smallint DEFAULT 0,
    "InvoiceStatus" smallint DEFAULT 0,
    "DeliveryAddress" varchar(200),
    "DeliveryDate" timestamp,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("PurchaseOrderId"),
    CONSTRAINT "UK_PurchaseOrderCode" UNIQUE ("PurchaseOrderCode")
);

-- 8. 物料主表
CREATE TABLE IF NOT EXISTS "material" (
    "MaterialId" char(36) NOT NULL,
    "MaterialCode" varchar(50) NOT NULL,
    "MaterialName" varchar(200) NOT NULL,
    "MaterialModel" varchar(100),
    "BrandId" char(36),
    "CategoryId" char(36),
    "Unit" varchar(20),
    "Status" smallint DEFAULT 1,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    PRIMARY KEY ("MaterialId"),
    CONSTRAINT "UK_MaterialCode" UNIQUE ("MaterialCode")
);

-- 9. 创建索引
CREATE INDEX IF NOT EXISTS "IDX_Customer_SalesUserId" ON "customerinfo" ("SalesUserId");
CREATE INDEX IF NOT EXISTS "IDX_Customer_Level" ON "customerinfo" ("Level");
CREATE INDEX IF NOT EXISTS "IDX_Customer_AscriptionType" ON "customerinfo" ("AscriptionType");
CREATE INDEX IF NOT EXISTS "IDX_Customer_CreateTime" ON "customerinfo" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_CustomerContact_CustomerId" ON "customercontactinfo" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerAddress_CustomerId" ON "customeraddress" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_Vendor_PurchaseUserId" ON "vendorinfo" ("PurchaseUserId");
CREATE INDEX IF NOT EXISTS "IDX_Vendor_PurchaseGroupId" ON "vendorinfo" ("PurchaseGroupId");
CREATE INDEX IF NOT EXISTS "IDX_Vendor_CreateTime" ON "vendorinfo" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_SellOrder_CustomerId" ON "sellorder" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_SellOrder_Status" ON "sellorder" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrder_VendorId" ON "purchaseorder" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrder_Status" ON "purchaseorder" ("Status");

-- 10. 验证表创建
SELECT 'EBS 核心业务表创建完成!' AS message;

SELECT 
    COUNT(*) as total_tables,
    STRING_AGG(table_name, ', ' ORDER BY table_name) as table_list
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN (
        'customerinfo', 'customercontactinfo', 'customeraddress',
        'vendorinfo', 'vendorcontactinfo',
        'sellorder', 'purchaseorder', 'material'
    );
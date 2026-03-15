-- ============================================================
-- EBS Business Database Tables - PostgreSQL Version
-- Converted from MySQL
-- ============================================================

SET search_path TO public;

-- ============================================================
-- EBS 业务数据库表结构 SQL脚本
-- 版本: 1.0
-- 创建日期: 2026-03-14
-- 说明: EBS系统业务相关数据库表，可直接导入使用
-- ============================================================

-- 开启外键检查

-- ============================================================
-- 1. 客户相关表
-- ============================================================

-- 1.1 客户主表
DROP TABLE IF EXISTS "customerinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "customerinfo" (
  "CustomerId" char(36) NOT NULL ,
  "CustomerCode" varchar(16) NOT NULL ,
  "OfficialName" varchar(128) DEFAULT NULL ,
  "StandardOfficialName" varchar(128) DEFAULT NULL ,
  "NickName" varchar(64) DEFAULT NULL ,
  "Level" smallint DEFAULT 1 ,
  "Type" smallint DEFAULT NULL ,
  "Scale" smallint DEFAULT NULL ,
  "Background" smallint DEFAULT NULL ,
  "DealMode" smallint DEFAULT NULL ,
  "CompanyNature" smallint DEFAULT NULL ,
  "Country" smallint DEFAULT NULL ,
  "Industry" varchar(50) DEFAULT NULL ,
  "Product" varchar(200) DEFAULT NULL ,
  "Product2" varchar(200) DEFAULT NULL ,
  "Application" varchar(200) DEFAULT NULL ,
  "TradeCurrency" smallint DEFAULT NULL ,
  "TradeType" smallint DEFAULT NULL ,
  "Payment" smallint DEFAULT NULL ,
  "ExternalNumber" varchar(50) DEFAULT NULL ,
  "CreditLine" numeric(18,2) DEFAULT 0.00 ,
  "CreditLineRemain" numeric(18,2) DEFAULT 0.00 ,
  "AscriptionType" smallint DEFAULT 1 ,
  "ProtectStatus" boolean DEFAULT false ,
  "ProtectFromUserId" char(36) DEFAULT NULL ,
  "ProtectTime" timestamp DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "BlackList" boolean DEFAULT false ,
  "DisenableStatus" boolean DEFAULT false ,
  "DisenableType" smallint DEFAULT NULL ,
  "CommonSeaAuditStatus" smallint DEFAULT NULL ,
  "Longitude" numeric(10,6) DEFAULT NULL ,
  "Latitude" numeric(10,6) DEFAULT NULL ,
  "CompanyInfo" text ,
  "Remark" varchar(500) DEFAULT NULL ,
  "DUNS" varchar(20) DEFAULT NULL ,
  "IsControl" boolean DEFAULT false ,
  "CreditCode" varchar(50) DEFAULT NULL ,
  "IdentityType" smallint DEFAULT NULL ,
  "SalesUserId" char(36) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  "ModifyUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("CustomerId")) ;

-- 1.2 客户联系人表
DROP TABLE IF EXISTS "customercontactinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "customercontactinfo" (
  "ContactId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "CName" varchar(50) DEFAULT NULL ,
  "EName" varchar(100) DEFAULT NULL ,
  "Title" varchar(50) DEFAULT NULL ,
  "Department" varchar(50) DEFAULT NULL ,
  "Mobile" varchar(20) DEFAULT NULL ,
  "Tel" varchar(30) DEFAULT NULL ,
  "Email" varchar(100) DEFAULT NULL ,
  "QQ" varchar(20) DEFAULT NULL ,
  "WeChat" varchar(50) DEFAULT NULL ,
  "Address" varchar(200) DEFAULT NULL ,
  "IsMain" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("ContactId")) ;

-- 1.3 客户地址表
DROP TABLE IF EXISTS "customeraddress" CASCADE;
CREATE TABLE IF NOT EXISTS "customeraddress" (
  "AddressId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "AddressType" smallint DEFAULT 1 ,
  "Country" smallint DEFAULT NULL ,
  "Province" varchar(50) DEFAULT NULL ,
  "City" varchar(50) DEFAULT NULL ,
  "Area" varchar(50) DEFAULT NULL ,
  "Address" varchar(200) DEFAULT NULL ,
  "ContactName" varchar(50) DEFAULT NULL ,
  "ContactPhone" varchar(20) DEFAULT NULL ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("AddressId")) ;

-- 1.4 客户银行账户表
DROP TABLE IF EXISTS "customerbankinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "customerbankinfo" (
  "BankId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "BankName" varchar(100) DEFAULT NULL ,
  "BankAccount" varchar(50) DEFAULT NULL ,
  "AccountName" varchar(50) DEFAULT NULL ,
  "BankBranch" varchar(100) DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("BankId")) ;

-- 1.5 客户发票信息表
DROP TABLE IF EXISTS "customerinvoiceinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "customerinvoiceinfo" (
  "InvoiceId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "InvoiceTitle" varchar(200) DEFAULT NULL ,
  "TaxNo" varchar(50) DEFAULT NULL ,
  "InvoiceAddress" varchar(200) DEFAULT NULL ,
  "InvoicePhone" varchar(20) DEFAULT NULL ,
  "InvoiceBank" varchar(100) DEFAULT NULL ,
  "InvoiceAccount" varchar(50) DEFAULT NULL ,
  "InvoiceType" smallint DEFAULT 1 ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("InvoiceId")) ;

-- 1.6 客户账期信息表
DROP TABLE IF EXISTS "customerpaymentinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "customerpaymentinfo" (
  "PaymentId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "PeriodDays" int DEFAULT NULL ,
  "LimitAmount" numeric(18,2) DEFAULT NULL ,
  "Reason" varchar(500) DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("PaymentId")) ;

-- 1.7 客户风险表
DROP TABLE IF EXISTS "customerrisk" CASCADE;
CREATE TABLE IF NOT EXISTS "customerrisk" (
  "RiskId" char(36) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "RiskType" smallint DEFAULT NULL ,
  "RiskLevel" smallint DEFAULT NULL ,
  "RiskDesc" varchar(500) DEFAULT NULL ,
  "HandleStatus" smallint DEFAULT NULL ,
  "HandleResult" varchar(500) DEFAULT NULL ,
  "HandleUserId" bigint DEFAULT NULL ,
  "HandleTime" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("RiskId")) ;

-- ============================================================
-- 2. 供应商相关表
-- ============================================================

-- 2.1 供应商主表
DROP TABLE IF EXISTS "vendorinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "vendorinfo" (
  "VendorId" char(36) NOT NULL ,
  "Code" varchar(16) NOT NULL ,
  "OfficialName" varchar(64) DEFAULT NULL ,
  "NickName" varchar(64) DEFAULT NULL ,
  "VendorIdCrm" varchar(50) DEFAULT NULL ,
  "Level" smallint DEFAULT NULL ,
  "Scale" smallint DEFAULT NULL ,
  "Background" smallint DEFAULT NULL ,
  "CompanyClass" smallint DEFAULT NULL ,
  "Country" smallint DEFAULT NULL ,
  "LocationType" smallint DEFAULT NULL ,
  "Industry" varchar(50) DEFAULT NULL ,
  "Product" varchar(200) DEFAULT NULL ,
  "OfficeAddress" varchar(200) DEFAULT NULL ,
  "TradeCurrency" smallint DEFAULT NULL ,
  "TradeType" smallint DEFAULT NULL ,
  "Payment" smallint DEFAULT NULL ,
  "ExternalNumber" varchar(50) DEFAULT NULL ,
  "Credit" smallint DEFAULT NULL ,
  "QualityPrejudgement" smallint DEFAULT NULL ,
  "Traceability" smallint DEFAULT NULL ,
  "AfterSalesService" smallint DEFAULT NULL ,
  "DegreeAdaptability" smallint DEFAULT NULL ,
  "ISCPFlag" boolean DEFAULT false ,
  "Strategy" smallint DEFAULT NULL ,
  "SelfSupport" boolean DEFAULT false ,
  "BlackList" boolean DEFAULT false ,
  "IsDisenable" boolean DEFAULT false ,
  "Longitude" numeric(10,6) DEFAULT NULL ,
  "Latitude" numeric(10,6) DEFAULT NULL ,
  "CompanyInfo" text ,
  "ListingCode" varchar(50) DEFAULT NULL ,
  "VendorScope" varchar(200) DEFAULT NULL ,
  "IsControl" boolean DEFAULT false ,
  "CreditCode" varchar(50) DEFAULT NULL ,
  "AscriptionType" smallint DEFAULT 1 ,
  "PurchaseUserId" char(36) DEFAULT NULL ,
  "PurchaseGroupId" char(36) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  "ModifyUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("VendorId")) ;

-- 2.2 供应商联系人表
DROP TABLE IF EXISTS "vendorcontactinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "vendorcontactinfo" (
  "ContactId" char(36) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "CName" varchar(50) DEFAULT NULL ,
  "EName" varchar(100) DEFAULT NULL ,
  "Title" varchar(50) DEFAULT NULL ,
  "Department" varchar(50) DEFAULT NULL ,
  "Mobile" varchar(20) DEFAULT NULL ,
  "Tel" varchar(30) DEFAULT NULL ,
  "Email" varchar(100) DEFAULT NULL ,
  "QQ" varchar(20) DEFAULT NULL ,
  "WeChat" varchar(50) DEFAULT NULL ,
  "Address" varchar(200) DEFAULT NULL ,
  "IsMain" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("ContactId")) ;

-- 2.3 供应商地址表
DROP TABLE IF EXISTS "vendoraddress" CASCADE;
CREATE TABLE IF NOT EXISTS "vendoraddress" (
  "AddressId" char(36) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "AddressType" smallint DEFAULT 1 ,
  "Country" smallint DEFAULT NULL ,
  "Province" varchar(50) DEFAULT NULL ,
  "City" varchar(50) DEFAULT NULL ,
  "Area" varchar(50) DEFAULT NULL ,
  "Address" varchar(200) DEFAULT NULL ,
  "ContactName" varchar(50) DEFAULT NULL ,
  "ContactPhone" varchar(20) DEFAULT NULL ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("AddressId")) ;

-- 2.4 供应商银行账户表
DROP TABLE IF EXISTS "vendorbankinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "vendorbankinfo" (
  "BankId" char(36) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "BankName" varchar(100) DEFAULT NULL ,
  "BankAccount" varchar(50) DEFAULT NULL ,
  "AccountName" varchar(50) DEFAULT NULL ,
  "BankBranch" varchar(100) DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("BankId")) ;

-- ============================================================
-- 3. 销售订单相关表
-- ============================================================

-- 3.1 销售订单主表
DROP TABLE IF EXISTS "sellorder" CASCADE;
CREATE TABLE IF NOT EXISTS "sellorder" (
  "SellOrderId" char(36) NOT NULL ,
  "SellOrderCode" varchar(32) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "CustomerContactId" char(36) DEFAULT NULL ,
  "SalesUserId" char(36) DEFAULT NULL ,
  "PurchaseGroupId" char(36) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Type" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "Total" numeric(18,2) DEFAULT 0.00 ,
  "ConvertTotal" numeric(18,2) DEFAULT 0.00 ,
  "ItemRows" int DEFAULT 0 ,
  "PurchaseOrderStatus" smallint DEFAULT 0 ,
  "StockOutStatus" smallint DEFAULT 0 ,
  "StockInStatus" smallint DEFAULT 0 ,
  "FinanceReceiptStatus" smallint DEFAULT 0 ,
  "FinancePaymentStatus" smallint DEFAULT 0 ,
  "InvoiceStatus" smallint DEFAULT 0 ,
  "PurchaseInvoiceProgress" numeric(5,2) DEFAULT 0.00 ,
  "DeliveryAddress" varchar(200) DEFAULT NULL ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  "ModifyUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("SellOrderId")) ;

-- 3.2 销售订单明细表
DROP TABLE IF EXISTS "sellorderitem" CASCADE;
CREATE TABLE IF NOT EXISTS "sellorderitem" (
  "ItemId" char(36) NOT NULL ,
  "SellOrderId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "PurchaseOrderId" char(36) DEFAULT NULL ,
  "PurchaseOrderItemId" char(36) DEFAULT NULL ,
  "ItemCode" varchar(50) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "Currency" smallint DEFAULT NULL ,
  "ConvertPrice" numeric(18,2) DEFAULT 0.00 ,
  "ConvertAmount" numeric(18,2) DEFAULT 0.00 ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("ItemId")) ;

-- ============================================================
-- 4. 采购订单相关表
-- ============================================================

-- 4.1 采购订单主表
DROP TABLE IF EXISTS "purchaseorder" CASCADE;
CREATE TABLE IF NOT EXISTS "purchaseorder" (
  "PurchaseOrderId" char(36) NOT NULL ,
  "PurchaseOrderCode" varchar(32) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "VendorContactId" char(36) DEFAULT NULL ,
  "PurchaseUserId" char(36) DEFAULT NULL ,
  "PurchaseGroupId" char(36) DEFAULT NULL ,
  "SalesGroupId" char(36) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Type" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "Total" numeric(18,2) DEFAULT 0.00 ,
  "ConvertTotal" numeric(18,2) DEFAULT 0.00 ,
  "ItemRows" int DEFAULT 0 ,
  "StockStatus" smallint DEFAULT 0 ,
  "FinanceStatus" smallint DEFAULT 0 ,
  "StockOutStatus" smallint DEFAULT 0 ,
  "InvoiceStatus" smallint DEFAULT 0 ,
  "DeliveryAddress" varchar(200) DEFAULT NULL ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  "ModifyUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("PurchaseOrderId")) ;

-- 4.2 采购订单明细表
DROP TABLE IF EXISTS "purchaseorderitem" CASCADE;
CREATE TABLE IF NOT EXISTS "purchaseorderitem" (
  "ItemId" char(36) NOT NULL ,
  "PurchaseOrderId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "SellOrderItemId" char(36) DEFAULT NULL ,
  "ItemCode" varchar(50) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "Currency" smallint DEFAULT NULL ,
  "ConvertPrice" numeric(18,2) DEFAULT 0.00 ,
  "ConvertAmount" numeric(18,2) DEFAULT 0.00 ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("ItemId")) ;

-- ============================================================
-- 5. 库存相关表
-- ============================================================

-- 5.1 入库单主表
DROP TABLE IF EXISTS "stockinlist" CASCADE;
CREATE TABLE IF NOT EXISTS "stockinlist" (
  "StockInListId" char(36) NOT NULL ,
  "StockInCode" varchar(50) NOT NULL ,
  "StockInType" smallint DEFAULT NULL ,
  "PurchaseOrderId" char(36) DEFAULT NULL ,
  "StockDefId" char(36) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "TotalQuantity" numeric(18,2) DEFAULT 0.00 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("StockInListId")) ;

-- 5.2 入库单明细表
DROP TABLE IF EXISTS "stockinitem" CASCADE;
CREATE TABLE IF NOT EXISTS "stockinitem" (
  "StockInItemId" char(36) NOT NULL ,
  "StockInListId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "BatchNo" varchar(50) DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("StockInItemId")) ;

-- 5.3 出库单主表
DROP TABLE IF EXISTS "stockoutlist" CASCADE;
CREATE TABLE IF NOT EXISTS "stockoutlist" (
  "StockOutListId" char(36) NOT NULL ,
  "StockOutCode" varchar(50) NOT NULL ,
  "StockOutType" smallint DEFAULT NULL ,
  "SellOrderId" char(36) DEFAULT NULL ,
  "StockDefId" char(36) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "TotalQuantity" numeric(18,2) DEFAULT 0.00 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("StockOutListId")) ;

-- 5.4 出库单明细表
DROP TABLE IF EXISTS "stockoutitem" CASCADE;
CREATE TABLE IF NOT EXISTS "stockoutitem" (
  "StockOutItemId" char(36) NOT NULL ,
  "StockOutListId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "BatchNo" varchar(50) DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("StockOutItemId")) ;

-- 5.5 库存信息表
DROP TABLE IF EXISTS "stockinfo" CASCADE;
CREATE TABLE IF NOT EXISTS "stockinfo" (
  "StockInfoId" char(36) NOT NULL ,
  "MaterialId" char(36) NOT NULL ,
  "StockDefId" char(36) NOT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "AvailableQuantity" numeric(18,2) DEFAULT 0.00 ,
  "FrozenQuantity" numeric(18,2) DEFAULT 0.00 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "ModifyTime" timestamp DEFAULT NULL ,
  PRIMARY KEY ("StockInfoId")) ;

-- 5.6 仓库定义表
DROP TABLE IF EXISTS "stockdef" CASCADE;
CREATE TABLE IF NOT EXISTS "stockdef" (
  "StockDefId" char(36) NOT NULL ,
  "StockCode" varchar(20) NOT NULL ,
  "StockName" varchar(100) NOT NULL ,
  "StockType" smallint DEFAULT NULL ,
  "Address" varchar(200) DEFAULT NULL ,
  "ContactName" varchar(50) DEFAULT NULL ,
  "ContactPhone" varchar(20) DEFAULT NULL ,
  "IsDefault" boolean DEFAULT false ,
  "Remark" varchar(500) DEFAULT NULL ,
  "Status" smallint DEFAULT 1 ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("StockDefId")) ;

-- ============================================================
-- 6. 需求报价相关表
-- ============================================================

-- 6.1 需求主表
DROP TABLE IF EXISTS "rfq" CASCADE;
CREATE TABLE IF NOT EXISTS "rfq" (
  "RfqId" char(36) NOT NULL ,
  "RfqCode" varchar(50) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Type" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "TotalAmount" numeric(18,2) DEFAULT 0.00 ,
  "ItemCount" int DEFAULT 0 ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  PRIMARY KEY ("RfqId")) ;

-- 6.2 需求明细表
DROP TABLE IF EXISTS "rfqitem" CASCADE;
CREATE TABLE IF NOT EXISTS "rfqitem" (
  "ItemId" char(36) NOT NULL ,
  "RfqId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "TargetPrice" numeric(18,2) DEFAULT NULL ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("ItemId")) ;

-- 6.3 报价主表
DROP TABLE IF EXISTS "quote" CASCADE;
CREATE TABLE IF NOT EXISTS "quote" (
  "QuoteId" char(36) NOT NULL ,
  "QuoteCode" varchar(50) NOT NULL ,
  "RfqId" char(36) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Currency" smallint DEFAULT NULL ,
  "TotalAmount" numeric(18,2) DEFAULT 0.00 ,
  "ValidDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("QuoteId")) ;

-- 6.4 报价明细表
DROP TABLE IF EXISTS "quoteitem" CASCADE;
CREATE TABLE IF NOT EXISTS "quoteitem" (
  "ItemId" char(36) NOT NULL ,
  "QuoteId" char(36) NOT NULL ,
  "RfqItemId" char(36) DEFAULT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "DeliveryDate" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("ItemId")) ;

-- ============================================================
-- 7. 财务相关表
-- ============================================================

-- 7.1 付款表
DROP TABLE IF EXISTS "payment" CASCADE;
CREATE TABLE IF NOT EXISTS "payment" (
  "PaymentId" char(36) NOT NULL ,
  "PaymentCode" varchar(50) NOT NULL ,
  "VendorId" char(36) NOT NULL ,
  "PurchaseOrderId" char(36) DEFAULT NULL ,
  "PaymentType" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "PaymentDate" timestamp DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("PaymentId")) ;

-- 7.2 收款表
DROP TABLE IF EXISTS "receipt" CASCADE;
CREATE TABLE IF NOT EXISTS "receipt" (
  "ReceiptId" char(36) NOT NULL ,
  "ReceiptCode" varchar(50) NOT NULL ,
  "CustomerId" char(36) NOT NULL ,
  "SellOrderId" char(36) DEFAULT NULL ,
  "ReceiptType" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "ReceiptDate" timestamp DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("ReceiptId")) ;

-- 7.3 发票主表
DROP TABLE IF EXISTS "invoice" CASCADE;
CREATE TABLE IF NOT EXISTS "invoice" (
  "InvoiceId" char(36) NOT NULL ,
  "InvoiceCode" varchar(50) NOT NULL ,
  "InvoiceType" smallint DEFAULT NULL ,
  "OrderId" char(36) DEFAULT NULL ,
  "OrderType" smallint DEFAULT NULL ,
  "Currency" smallint DEFAULT NULL ,
  "TotalAmount" numeric(18,2) DEFAULT 0.00 ,
  "InvoiceDate" timestamp DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  PRIMARY KEY ("InvoiceId")) ;

-- 7.4 发票明细表
DROP TABLE IF EXISTS "invoiceitem" CASCADE;
CREATE TABLE IF NOT EXISTS "invoiceitem" (
  "InvoiceItemId" char(36) NOT NULL ,
  "InvoiceId" char(36) NOT NULL ,
  "MaterialId" char(36) DEFAULT NULL ,
  "Quantity" numeric(18,2) DEFAULT 0.00 ,
  "Price" numeric(18,2) DEFAULT 0.00 ,
  "Amount" numeric(18,2) DEFAULT 0.00 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("InvoiceItemId")) ;

-- ============================================================
-- 8. 物料品牌相关表
-- ============================================================

-- 8.1 物料主表
DROP TABLE IF EXISTS "material" CASCADE;
CREATE TABLE IF NOT EXISTS "material" (
  "MaterialId" char(36) NOT NULL ,
  "MaterialCode" varchar(50) NOT NULL ,
  "MaterialName" varchar(200) NOT NULL ,
  "MaterialModel" varchar(100) DEFAULT NULL ,
  "BrandId" char(36) DEFAULT NULL ,
  "CategoryId" char(36) DEFAULT NULL ,
  "Unit" varchar(20) DEFAULT NULL ,
  "Status" smallint DEFAULT 1 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  PRIMARY KEY ("MaterialId")) ;

-- 8.2 物料分类表
DROP TABLE IF EXISTS "materialcategory" CASCADE;
CREATE TABLE IF NOT EXISTS "materialcategory" (
  "CategoryId" char(36) NOT NULL ,
  "CategoryCode" varchar(50) NOT NULL ,
  "CategoryName" varchar(100) NOT NULL ,
  "ParentId" char(36) DEFAULT NULL ,
  "Level" int DEFAULT 1 ,
  "Sort" int DEFAULT 0 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("CategoryId")) ;

-- 8.3 品牌表
DROP TABLE IF EXISTS "brand" CASCADE;
CREATE TABLE IF NOT EXISTS "brand" (
  "BrandId" char(36) NOT NULL ,
  "BrandCode" varchar(50) NOT NULL ,
  "BrandName" varchar(100) NOT NULL ,
  "BrandNameEn" varchar(100) DEFAULT NULL ,
  "Logo" varchar(200) DEFAULT NULL ,
  "Status" smallint DEFAULT 1 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("BrandId")) ;

-- ============================================================
-- 9. 系统日志表
-- ============================================================

-- 9.1 业务历史记录表
DROP TABLE IF EXISTS "sys_business_historical_record_item" CASCADE;
CREATE TABLE IF NOT EXISTS "sys_business_historical_record_item" (
  "Id" bigint NOT NULL ,
  "ObjectId" char(36) DEFAULT NULL ,
  "EntityName" varchar(100) DEFAULT NULL ,
  "FieldName" varchar(100) DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "OldValue" text ,
  "NewValue" text ,
  "CreateUserCname" varchar(50) DEFAULT NULL ,
  "CreateUserEname" varchar(50) DEFAULT NULL ,
  "EnterpriseId" bigint DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "CreateUserId" bigint DEFAULT NULL ,
  "IsDelete" boolean DEFAULT false ,
  PRIMARY KEY ("Id")) ;

-- 9.2 单据变更表
DROP TABLE IF EXISTS "sys_document_change" CASCADE;
CREATE TABLE IF NOT EXISTS "sys_document_change" (
  "Id" bigint NOT NULL ,
  "Code" varchar(50) DEFAULT NULL ,
  "Type" smallint DEFAULT NULL ,
  "ObjectId" char(36) DEFAULT NULL ,
  "ObjectName" varchar(50) DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "ChangeLevel" smallint DEFAULT NULL ,
  "OriginalValue" text ,
  "NewValue" text ,
  "ApprovalFlowResult" smallint DEFAULT NULL ,
  "DownstreamData" text ,
  "StringAttachField1" varchar(500) DEFAULT NULL ,
  "ChangeContent" text ,
  "EnterpriseId" bigint DEFAULT NULL ,
  "CreateUserId" bigint DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  "ModifyUserId" bigint DEFAULT NULL ,
  "ModifyTime" timestamp DEFAULT NULL ,
  PRIMARY KEY ("Id")) ;

-- 9.3 单据变更字段配置表
DROP TABLE IF EXISTS "sys_document_change_field_config" CASCADE;
CREATE TABLE IF NOT EXISTS "sys_document_change_field_config" (
  "Id" bigint NOT NULL ,
  "BusinessType" smallint DEFAULT NULL ,
  "FieldName" varchar(100) DEFAULT NULL ,
  "FieldPropName" varchar(100) DEFAULT NULL ,
  "IsJoinChange" boolean DEFAULT true ,
  "IsEffectSales" boolean DEFAULT false ,
  "IsEffectPurchase" boolean DEFAULT false ,
  "IsEffectWarehouse" boolean DEFAULT false ,
  "IsEffectFinance" boolean DEFAULT false ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("Id")) ;

-- 9.4 用户登录日志表
DROP TABLE IF EXISTS "log_login" CASCADE;
CREATE TABLE IF NOT EXISTS "log_login" (
  "Id" bigint NOT NULL ,
  "UserId" bigint DEFAULT NULL ,
  "UserName" varchar(50) DEFAULT NULL ,
  "LoginTime" timestamp DEFAULT NULL ,
  "LogoutTime" timestamp DEFAULT NULL ,
  "LoginIp" varchar(50) DEFAULT NULL ,
  "Browser" varchar(200) DEFAULT NULL ,
  "DeviceType" smallint DEFAULT NULL ,
  "LoginStatus" smallint DEFAULT NULL ,
  "EnterpriseId" bigint DEFAULT NULL ,
  PRIMARY KEY ("Id")) ;

-- 9.5 用户操作日志表
DROP TABLE IF EXISTS "log_operate" CASCADE;
CREATE TABLE IF NOT EXISTS "log_operate" (
  "Id" bigint NOT NULL ,
  "UserId" bigint DEFAULT NULL ,
  "UserName" varchar(50) DEFAULT NULL ,
  "OperateTime" timestamp DEFAULT NULL ,
  "OperateType" varchar(50) DEFAULT NULL ,
  "OperateModule" varchar(100) DEFAULT NULL ,
  "OperateObject" varchar(100) DEFAULT NULL ,
  "OperateContent" text ,
  "OperateIp" varchar(50) DEFAULT NULL ,
  "EnterpriseId" bigint DEFAULT NULL ,
  PRIMARY KEY ("Id")) ;

-- ============================================================
-- 10. 审批流程表
-- ============================================================

-- 10.1 审批流程定义表
DROP TABLE IF EXISTS "approval_flow" CASCADE;
CREATE TABLE IF NOT EXISTS "approval_flow" (
  "FlowId" char(36) NOT NULL ,
  "FlowCode" varchar(50) NOT NULL ,
  "FlowName" varchar(100) NOT NULL ,
  "FlowType" smallint DEFAULT NULL ,
  "BusinessType" smallint DEFAULT NULL ,
  "Status" smallint DEFAULT 1 ,
  "Version" int DEFAULT 1 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("FlowId")) ;

-- 10.2 审批流程实例表
DROP TABLE IF EXISTS "approval_flow_instance" CASCADE;
CREATE TABLE IF NOT EXISTS "approval_flow_instance" (
  "InstanceId" char(36) NOT NULL ,
  "FlowId" char(36) NOT NULL ,
  "BusinessId" char(36) DEFAULT NULL ,
  "BusinessType" smallint DEFAULT NULL ,
  "Status" smallint DEFAULT 0 ,
  "CurrentNodeId" char(36) DEFAULT NULL ,
  "ApplyUserId" bigint DEFAULT NULL ,
  "ApplyTime" timestamp DEFAULT NULL ,
  "FinishTime" timestamp DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("InstanceId")) ;

-- 10.3 审批节点定义表
DROP TABLE IF EXISTS "approval_flow_node" CASCADE;
CREATE TABLE IF NOT EXISTS "approval_flow_node" (
  "NodeId" char(36) NOT NULL ,
  "FlowId" char(36) NOT NULL ,
  "NodeCode" varchar(50) NOT NULL ,
  "NodeName" varchar(100) NOT NULL ,
  "NodeType" smallint DEFAULT NULL ,
  "ApproverType" smallint DEFAULT NULL ,
  "ApproverIds" varchar(500) DEFAULT NULL ,
  "Sort" int DEFAULT 0 ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("NodeId")) ;

-- 10.4 审批记录表
DROP TABLE IF EXISTS "approval_flow_record" CASCADE;
CREATE TABLE IF NOT EXISTS "approval_flow_record" (
  "RecordId" char(36) NOT NULL ,
  "InstanceId" char(36) NOT NULL ,
  "NodeId" char(36) NOT NULL ,
  "ApproverId" bigint DEFAULT NULL ,
  "ApproverName" varchar(50) DEFAULT NULL ,
  "ApproveStatus" smallint DEFAULT NULL ,
  "ApproveTime" timestamp DEFAULT NULL ,
  "ApproveOpinion" varchar(500) DEFAULT NULL ,
  "Remark" varchar(500) DEFAULT NULL ,
  "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP ,
  PRIMARY KEY ("RecordId")) ;

-- ============================================================
-- 11. 添加外键约束
-- ============================================================

-- 客户表外键
ALTER TABLE "customercontactinfo" ADD CONSTRAINT "FK_CustomerContact_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;
ALTER TABLE "customeraddress" ADD CONSTRAINT "FK_CustomerAddress_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;
ALTER TABLE "customerbankinfo" ADD CONSTRAINT "FK_CustomerBank_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;
ALTER TABLE "customerinvoiceinfo" ADD CONSTRAINT "FK_CustomerInvoice_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;
ALTER TABLE "customerpaymentinfo" ADD CONSTRAINT "FK_CustomerPayment_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;
ALTER TABLE "customerrisk" ADD CONSTRAINT "FK_CustomerRisk_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId") ON DELETE CASCADE;

-- 供应商表外键
ALTER TABLE "vendorcontactinfo" ADD CONSTRAINT "FK_VendorContact_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId") ON DELETE CASCADE;
ALTER TABLE "vendoraddress" ADD CONSTRAINT "FK_VendorAddress_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId") ON DELETE CASCADE;
ALTER TABLE "vendorbankinfo" ADD CONSTRAINT "FK_VendorBank_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId") ON DELETE CASCADE;

-- 销售订单表外键
ALTER TABLE "sellorder" ADD CONSTRAINT "FK_SellOrder_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId");
ALTER TABLE "sellorderitem" ADD CONSTRAINT "FK_SellOrderItem_SellOrder" FOREIGN KEY ("SellOrderId") REFERENCES "sellorder"("SellOrderId") ON DELETE CASCADE;
ALTER TABLE "sellorderitem" ADD CONSTRAINT "FK_SellOrderItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

-- 采购订单表外键
ALTER TABLE "purchaseorder" ADD CONSTRAINT "FK_PurchaseOrder_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId");
ALTER TABLE "purchaseorderitem" ADD CONSTRAINT "FK_PurchaseOrderItem_PurchaseOrder" FOREIGN KEY ("PurchaseOrderId") REFERENCES "purchaseorder"("PurchaseOrderId") ON DELETE CASCADE;
ALTER TABLE "purchaseorderitem" ADD CONSTRAINT "FK_PurchaseOrderItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

-- 库存表外键
ALTER TABLE "stockinlist" ADD CONSTRAINT "FK_StockInList_PurchaseOrder" FOREIGN KEY ("PurchaseOrderId") REFERENCES "purchaseorder"("PurchaseOrderId");
ALTER TABLE "stockinlist" ADD CONSTRAINT "FK_StockInList_StockDef" FOREIGN KEY ("StockDefId") REFERENCES "stockdef"("StockDefId");
ALTER TABLE "stockinitem" ADD CONSTRAINT "FK_StockInItem_StockInList" FOREIGN KEY ("StockInListId") REFERENCES "stockinlist"("StockInListId") ON DELETE CASCADE;
ALTER TABLE "stockinitem" ADD CONSTRAINT "FK_StockInItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

ALTER TABLE "stockoutlist" ADD CONSTRAINT "FK_StockOutList_SellOrder" FOREIGN KEY ("SellOrderId") REFERENCES "sellorder"("SellOrderId");
ALTER TABLE "stockoutlist" ADD CONSTRAINT "FK_StockOutList_StockDef" FOREIGN KEY ("StockDefId") REFERENCES "stockdef"("StockDefId");
ALTER TABLE "stockoutitem" ADD CONSTRAINT "FK_StockOutItem_StockOutList" FOREIGN KEY ("StockOutListId") REFERENCES "stockoutlist"("StockOutListId") ON DELETE CASCADE;
ALTER TABLE "stockoutitem" ADD CONSTRAINT "FK_StockOutItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

ALTER TABLE "stockinfo" ADD CONSTRAINT "FK_StockInfo_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");
ALTER TABLE "stockinfo" ADD CONSTRAINT "FK_StockInfo_StockDef" FOREIGN KEY ("StockDefId") REFERENCES "stockdef"("StockDefId");

-- 需求报价表外键
ALTER TABLE "rfq" ADD CONSTRAINT "FK_RFQ_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId");
ALTER TABLE "rfqitem" ADD CONSTRAINT "FK_RFQItem_RFQ" FOREIGN KEY ("RfqId") REFERENCES "rfq"("RfqId") ON DELETE CASCADE;
ALTER TABLE "rfqitem" ADD CONSTRAINT "FK_RFQItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

ALTER TABLE "quote" ADD CONSTRAINT "FK_Quote_RFQ" FOREIGN KEY ("RfqId") REFERENCES "rfq"("RfqId");
ALTER TABLE "quote" ADD CONSTRAINT "FK_Quote_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId");
ALTER TABLE "quoteitem" ADD CONSTRAINT "FK_QuoteItem_Quote" FOREIGN KEY ("QuoteId") REFERENCES "quote"("QuoteId") ON DELETE CASCADE;
ALTER TABLE "quoteitem" ADD CONSTRAINT "FK_QuoteItem_RFQItem" FOREIGN KEY ("RfqItemId") REFERENCES "rfqitem"("ItemId");
ALTER TABLE "quoteitem" ADD CONSTRAINT "FK_QuoteItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

-- 财务表外键
ALTER TABLE "payment" ADD CONSTRAINT "FK_Payment_Vendor" FOREIGN KEY ("VendorId") REFERENCES "vendorinfo"("VendorId");
ALTER TABLE "payment" ADD CONSTRAINT "FK_Payment_PurchaseOrder" FOREIGN KEY ("PurchaseOrderId") REFERENCES "purchaseorder"("PurchaseOrderId");
ALTER TABLE "receipt" ADD CONSTRAINT "FK_Receipt_Customer" FOREIGN KEY ("CustomerId") REFERENCES "customerinfo"("CustomerId");
ALTER TABLE "receipt" ADD CONSTRAINT "FK_Receipt_SellOrder" FOREIGN KEY ("SellOrderId") REFERENCES "sellorder"("SellOrderId");
ALTER TABLE "invoiceitem" ADD CONSTRAINT "FK_InvoiceItem_Invoice" FOREIGN KEY ("InvoiceId") REFERENCES "invoice"("InvoiceId") ON DELETE CASCADE;
ALTER TABLE "invoiceitem" ADD CONSTRAINT "FK_InvoiceItem_Material" FOREIGN KEY ("MaterialId") REFERENCES "material"("MaterialId");

-- 物料表外键
ALTER TABLE "material" ADD CONSTRAINT "FK_Material_Brand" FOREIGN KEY ("BrandId") REFERENCES "brand"("BrandId");
ALTER TABLE "material" ADD CONSTRAINT "FK_Material_Category" FOREIGN KEY ("CategoryId") REFERENCES "materialcategory"("CategoryId");

-- 审批流程表外键
ALTER TABLE "approval_flow_instance" ADD CONSTRAINT "FK_ApprovalInstance_Flow" FOREIGN KEY ("FlowId") REFERENCES "approval_flow"("FlowId");
ALTER TABLE "approval_flow_node" ADD CONSTRAINT "FK_ApprovalNode_Flow" FOREIGN KEY ("FlowId") REFERENCES "approval_flow"("FlowId") ON DELETE CASCADE;
ALTER TABLE "approval_flow_record" ADD CONSTRAINT "FK_ApprovalRecord_Instance" FOREIGN KEY ("InstanceId") REFERENCES "approval_flow_instance"("InstanceId") ON DELETE CASCADE;
ALTER TABLE "approval_flow_record" ADD CONSTRAINT "FK_ApprovalRecord_Node" FOREIGN KEY ("NodeId") REFERENCES "approval_flow_node"("NodeId");

-- ============================================================
-- 脚本执行完成
-- ============================================================

SELECT 'EBS业务数据库表结构创建完成！' AS Result;

-- ============================================================
-- Indexes
-- ============================================================

CREATE UNIQUE INDEX IF NOT EXISTS "UK_CustomerCode" ON "customerinfo" ("CustomerCode");
CREATE INDEX IF NOT EXISTS "IDX_SalesUserId" ON "customerinfo" ("SalesUserId");
CREATE INDEX IF NOT EXISTS "IDX_Level" ON "customerinfo" ("Level");
CREATE INDEX IF NOT EXISTS "IDX_AscriptionType" ON "customerinfo" ("AscriptionType");
CREATE INDEX IF NOT EXISTS "IDX_Industry" ON "customerinfo" ("Industry");
CREATE INDEX IF NOT EXISTS "IDX_CreateTime" ON "customerinfo" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customercontactinfo" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customeraddress" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customerbankinfo" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customerinvoiceinfo" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customerpaymentinfo" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "customerrisk" ("CustomerId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_Code" ON "vendorinfo" ("Code");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseUserId" ON "vendorinfo" ("PurchaseUserId");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseGroupId" ON "vendorinfo" ("PurchaseGroupId");
CREATE INDEX IF NOT EXISTS "IDX_Level" ON "vendorinfo" ("Level");
CREATE INDEX IF NOT EXISTS "IDX_AscriptionType" ON "vendorinfo" ("AscriptionType");
CREATE INDEX IF NOT EXISTS "IDX_CreateTime" ON "vendorinfo" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "vendorcontactinfo" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "vendoraddress" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "vendorbankinfo" ("VendorId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_SellOrderCode" ON "sellorder" ("SellOrderCode");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "sellorder" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_SalesUserId" ON "sellorder" ("SalesUserId");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "sellorder" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_CreateTime" ON "sellorder" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_SellOrderId" ON "sellorderitem" ("SellOrderId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "sellorderitem" ("MaterialId");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrderId" ON "sellorderitem" ("PurchaseOrderId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_PurchaseOrderCode" ON "purchaseorder" ("PurchaseOrderCode");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "purchaseorder" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseUserId" ON "purchaseorder" ("PurchaseUserId");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "purchaseorder" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_CreateTime" ON "purchaseorder" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrderId" ON "purchaseorderitem" ("PurchaseOrderId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "purchaseorderitem" ("MaterialId");
CREATE INDEX IF NOT EXISTS "IDX_SellOrderItemId" ON "purchaseorderitem" ("SellOrderItemId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_StockInCode" ON "stockinlist" ("StockInCode");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrderId" ON "stockinlist" ("PurchaseOrderId");
CREATE INDEX IF NOT EXISTS "IDX_StockDefId" ON "stockinlist" ("StockDefId");
CREATE INDEX IF NOT EXISTS "IDX_StockInListId" ON "stockinitem" ("StockInListId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "stockinitem" ("MaterialId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_StockOutCode" ON "stockoutlist" ("StockOutCode");
CREATE INDEX IF NOT EXISTS "IDX_SellOrderId" ON "stockoutlist" ("SellOrderId");
CREATE INDEX IF NOT EXISTS "IDX_StockDefId" ON "stockoutlist" ("StockDefId");
CREATE INDEX IF NOT EXISTS "IDX_StockOutListId" ON "stockoutitem" ("StockOutListId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "stockoutitem" ("MaterialId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_Material_Stock" ON "stockinfo" ("MaterialId", "StockDefId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "stockinfo" ("MaterialId");
CREATE INDEX IF NOT EXISTS "IDX_StockDefId" ON "stockinfo" ("StockDefId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_StockCode" ON "stockdef" ("StockCode");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_RfqCode" ON "rfq" ("RfqCode");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "rfq" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "rfq" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_RfqId" ON "rfqitem" ("RfqId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "rfqitem" ("MaterialId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_QuoteCode" ON "quote" ("QuoteCode");
CREATE INDEX IF NOT EXISTS "IDX_RfqId" ON "quote" ("RfqId");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "quote" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "quote" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_QuoteId" ON "quoteitem" ("QuoteId");
CREATE INDEX IF NOT EXISTS "IDX_RfqItemId" ON "quoteitem" ("RfqItemId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "quoteitem" ("MaterialId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_PaymentCode" ON "payment" ("PaymentCode");
CREATE INDEX IF NOT EXISTS "IDX_VendorId" ON "payment" ("VendorId");
CREATE INDEX IF NOT EXISTS "IDX_PurchaseOrderId" ON "payment" ("PurchaseOrderId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_ReceiptCode" ON "receipt" ("ReceiptCode");
CREATE INDEX IF NOT EXISTS "IDX_CustomerId" ON "receipt" ("CustomerId");
CREATE INDEX IF NOT EXISTS "IDX_SellOrderId" ON "receipt" ("SellOrderId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_InvoiceCode" ON "invoice" ("InvoiceCode");
CREATE INDEX IF NOT EXISTS "IDX_OrderId" ON "invoice" ("OrderId");
CREATE INDEX IF NOT EXISTS "IDX_OrderType" ON "invoice" ("OrderType");
CREATE INDEX IF NOT EXISTS "IDX_InvoiceId" ON "invoiceitem" ("InvoiceId");
CREATE INDEX IF NOT EXISTS "IDX_MaterialId" ON "invoiceitem" ("MaterialId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_MaterialCode" ON "material" ("MaterialCode");
CREATE INDEX IF NOT EXISTS "IDX_BrandId" ON "material" ("BrandId");
CREATE INDEX IF NOT EXISTS "IDX_CategoryId" ON "material" ("CategoryId");
CREATE INDEX IF NOT EXISTS "IDX_ParentId" ON "materialcategory" ("ParentId");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_BrandCode" ON "brand" ("BrandCode");
CREATE INDEX IF NOT EXISTS "IDX_ObjectId" ON "sys_business_historical_record_item" ("ObjectId");
CREATE INDEX IF NOT EXISTS "IDX_EntityName" ON "sys_business_historical_record_item" ("EntityName");
CREATE INDEX IF NOT EXISTS "IDX_CreateTime" ON "sys_business_historical_record_item" ("CreateTime");
CREATE INDEX IF NOT EXISTS "IDX_ObjectId" ON "sys_document_change" ("ObjectId");
CREATE INDEX IF NOT EXISTS "IDX_Type" ON "sys_document_change" ("Type");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "sys_document_change" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_BusinessType" ON "sys_document_change_field_config" ("BusinessType");
CREATE INDEX IF NOT EXISTS "IDX_UserId" ON "log_login" ("UserId");
CREATE INDEX IF NOT EXISTS "IDX_LoginTime" ON "log_login" ("LoginTime");
CREATE INDEX IF NOT EXISTS "IDX_UserId" ON "log_operate" ("UserId");
CREATE INDEX IF NOT EXISTS "IDX_OperateTime" ON "log_operate" ("OperateTime");
CREATE INDEX IF NOT EXISTS "IDX_OperateModule" ON "log_operate" ("OperateModule");
CREATE UNIQUE INDEX IF NOT EXISTS "UK_FlowCode" ON "approval_flow" ("FlowCode");
CREATE INDEX IF NOT EXISTS "IDX_FlowId" ON "approval_flow_instance" ("FlowId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessId" ON "approval_flow_instance" ("BusinessId");
CREATE INDEX IF NOT EXISTS "IDX_Status" ON "approval_flow_instance" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_FlowId" ON "approval_flow_node" ("FlowId");
CREATE INDEX IF NOT EXISTS "IDX_InstanceId" ON "approval_flow_record" ("InstanceId");
CREATE INDEX IF NOT EXISTS "IDX_NodeId" ON "approval_flow_record" ("NodeId");
CREATE INDEX IF NOT EXISTS "IDX_ApproverId" ON "approval_flow_record" ("ApproverId");

-- ============================================================
-- Verification
-- ============================================================

SELECT 
    'EBS Tables Created Successfully' AS status,
    COUNT(*) AS total_tables
FROM information_schema.tables 
WHERE table_schema = 'public';

SELECT 
    table_name AS table_name,
    CASE 
        WHEN table_name LIKE 'customer%' THEN 'Customer Module'
        WHEN table_name LIKE 'vendor%' THEN 'Vendor Module'
        WHEN table_name LIKE 'sell%' THEN 'Sales Module'
        WHEN table_name LIKE 'purchase%' THEN 'Purchase Module'
        WHEN table_name LIKE 'material%' THEN 'Material Module'
        WHEN table_name LIKE 'stock%' THEN 'Stock Module'
        WHEN table_name LIKE 'finance%' THEN 'Finance Module'
        ELSE 'Other'
    END AS module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;

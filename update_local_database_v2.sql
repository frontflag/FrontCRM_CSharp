-- =============================================
-- 本地数据库更新脚本 V2
-- 统一使用 PascalCase 字段名（与 EF Core 一致）
-- =============================================

-- 开始事务
BEGIN;

-- =============================================
-- 1. 创建迁移历史表
-- =============================================
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL PRIMARY KEY,
    "ProductVersion" character varying(32) NOT NULL
);

-- =============================================
-- 2. 供应商相关表
-- =============================================

-- vendor 表
CREATE TABLE IF NOT EXISTS vendor (
    "Id" character varying(36) NOT NULL PRIMARY KEY,
    "Code" character varying(16) NOT NULL,
    "OfficialName" character varying(64),
    "NickName" character varying(64),
    "Level" smallint NOT NULL,
    "Type" smallint NOT NULL,
    "Status" smallint NOT NULL DEFAULT 1,
    "CreditLine" numeric(18,2) NOT NULL,
    "Payment" smallint NOT NULL,
    "TradeCurrency" character varying(10),
    "TaxRate" numeric(5,2) NOT NULL,
    "UniformNumber" character varying(20),
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_vendor_Code" ON vendor ("Code");

-- vendor_address 表
CREATE TABLE IF NOT EXISTS vendor_address (
    "Id" character varying(36) NOT NULL PRIMARY KEY,
    "VendorId" character varying(36) NOT NULL,
    "AddressType" smallint NOT NULL,
    "Country" character varying(50),
    "Province" character varying(50),
    "City" character varying(50),
    "Area" character varying(50),
    "Address" character varying(200),
    "ContactName" character varying(50),
    "ContactPhone" character varying(20),
    "IsDefault" boolean NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_vendor_address_VendorId" ON vendor_address ("VendorId");

-- vendorcontactinfo 表
CREATE TABLE IF NOT EXISTS vendorcontactinfo (
    "Id" character varying(36) NOT NULL PRIMARY KEY,
    "VendorId" character varying(36) NOT NULL,
    "CName" character varying(50),
    "EName" character varying(100),
    "Title" character varying(50),
    "Department" character varying(50),
    "Mobile" character varying(20),
    "Tel" character varying(30),
    "Email" character varying(100),
    "QQ" character varying(20),
    "WeChat" character varying(50),
    "IsMain" boolean NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_vendorcontactinfo_VendorId" ON vendorcontactinfo ("VendorId");

-- vendorbankinfo 表
CREATE TABLE IF NOT EXISTS vendorbankinfo (
    "Id" character varying(36) NOT NULL PRIMARY KEY,
    "VendorId" character varying(36) NOT NULL,
    "BankName" character varying(100),
    "BankAccount" character varying(50),
    "AccountName" character varying(50),
    "BankBranch" character varying(100),
    "Currency" character varying(10),
    "IsDefault" boolean NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_vendorbankinfo_VendorId" ON vendorbankinfo ("VendorId");

-- =============================================
-- 3. 库存相关表
-- =============================================

-- stock 表
CREATE TABLE IF NOT EXISTS stock (
    "StockId" character varying(36) NOT NULL PRIMARY KEY,
    "MaterialId" character varying(36) NOT NULL,
    "WarehouseId" character varying(36) NOT NULL,
    "LocationId" character varying(36),
    "Quantity" numeric(18,4) NOT NULL,
    "AvailableQuantity" numeric(18,4) NOT NULL,
    "LockedQuantity" numeric(18,4) NOT NULL,
    "Unit" character varying(20),
    "BatchNo" character varying(50),
    "ProductionDate" timestamp with time zone,
    "ExpiryDate" timestamp with time zone,
    "Status" smallint NOT NULL DEFAULT 1,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_stock_MaterialId" ON stock ("MaterialId");
CREATE INDEX IF NOT EXISTS "IX_stock_WarehouseId" ON stock ("WarehouseId");

-- stockin 表
CREATE TABLE IF NOT EXISTS stockin (
    "StockInId" character varying(36) NOT NULL PRIMARY KEY,
    "StockInCode" character varying(32) NOT NULL,
    "StockInType" smallint NOT NULL,
    "SourceCode" character varying(32),
    "WarehouseId" character varying(36) NOT NULL,
    "VendorId" character varying(36),
    "StockInDate" timestamp with time zone NOT NULL,
    "TotalQuantity" numeric(18,4) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Status" smallint NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_stockin_StockInCode" ON stockin ("StockInCode");

-- stockinitem 表
CREATE TABLE IF NOT EXISTS stockinitem (
    "ItemId" character varying(36) NOT NULL PRIMARY KEY,
    "StockInId" character varying(36) NOT NULL,
    "MaterialId" character varying(36) NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "Price" numeric(18,4) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "LocationId" character varying(36),
    "BatchNo" character varying(50),
    "ProductionDate" timestamp with time zone,
    "ExpiryDate" timestamp with time zone,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_stockinitem_StockInId" ON stockinitem ("StockInId");

-- stockout 表
CREATE TABLE IF NOT EXISTS stockout (
    "StockOutId" character varying(36) NOT NULL PRIMARY KEY,
    "StockOutCode" character varying(32) NOT NULL,
    "StockOutType" smallint NOT NULL,
    "SourceCode" character varying(32),
    "WarehouseId" character varying(36) NOT NULL,
    "CustomerId" character varying(36),
    "StockOutDate" timestamp with time zone NOT NULL,
    "TotalQuantity" numeric(18,4) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "Status" smallint NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_stockout_StockOutCode" ON stockout ("StockOutCode");

-- stockoutitem 表
CREATE TABLE IF NOT EXISTS stockoutitem (
    "ItemId" character varying(36) NOT NULL PRIMARY KEY,
    "StockOutId" character varying(36) NOT NULL,
    "MaterialId" character varying(36) NOT NULL,
    "Quantity" numeric(18,4) NOT NULL,
    "Price" numeric(18,4) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "LocationId" character varying(36),
    "BatchNo" character varying(50),
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

CREATE INDEX IF NOT EXISTS "IX_stockoutitem_StockOutId" ON stockoutitem ("StockOutId");

-- stockoutrequest 表
CREATE TABLE IF NOT EXISTS stockoutrequest (
    "Id" character varying(36) NOT NULL PRIMARY KEY,
    "RequestCode" character varying(50) NOT NULL,
    "SalesOrderId" character varying(36),
    "CustomerId" character varying(36),
    "RequestUserId" character varying(36),
    "RequestDate" timestamp with time zone NOT NULL,
    "TotalQuantity" numeric(18,4) NOT NULL,
    "Status" smallint NOT NULL,
    "Remark" character varying(500),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" character varying(36),
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" character varying(36)
);

-- 提交事务
COMMIT;

-- 验证
SELECT '数据库更新完成！' AS message;
SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('vendor', 'stock', 'stockin', 'stockout') ORDER BY table_name;

-- ============================================================
-- EBS 业务数据库表结构 PostgreSQL 版本
-- 从 MySQL 转换而来
-- ============================================================

-- 1. 客户相关表

-- 1.1 客户主表
DROP TABLE IF EXISTS "customerinfo";
CREATE TABLE "customerinfo" (
  "CustomerId" char(36) NOT NULL,
  "CustomerCode" varchar(16) NOT NULL,
  "OfficialName" varchar(128) DEFAULT NULL,
  "StandardOfficialName" varchar(128) DEFAULT NULL,
  "NickName" varchar(64) DEFAULT NULL,
  "Level" smallint DEFAULT 1,
  "Type" smallint DEFAULT NULL,
  "Scale" smallint DEFAULT NULL,
  "Background" smallint DEFAULT NULL,
  "DealMode" smallint DEFAULT NULL,
  "CompanyNature" smallint DEFAULT NULL,
  "Country" smallint DEFAULT NULL,
  "Industry" varchar(50) DEFAULT NULL,
  "Product" varchar(200) DEFAULT NULL,
  "Product2" varchar(200) DEFAULT NULL,
  "Application" varchar(200) DEFAULT NULL,
  "TradeCurrency" smallint DEFAULT NULL,
  "TradeType" smallint DEFAULT NULL,
  "Payment" smallint DEFAULT NULL,
  "ExternalNumber" varchar(50) DEFAULT NULL,
  "CreditLine" decimal(18,2) DEFAULT 0.00,
  "CreditLineRemain" decimal(18,2) DEFAULT 0.00,
  "AscriptionType" smallint DEFAULT 1,
  "ProtectStatus" boolean DEFAULT false,
  "ProtectFromUserId" char(36) DEFAULT NULL,
  "ProtectTime" timestamp DEFAULT NULL,
  "Status" smallint DEFAULT 0,
  "BlackList" boolean DEFAULT false,
  "DisenableStatus" boolean DEFAULT false,
  "DisenableType" smallint DEFAULT NULL,
  "CommonSeaAuditStatus" smallint DEFAULT NULL,
  "Longitude" decimal(10,6) DEFAULT NULL,
  "Latitude" decimal(10,6) DEFAULT NULL,
  "CompanyInfo" text DEFAULT NULL,
  "Remark" varchar(500) DEFAULT NULL,
  "DUNS" varchar(20) DEFAULT NULL,
  "IsControl" boolean DEFAULT false,
  "CreditCode" varchar(50) DEFAULT NULL,
  "IdentityType" smallint DEFAULT NULL,
  "SalesUserId" char(36) DEFAULT NULL,
  "CreateTime" timestamp DEFAULT NOW(),
  "CreateUserId" bigint DEFAULT NULL,
  "ModifyTime" timestamp DEFAULT NULL,
  "ModifyUserId" bigint DEFAULT NULL,
  PRIMARY KEY ("CustomerId"),
  UNIQUE ("CustomerCode")
);

COMMENT ON TABLE "customerinfo" IS '客户主表';
COMMENT ON COLUMN "customerinfo"."CustomerId" IS '客户ID (主键)';
COMMENT ON COLUMN "customerinfo"."CustomerCode" IS '客户编码';
COMMENT ON COLUMN "customerinfo"."OfficialName" IS '公司全称';
COMMENT ON COLUMN "customerinfo"."StandardOfficialName" IS '公司标准全称';
COMMENT ON COLUMN "customerinfo"."NickName" IS '公司简称';
COMMENT ON COLUMN "customerinfo"."Level" IS '客户等级 (1:D 2:C 3:B 4:BPO 5:VIP 6:VPO)';
COMMENT ON COLUMN "customerinfo"."Type" IS '客户类型 (1:OEM 2:ODM 3:终端 4:IDH 5:贸易商 6:代理商)';
COMMENT ON COLUMN "customerinfo"."Scale" IS '规模';
COMMENT ON COLUMN "customerinfo"."Background" IS '背景';
COMMENT ON COLUMN "customerinfo"."DealMode" IS '交易模式';

CREATE INDEX "IDX_SalesUserId" ON "customerinfo" ("SalesUserId");
CREATE INDEX "IDX_Level" ON "customerinfo" ("Level");
CREATE INDEX "IDX_AscriptionType" ON "customerinfo" ("AscriptionType");
CREATE INDEX "IDX_Industry" ON "customerinfo" ("Industry");
CREATE INDEX "IDX_CreateTime" ON "customerinfo" ("CreateTime");

-- 1.2 客户联系人表
DROP TABLE IF EXISTS "customercontactinfo";
CREATE TABLE "customercontactinfo" (
  "ContactId" char(36) NOT NULL,
  "CustomerId" char(36) NOT NULL,
  "CName" varchar(50) DEFAULT NULL,
  "EName" varchar(100) DEFAULT NULL,
  "Title" varchar(50) DEFAULT NULL,
  "Department" varchar(50) DEFAULT NULL,
  "Mobile" varchar(20) DEFAULT NULL,
  "Tel" varchar(30) DEFAULT NULL,
  "Email" varchar(100) DEFAULT NULL,
  "QQ" varchar(20) DEFAULT NULL,
  "WeChat" varchar(50) DEFAULT NULL,
  "Address" varchar(200) DEFAULT NULL,
  "IsMain" boolean DEFAULT false,
  "Remark" varchar(500) DEFAULT NULL,
  "CreateTime" timestamp DEFAULT NOW(),
  "CreateUserId" bigint DEFAULT NULL,
  PRIMARY KEY ("ContactId")
);

COMMENT ON TABLE "customercontactinfo" IS '客户联系人表';
COMMENT ON COLUMN "customercontactinfo"."ContactId" IS '联系人ID (主键)';
COMMENT ON COLUMN "customercontactinfo"."CustomerId" IS '客户ID (外键)';
COMMENT ON COLUMN "customercontactinfo"."CName" IS '中文名';
COMMENT ON COLUMN "customercontactinfo"."EName" IS '英文名';
COMMENT ON COLUMN "customercontactinfo"."Title" IS '职位';
COMMENT ON COLUMN "customercontactinfo"."Department" IS '部门';

CREATE INDEX "IDX_CustomerId" ON "customercontactinfo" ("CustomerId");

-- 1.3 客户地址表
DROP TABLE IF EXISTS "customeraddress";
CREATE TABLE "customeraddress" (
  "AddressId" char(36) NOT NULL,
  "CustomerId" char(36) NOT NULL,
  "AddressType" smallint DEFAULT 1,
  "Country" smallint DEFAULT NULL,
  "Province" varchar(50) DEFAULT NULL,
  "City" varchar(50) DEFAULT NULL,
  "District" varchar(50) DEFAULT NULL,
  "AddressLine" varchar(500) DEFAULT NULL,
  "PostalCode" varchar(20) DEFAULT NULL,
  "ContactPerson" varchar(50) DEFAULT NULL,
  "ContactPhone" varchar(20) DEFAULT NULL,
  "IsDefault" boolean DEFAULT false,
  "Remark" varchar(500) DEFAULT NULL,
  "CreateTime" timestamp DEFAULT NOW(),
  "CreateUserId" bigint DEFAULT NULL,
  PRIMARY KEY ("AddressId")
);

COMMENT ON TABLE "customeraddress" IS '客户地址表';
COMMENT ON COLUMN "customeraddress"."AddressId" IS '地址ID (主键)';
COMMENT ON COLUMN "customeraddress"."CustomerId" IS '客户ID (外键)';
COMMENT ON COLUMN "customeraddress"."AddressType" IS '地址类型 (1:收货地址 2:账单地址)';
COMMENT ON COLUMN "customeraddress"."Country" IS '国家';

CREATE INDEX "IDX_CustomerId_Addr" ON "customeraddress" ("CustomerId");

-- 继续添加其他主要表...
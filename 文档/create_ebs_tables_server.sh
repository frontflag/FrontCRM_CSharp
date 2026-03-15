#!/bin/bash
# 直接在服务器上创建 EBS 表结构的脚本

echo "正在创建 EBS 业务表..."

# 连接到数据库并执行 SQL
docker compose exec postgres psql -U postgres -d FrontCRM << EOF

-- 1. 客户主表
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

-- 2. 客户联系人表
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

-- 3. 客户地址表
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

EOF

echo "表创建完成!"
echo "验证表结构:"
docker compose exec postgres psql -U postgres -d FrontCRM -c "\dt"
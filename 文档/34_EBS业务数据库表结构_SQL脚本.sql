-- ============================================================
-- EBS 业务数据库表结构 SQL脚本
-- 版本: 1.0
-- 创建日期: 2026-03-14
-- 说明: EBS系统业务相关数据库表，可直接导入使用
-- ============================================================

-- 开启外键检查
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- 1. 客户相关表
-- ============================================================

-- 1.1 客户主表
DROP TABLE IF EXISTS `customerinfo`;
CREATE TABLE `customerinfo` (
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (主键)',
  `CustomerCode` varchar(16) NOT NULL COMMENT '客户编码',
  `OfficialName` varchar(128) DEFAULT NULL COMMENT '公司全称',
  `StandardOfficialName` varchar(128) DEFAULT NULL COMMENT '公司标准全称',
  `NickName` varchar(64) DEFAULT NULL COMMENT '公司简称',
  `Level` smallint DEFAULT 1 COMMENT '客户等级 (1:D 2:C 3:B 4:BPO 5:VIP 6:VPO)',
  `Type` smallint DEFAULT NULL COMMENT '客户类型 (1:OEM 2:ODM 3:终端 4:IDH 5:贸易商 6:代理商)',
  `Scale` smallint DEFAULT NULL COMMENT '规模',
  `Background` smallint DEFAULT NULL COMMENT '背景',
  `DealMode` smallint DEFAULT NULL COMMENT '交易模式',
  `CompanyNature` smallint DEFAULT NULL COMMENT '公司性质',
  `Country` smallint DEFAULT NULL COMMENT '国家类型',
  `Industry` varchar(50) DEFAULT NULL COMMENT '行业',
  `Product` varchar(200) DEFAULT NULL COMMENT '主营产品',
  `Product2` varchar(200) DEFAULT NULL COMMENT '主营产品2',
  `Application` varchar(200) DEFAULT NULL COMMENT '应用领域',
  `TradeCurrency` tinyint DEFAULT NULL COMMENT '交易币别',
  `TradeType` smallint DEFAULT NULL COMMENT '交易方式',
  `Payment` smallint DEFAULT NULL COMMENT '账期',
  `ExternalNumber` varchar(50) DEFAULT NULL COMMENT '外部系统编号',
  `CreditLine` decimal(18,2) DEFAULT 0.00 COMMENT '授信额度',
  `CreditLineRemain` decimal(18,2) DEFAULT 0.00 COMMENT '授信额度剩余',
  `AscriptionType` smallint DEFAULT 1 COMMENT '客户类型 (1:专属 2:公海)',
  `ProtectStatus` bit(1) DEFAULT b'0' COMMENT '保护状态',
  `ProtectFromUserId` char(36) DEFAULT NULL COMMENT '保护人ID',
  `ProtectTime` datetime DEFAULT NULL COMMENT '保护时间',
  `Status` tinyint DEFAULT 0 COMMENT '审核状态',
  `BlackList` bit(1) DEFAULT b'0' COMMENT '黑名单',
  `DisenableStatus` bit(1) DEFAULT b'0' COMMENT '禁用状态',
  `DisenableType` smallint DEFAULT NULL COMMENT '禁用类型',
  `CommonSeaAuditStatus` smallint DEFAULT NULL COMMENT '公海审核状态',
  `Longitude` decimal(10,6) DEFAULT NULL COMMENT '经度',
  `Latitude` decimal(10,6) DEFAULT NULL COMMENT '纬度',
  `CompanyInfo` text COMMENT '公司简介',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `DUNS` varchar(20) DEFAULT NULL COMMENT '邓白氏码',
  `IsControl` bit(1) DEFAULT b'0' COMMENT '是否管控',
  `CreditCode` varchar(50) DEFAULT NULL COMMENT '统一社会信用代码',
  `IdentityType` smallint DEFAULT NULL COMMENT '客户身份类型',
  `SalesUserId` char(36) DEFAULT NULL COMMENT '业务员ID',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  `ModifyUserId` bigint DEFAULT NULL COMMENT '修改人ID',
  PRIMARY KEY (`CustomerId`),
  UNIQUE KEY `UK_CustomerCode` (`CustomerCode`),
  KEY `IDX_SalesUserId` (`SalesUserId`),
  KEY `IDX_Level` (`Level`),
  KEY `IDX_AscriptionType` (`AscriptionType`),
  KEY `IDX_Industry` (`Industry`),
  KEY `IDX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户主表';

-- 1.2 客户联系人表
DROP TABLE IF EXISTS `customercontactinfo`;
CREATE TABLE `customercontactinfo` (
  `ContactId` char(36) NOT NULL COMMENT '联系人ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `CName` varchar(50) DEFAULT NULL COMMENT '中文名',
  `EName` varchar(100) DEFAULT NULL COMMENT '英文名',
  `Title` varchar(50) DEFAULT NULL COMMENT '职位',
  `Department` varchar(50) DEFAULT NULL COMMENT '部门',
  `Mobile` varchar(20) DEFAULT NULL COMMENT '手机',
  `Tel` varchar(30) DEFAULT NULL COMMENT '电话',
  `Email` varchar(100) DEFAULT NULL COMMENT '邮箱',
  `QQ` varchar(20) DEFAULT NULL COMMENT 'QQ',
  `WeChat` varchar(50) DEFAULT NULL COMMENT '微信',
  `Address` varchar(200) DEFAULT NULL COMMENT '地址',
  `IsMain` bit(1) DEFAULT b'0' COMMENT '是否主联系人',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`ContactId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户联系人表';

-- 1.3 客户地址表
DROP TABLE IF EXISTS `customeraddress`;
CREATE TABLE `customeraddress` (
  `AddressId` char(36) NOT NULL COMMENT '地址ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `AddressType` smallint DEFAULT 1 COMMENT '地址类型 (1:收货地址 2:账单地址)',
  `Country` smallint DEFAULT NULL COMMENT '国家',
  `Province` varchar(50) DEFAULT NULL COMMENT '省份',
  `City` varchar(50) DEFAULT NULL COMMENT '城市',
  `Area` varchar(50) DEFAULT NULL COMMENT '区域',
  `Address` varchar(200) DEFAULT NULL COMMENT '详细地址',
  `ContactName` varchar(50) DEFAULT NULL COMMENT '联系人',
  `ContactPhone` varchar(20) DEFAULT NULL COMMENT '联系电话',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认地址',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`AddressId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户地址表';

-- 1.4 客户银行账户表
DROP TABLE IF EXISTS `customerbankinfo`;
CREATE TABLE `customerbankinfo` (
  `BankId` char(36) NOT NULL COMMENT '银行账户ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `BankName` varchar(100) DEFAULT NULL COMMENT '银行名称',
  `BankAccount` varchar(50) DEFAULT NULL COMMENT '银行账号',
  `AccountName` varchar(50) DEFAULT NULL COMMENT '账户名称',
  `BankBranch` varchar(100) DEFAULT NULL COMMENT '银行支行',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认账户',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`BankId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户银行账户表';

-- 1.5 客户发票信息表
DROP TABLE IF EXISTS `customerinvoiceinfo`;
CREATE TABLE `customerinvoiceinfo` (
  `InvoiceId` char(36) NOT NULL COMMENT '发票ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `InvoiceTitle` varchar(200) DEFAULT NULL COMMENT '发票抬头',
  `TaxNo` varchar(50) DEFAULT NULL COMMENT '税号',
  `InvoiceAddress` varchar(200) DEFAULT NULL COMMENT '发票地址',
  `InvoicePhone` varchar(20) DEFAULT NULL COMMENT '发票电话',
  `InvoiceBank` varchar(100) DEFAULT NULL COMMENT '开户银行',
  `InvoiceAccount` varchar(50) DEFAULT NULL COMMENT '银行账号',
  `InvoiceType` smallint DEFAULT 1 COMMENT '发票类型 (1:增值税普通发票 2:增值税专用发票)',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认发票',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`InvoiceId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户发票信息表';

-- 1.6 客户账期信息表
DROP TABLE IF EXISTS `customerpaymentinfo`;
CREATE TABLE `customerpaymentinfo` (
  `PaymentId` char(36) NOT NULL COMMENT '账期ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `PeriodDays` int DEFAULT NULL COMMENT '账期天数',
  `LimitAmount` decimal(18,2) DEFAULT NULL COMMENT '限额金额',
  `Reason` varchar(500) DEFAULT NULL COMMENT '原因',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`PaymentId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户账期信息表';

-- 1.7 客户风险表
DROP TABLE IF EXISTS `customerrisk`;
CREATE TABLE `customerrisk` (
  `RiskId` char(36) NOT NULL COMMENT '风险ID (主键)',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `RiskType` smallint DEFAULT NULL COMMENT '风险类型',
  `RiskLevel` smallint DEFAULT NULL COMMENT '风险等级',
  `RiskDesc` varchar(500) DEFAULT NULL COMMENT '风险描述',
  `HandleStatus` smallint DEFAULT NULL COMMENT '处理状态',
  `HandleResult` varchar(500) DEFAULT NULL COMMENT '处理结果',
  `HandleUserId` bigint DEFAULT NULL COMMENT '处理人ID',
  `HandleTime` datetime DEFAULT NULL COMMENT '处理时间',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`RiskId`),
  KEY `IDX_CustomerId` (`CustomerId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='客户风险表';


-- ============================================================
-- 2. 供应商相关表
-- ============================================================

-- 2.1 供应商主表
DROP TABLE IF EXISTS `vendorinfo`;
CREATE TABLE `vendorinfo` (
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (主键)',
  `Code` varchar(16) NOT NULL COMMENT '供应商编码',
  `OfficialName` varchar(64) DEFAULT NULL COMMENT '供应商全称',
  `NickName` varchar(64) DEFAULT NULL COMMENT '供应商简称',
  `VendorIdCrm` varchar(50) DEFAULT NULL COMMENT 'CRM供应商ID',
  `Level` smallint DEFAULT NULL COMMENT '供应商等级',
  `Scale` smallint DEFAULT NULL COMMENT '规模',
  `Background` smallint DEFAULT NULL COMMENT '背景',
  `CompanyClass` smallint DEFAULT NULL COMMENT '公司类别',
  `Country` smallint DEFAULT NULL COMMENT '国家',
  `LocationType` smallint DEFAULT NULL COMMENT '地域类型',
  `Industry` varchar(50) DEFAULT NULL COMMENT '行业',
  `Product` varchar(200) DEFAULT NULL COMMENT '主营产品',
  `OfficeAddress` varchar(200) DEFAULT NULL COMMENT '办公地址',
  `TradeCurrency` tinyint DEFAULT NULL COMMENT '交易币别',
  `TradeType` smallint DEFAULT NULL COMMENT '交易方式',
  `Payment` smallint DEFAULT NULL COMMENT '账期',
  `ExternalNumber` varchar(50) DEFAULT NULL COMMENT '外部系统编号',
  `Credit` smallint DEFAULT NULL COMMENT '信用评级',
  `QualityPrejudgement` smallint DEFAULT NULL COMMENT '质量预判',
  `Traceability` smallint DEFAULT NULL COMMENT '可追溯性',
  `AfterSalesService` smallint DEFAULT NULL COMMENT '售后服务',
  `DegreeAdaptability` smallint DEFAULT NULL COMMENT '适应程度',
  `ISCPFlag` bit(1) DEFAULT b'0' COMMENT 'ISCP标志',
  `Strategy` smallint DEFAULT NULL COMMENT '战略属性',
  `SelfSupport` bit(1) DEFAULT b'0' COMMENT '自营',
  `BlackList` bit(1) DEFAULT b'0' COMMENT '黑名单',
  `IsDisenable` bit(1) DEFAULT b'0' COMMENT '是否禁用',
  `Longitude` decimal(10,6) DEFAULT NULL COMMENT '经度',
  `Latitude` decimal(10,6) DEFAULT NULL COMMENT '纬度',
  `CompanyInfo` text COMMENT '公司简介',
  `ListingCode` varchar(50) DEFAULT NULL COMMENT '上市代码',
  `VendorScope` varchar(200) DEFAULT NULL COMMENT '经营范围',
  `IsControl` bit(1) DEFAULT b'0' COMMENT '是否管控',
  `CreditCode` varchar(50) DEFAULT NULL COMMENT '统一社会信用代码',
  `AscriptionType` smallint DEFAULT 1 COMMENT '供应商归属 (1:专属 2:公海)',
  `PurchaseUserId` char(36) DEFAULT NULL COMMENT '采购员ID',
  `PurchaseGroupId` char(36) DEFAULT NULL COMMENT '采购员组ID',
  `Status` tinyint DEFAULT 0 COMMENT '审核状态',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  `ModifyUserId` bigint DEFAULT NULL COMMENT '修改人ID',
  PRIMARY KEY (`VendorId`),
  UNIQUE KEY `UK_Code` (`Code`),
  KEY `IDX_PurchaseUserId` (`PurchaseUserId`),
  KEY `IDX_PurchaseGroupId` (`PurchaseGroupId`),
  KEY `IDX_Level` (`Level`),
  KEY `IDX_AscriptionType` (`AscriptionType`),
  KEY `IDX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='供应商主表';

-- 2.2 供应商联系人表
DROP TABLE IF EXISTS `vendorcontactinfo`;
CREATE TABLE `vendorcontactinfo` (
  `ContactId` char(36) NOT NULL COMMENT '联系人ID (主键)',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (外键)',
  `CName` varchar(50) DEFAULT NULL COMMENT '中文名',
  `EName` varchar(100) DEFAULT NULL COMMENT '英文名',
  `Title` varchar(50) DEFAULT NULL COMMENT '职位',
  `Department` varchar(50) DEFAULT NULL COMMENT '部门',
  `Mobile` varchar(20) DEFAULT NULL COMMENT '手机',
  `Tel` varchar(30) DEFAULT NULL COMMENT '电话',
  `Email` varchar(100) DEFAULT NULL COMMENT '邮箱',
  `QQ` varchar(20) DEFAULT NULL COMMENT 'QQ',
  `WeChat` varchar(50) DEFAULT NULL COMMENT '微信',
  `Address` varchar(200) DEFAULT NULL COMMENT '地址',
  `IsMain` bit(1) DEFAULT b'0' COMMENT '是否主联系人',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`ContactId`),
  KEY `IDX_VendorId` (`VendorId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='供应商联系人表';

-- 2.3 供应商地址表
DROP TABLE IF EXISTS `vendoraddress`;
CREATE TABLE `vendoraddress` (
  `AddressId` char(36) NOT NULL COMMENT '地址ID (主键)',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (外键)',
  `AddressType` smallint DEFAULT 1 COMMENT '地址类型 (1:收货地址 2:账单地址)',
  `Country` smallint DEFAULT NULL COMMENT '国家',
  `Province` varchar(50) DEFAULT NULL COMMENT '省份',
  `City` varchar(50) DEFAULT NULL COMMENT '城市',
  `Area` varchar(50) DEFAULT NULL COMMENT '区域',
  `Address` varchar(200) DEFAULT NULL COMMENT '详细地址',
  `ContactName` varchar(50) DEFAULT NULL COMMENT '联系人',
  `ContactPhone` varchar(20) DEFAULT NULL COMMENT '联系电话',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认地址',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`AddressId`),
  KEY `IDX_VendorId` (`VendorId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='供应商地址表';

-- 2.4 供应商银行账户表
DROP TABLE IF EXISTS `vendorbankinfo`;
CREATE TABLE `vendorbankinfo` (
  `BankId` char(36) NOT NULL COMMENT '银行账户ID (主键)',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (外键)',
  `BankName` varchar(100) DEFAULT NULL COMMENT '银行名称',
  `BankAccount` varchar(50) DEFAULT NULL COMMENT '银行账号',
  `AccountName` varchar(50) DEFAULT NULL COMMENT '账户名称',
  `BankBranch` varchar(100) DEFAULT NULL COMMENT '银行支行',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认账户',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`BankId`),
  KEY `IDX_VendorId` (`VendorId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='供应商银行账户表';


-- ============================================================
-- 3. 销售订单相关表
-- ============================================================

-- 3.1 销售订单主表
DROP TABLE IF EXISTS `sellorder`;
CREATE TABLE `sellorder` (
  `SellOrderId` char(36) NOT NULL COMMENT '销售订单ID (主键)',
  `SellOrderCode` varchar(32) NOT NULL COMMENT '销售单号',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID',
  `CustomerContactId` char(36) DEFAULT NULL COMMENT '客户联系人ID',
  `SalesUserId` char(36) DEFAULT NULL COMMENT '业务员ID',
  `PurchaseGroupId` char(36) DEFAULT NULL COMMENT '采购员组ID',
  `Status` tinyint DEFAULT 0 COMMENT '订单状态',
  `Type` smallint DEFAULT NULL COMMENT '订单类型',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `Total` decimal(18,2) DEFAULT 0.00 COMMENT '总金额原币别',
  `ConvertTotal` decimal(18,2) DEFAULT 0.00 COMMENT '折算总额',
  `ItemRows` int DEFAULT 0 COMMENT '明细条数',
  `PurchaseOrderStatus` smallint DEFAULT 0 COMMENT '采购状态',
  `StockOutStatus` smallint DEFAULT 0 COMMENT '出库状态',
  `StockInStatus` smallint DEFAULT 0 COMMENT '入库状态',
  `FinanceReceiptStatus` smallint DEFAULT 0 COMMENT '财务收款状态',
  `FinancePaymentStatus` smallint DEFAULT 0 COMMENT '财务付款状态',
  `InvoiceStatus` smallint DEFAULT 0 COMMENT '开票状态',
  `PurchaseInvoiceProgress` decimal(5,2) DEFAULT 0.00 COMMENT '进项发票开票进度',
  `DeliveryAddress` varchar(200) DEFAULT NULL COMMENT '收货地址',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '交货日期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  `ModifyUserId` bigint DEFAULT NULL COMMENT '修改人ID',
  PRIMARY KEY (`SellOrderId`),
  UNIQUE KEY `UK_SellOrderCode` (`SellOrderCode`),
  KEY `IDX_CustomerId` (`CustomerId`),
  KEY `IDX_SalesUserId` (`SalesUserId`),
  KEY `IDX_Status` (`Status`),
  KEY `IDX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='销售订单主表';

-- 3.2 销售订单明细表
DROP TABLE IF EXISTS `sellorderitem`;
CREATE TABLE `sellorderitem` (
  `ItemId` char(36) NOT NULL COMMENT '明细ID (主键)',
  `SellOrderId` char(36) NOT NULL COMMENT '销售订单ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `PurchaseOrderId` char(36) DEFAULT NULL COMMENT '采购订单ID (外键)',
  `PurchaseOrderItemId` char(36) DEFAULT NULL COMMENT '采购订单明细ID (外键)',
  `ItemCode` varchar(50) DEFAULT NULL COMMENT '明细编码',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '金额',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `ConvertPrice` decimal(18,2) DEFAULT 0.00 COMMENT '折算单价',
  `ConvertAmount` decimal(18,2) DEFAULT 0.00 COMMENT '折算金额',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '交期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`ItemId`),
  KEY `IDX_SellOrderId` (`SellOrderId`),
  KEY `IDX_MaterialId` (`MaterialId`),
  KEY `IDX_PurchaseOrderId` (`PurchaseOrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='销售订单明细表';


-- ============================================================
-- 4. 采购订单相关表
-- ============================================================

-- 4.1 采购订单主表
DROP TABLE IF EXISTS `purchaseorder`;
CREATE TABLE `purchaseorder` (
  `PurchaseOrderId` char(36) NOT NULL COMMENT '采购订单ID (主键)',
  `PurchaseOrderCode` varchar(32) NOT NULL COMMENT '采购单号',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID',
  `VendorContactId` char(36) DEFAULT NULL COMMENT '供应商联系人ID',
  `PurchaseUserId` char(36) DEFAULT NULL COMMENT '采购员ID',
  `PurchaseGroupId` char(36) DEFAULT NULL COMMENT '采购组ID',
  `SalesGroupId` char(36) DEFAULT NULL COMMENT '业务员组ID',
  `Status` tinyint DEFAULT 0 COMMENT '订单状态',
  `Type` smallint DEFAULT NULL COMMENT '订单类型',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `Total` decimal(18,2) DEFAULT 0.00 COMMENT '采购总额',
  `ConvertTotal` decimal(18,2) DEFAULT 0.00 COMMENT '折算总额',
  `ItemRows` int DEFAULT 0 COMMENT '明细条目',
  `StockStatus` smallint DEFAULT 0 COMMENT '入库状态',
  `FinanceStatus` smallint DEFAULT 0 COMMENT '付款状态',
  `StockOutStatus` smallint DEFAULT 0 COMMENT '出库状态',
  `InvoiceStatus` smallint DEFAULT 0 COMMENT '开票状态',
  `DeliveryAddress` varchar(200) DEFAULT NULL COMMENT '收货地址',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '交货日期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  `ModifyUserId` bigint DEFAULT NULL COMMENT '修改人ID',
  PRIMARY KEY (`PurchaseOrderId`),
  UNIQUE KEY `UK_PurchaseOrderCode` (`PurchaseOrderCode`),
  KEY `IDX_VendorId` (`VendorId`),
  KEY `IDX_PurchaseUserId` (`PurchaseUserId`),
  KEY `IDX_Status` (`Status`),
  KEY `IDX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='采购订单主表';

-- 4.2 采购订单明细表
DROP TABLE IF EXISTS `purchaseorderitem`;
CREATE TABLE `purchaseorderitem` (
  `ItemId` char(36) NOT NULL COMMENT '明细ID (主键)',
  `PurchaseOrderId` char(36) NOT NULL COMMENT '采购订单ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `SellOrderItemId` char(36) DEFAULT NULL COMMENT '销售订单明细ID (外键)',
  `ItemCode` varchar(50) DEFAULT NULL COMMENT '明细编码',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '金额',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `ConvertPrice` decimal(18,2) DEFAULT 0.00 COMMENT '折算单价',
  `ConvertAmount` decimal(18,2) DEFAULT 0.00 COMMENT '折算金额',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '交期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`ItemId`),
  KEY `IDX_PurchaseOrderId` (`PurchaseOrderId`),
  KEY `IDX_MaterialId` (`MaterialId`),
  KEY `IDX_SellOrderItemId` (`SellOrderItemId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='采购订单明细表';


-- ============================================================
-- 5. 库存相关表
-- ============================================================

-- 5.1 入库单主表
DROP TABLE IF EXISTS `stockinlist`;
CREATE TABLE `stockinlist` (
  `StockInListId` char(36) NOT NULL COMMENT '入库单ID (主键)',
  `StockInCode` varchar(50) NOT NULL COMMENT '入库单号',
  `StockInType` smallint DEFAULT NULL COMMENT '入库类型',
  `PurchaseOrderId` char(36) DEFAULT NULL COMMENT '采购订单ID',
  `StockDefId` char(36) DEFAULT NULL COMMENT '仓库ID',
  `Status` smallint DEFAULT 0 COMMENT '状态',
  `TotalQuantity` decimal(18,2) DEFAULT 0.00 COMMENT '总数量',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`StockInListId`),
  UNIQUE KEY `UK_StockInCode` (`StockInCode`),
  KEY `IDX_PurchaseOrderId` (`PurchaseOrderId`),
  KEY `IDX_StockDefId` (`StockDefId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='入库单主表';

-- 5.2 入库单明细表
DROP TABLE IF EXISTS `stockinitem`;
CREATE TABLE `stockinitem` (
  `StockInItemId` char(36) NOT NULL COMMENT '入库明细ID (主键)',
  `StockInListId` char(36) NOT NULL COMMENT '入库单ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '金额',
  `BatchNo` varchar(50) DEFAULT NULL COMMENT '批次号',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`StockInItemId`),
  KEY `IDX_StockInListId` (`StockInListId`),
  KEY `IDX_MaterialId` (`MaterialId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='入库单明细表';

-- 5.3 出库单主表
DROP TABLE IF EXISTS `stockoutlist`;
CREATE TABLE `stockoutlist` (
  `StockOutListId` char(36) NOT NULL COMMENT '出库单ID (主键)',
  `StockOutCode` varchar(50) NOT NULL COMMENT '出库单号',
  `StockOutType` smallint DEFAULT NULL COMMENT '出库类型',
  `SellOrderId` char(36) DEFAULT NULL COMMENT '销售订单ID',
  `StockDefId` char(36) DEFAULT NULL COMMENT '仓库ID',
  `Status` smallint DEFAULT 0 COMMENT '状态',
  `TotalQuantity` decimal(18,2) DEFAULT 0.00 COMMENT '总数量',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`StockOutListId`),
  UNIQUE KEY `UK_StockOutCode` (`StockOutCode`),
  KEY `IDX_SellOrderId` (`SellOrderId`),
  KEY `IDX_StockDefId` (`StockDefId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='出库单主表';

-- 5.4 出库单明细表
DROP TABLE IF EXISTS `stockoutitem`;
CREATE TABLE `stockoutitem` (
  `StockOutItemId` char(36) NOT NULL COMMENT '出库明细ID (主键)',
  `StockOutListId` char(36) NOT NULL COMMENT '出库单ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '金额',
  `BatchNo` varchar(50) DEFAULT NULL COMMENT '批次号',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`StockOutItemId`),
  KEY `IDX_StockOutListId` (`StockOutListId`),
  KEY `IDX_MaterialId` (`MaterialId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='出库单明细表';

-- 5.5 库存信息表
DROP TABLE IF EXISTS `stockinfo`;
CREATE TABLE `stockinfo` (
  `StockInfoId` char(36) NOT NULL COMMENT '库存ID (主键)',
  `MaterialId` char(36) NOT NULL COMMENT '物料ID (外键)',
  `StockDefId` char(36) NOT NULL COMMENT '仓库ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '库存数量',
  `AvailableQuantity` decimal(18,2) DEFAULT 0.00 COMMENT '可用数量',
  `FrozenQuantity` decimal(18,2) DEFAULT 0.00 COMMENT '冻结数量',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  PRIMARY KEY (`StockInfoId`),
  UNIQUE KEY `UK_Material_Stock` (`MaterialId`, `StockDefId`),
  KEY `IDX_MaterialId` (`MaterialId`),
  KEY `IDX_StockDefId` (`StockDefId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='库存信息表';

-- 5.6 仓库定义表
DROP TABLE IF EXISTS `stockdef`;
CREATE TABLE `stockdef` (
  `StockDefId` char(36) NOT NULL COMMENT '仓库ID (主键)',
  `StockCode` varchar(20) NOT NULL COMMENT '仓库编码',
  `StockName` varchar(100) NOT NULL COMMENT '仓库名称',
  `StockType` smallint DEFAULT NULL COMMENT '仓库类型',
  `Address` varchar(200) DEFAULT NULL COMMENT '地址',
  `ContactName` varchar(50) DEFAULT NULL COMMENT '联系人',
  `ContactPhone` varchar(20) DEFAULT NULL COMMENT '联系电话',
  `IsDefault` bit(1) DEFAULT b'0' COMMENT '是否默认仓库',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `Status` smallint DEFAULT 1 COMMENT '状态',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`StockDefId`),
  UNIQUE KEY `UK_StockCode` (`StockCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='仓库定义表';


-- ============================================================
-- 6. 需求报价相关表
-- ============================================================

-- 6.1 需求主表
DROP TABLE IF EXISTS `rfq`;
CREATE TABLE `rfq` (
  `RfqId` char(36) NOT NULL COMMENT '需求ID (主键)',
  `RfqCode` varchar(50) NOT NULL COMMENT '需求单号',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID',
  `Status` smallint DEFAULT 0 COMMENT '需求状态',
  `Type` smallint DEFAULT NULL COMMENT '需求类型',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `TotalAmount` decimal(18,2) DEFAULT 0.00 COMMENT '总金额',
  `ItemCount` int DEFAULT 0 COMMENT '明细数量',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '期望交期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  PRIMARY KEY (`RfqId`),
  UNIQUE KEY `UK_RfqCode` (`RfqCode`),
  KEY `IDX_CustomerId` (`CustomerId`),
  KEY `IDX_Status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='需求主表';

-- 6.2 需求明细表
DROP TABLE IF EXISTS `rfqitem`;
CREATE TABLE `rfqitem` (
  `ItemId` char(36) NOT NULL COMMENT '明细ID (主键)',
  `RfqId` char(36) NOT NULL COMMENT '需求ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '需求数量',
  `TargetPrice` decimal(18,2) DEFAULT NULL COMMENT '目标价格',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '期望交期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`ItemId`),
  KEY `IDX_RfqId` (`RfqId`),
  KEY `IDX_MaterialId` (`MaterialId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='需求明细表';

-- 6.3 报价主表
DROP TABLE IF EXISTS `quote`;
CREATE TABLE `quote` (
  `QuoteId` char(36) NOT NULL COMMENT '报价ID (主键)',
  `QuoteCode` varchar(50) NOT NULL COMMENT '报价单号',
  `RfqId` char(36) NOT NULL COMMENT '需求ID (外键)',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (外键)',
  `Status` smallint DEFAULT 0 COMMENT '报价状态',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `TotalAmount` decimal(18,2) DEFAULT 0.00 COMMENT '总金额',
  `ValidDate` datetime DEFAULT NULL COMMENT '有效期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`QuoteId`),
  UNIQUE KEY `UK_QuoteCode` (`QuoteCode`),
  KEY `IDX_RfqId` (`RfqId`),
  KEY `IDX_VendorId` (`VendorId`),
  KEY `IDX_Status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报价主表';

-- 6.4 报价明细表
DROP TABLE IF EXISTS `quoteitem`;
CREATE TABLE `quoteitem` (
  `ItemId` char(36) NOT NULL COMMENT '明细ID (主键)',
  `QuoteId` char(36) NOT NULL COMMENT '报价ID (外键)',
  `RfqItemId` char(36) DEFAULT NULL COMMENT '需求明细ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '报价数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '报价单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '报价金额',
  `DeliveryDate` datetime DEFAULT NULL COMMENT '交期',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`ItemId`),
  KEY `IDX_QuoteId` (`QuoteId`),
  KEY `IDX_RfqItemId` (`RfqItemId`),
  KEY `IDX_MaterialId` (`MaterialId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='报价明细表';


-- ============================================================
-- 7. 财务相关表
-- ============================================================

-- 7.1 付款表
DROP TABLE IF EXISTS `payment`;
CREATE TABLE `payment` (
  `PaymentId` char(36) NOT NULL COMMENT '付款ID (主键)',
  `PaymentCode` varchar(50) NOT NULL COMMENT '付款单号',
  `VendorId` char(36) NOT NULL COMMENT '供应商ID (外键)',
  `PurchaseOrderId` char(36) DEFAULT NULL COMMENT '采购订单ID (外键)',
  `PaymentType` smallint DEFAULT NULL COMMENT '付款类型',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '付款金额',
  `PaymentDate` datetime DEFAULT NULL COMMENT '付款日期',
  `Status` smallint DEFAULT 0 COMMENT '付款状态',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`PaymentId`),
  UNIQUE KEY `UK_PaymentCode` (`PaymentCode`),
  KEY `IDX_VendorId` (`VendorId`),
  KEY `IDX_PurchaseOrderId` (`PurchaseOrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='付款表';

-- 7.2 收款表
DROP TABLE IF EXISTS `receipt`;
CREATE TABLE `receipt` (
  `ReceiptId` char(36) NOT NULL COMMENT '收款ID (主键)',
  `ReceiptCode` varchar(50) NOT NULL COMMENT '收款单号',
  `CustomerId` char(36) NOT NULL COMMENT '客户ID (外键)',
  `SellOrderId` char(36) DEFAULT NULL COMMENT '销售订单ID (外键)',
  `ReceiptType` smallint DEFAULT NULL COMMENT '收款类型',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '收款金额',
  `ReceiptDate` datetime DEFAULT NULL COMMENT '收款日期',
  `Status` smallint DEFAULT 0 COMMENT '收款状态',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`ReceiptId`),
  UNIQUE KEY `UK_ReceiptCode` (`ReceiptCode`),
  KEY `IDX_CustomerId` (`CustomerId`),
  KEY `IDX_SellOrderId` (`SellOrderId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='收款表';

-- 7.3 发票主表
DROP TABLE IF EXISTS `invoice`;
CREATE TABLE `invoice` (
  `InvoiceId` char(36) NOT NULL COMMENT '发票ID (主键)',
  `InvoiceCode` varchar(50) NOT NULL COMMENT '发票号码',
  `InvoiceType` smallint DEFAULT NULL COMMENT '发票类型 (1:进项 2:销项)',
  `OrderId` char(36) DEFAULT NULL COMMENT '订单ID (外键)',
  `OrderType` smallint DEFAULT NULL COMMENT '订单类型 (1:销售订单 2:采购订单)',
  `Currency` tinyint DEFAULT NULL COMMENT '币别',
  `TotalAmount` decimal(18,2) DEFAULT 0.00 COMMENT '发票金额',
  `InvoiceDate` datetime DEFAULT NULL COMMENT '开票日期',
  `Status` smallint DEFAULT 0 COMMENT '发票状态',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  PRIMARY KEY (`InvoiceId`),
  UNIQUE KEY `UK_InvoiceCode` (`InvoiceCode`),
  KEY `IDX_OrderId` (`OrderId`),
  KEY `IDX_OrderType` (`OrderType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='发票主表';

-- 7.4 发票明细表
DROP TABLE IF EXISTS `invoiceitem`;
CREATE TABLE `invoiceitem` (
  `InvoiceItemId` char(36) NOT NULL COMMENT '发票明细ID (主键)',
  `InvoiceId` char(36) NOT NULL COMMENT '发票ID (外键)',
  `MaterialId` char(36) DEFAULT NULL COMMENT '物料ID (外键)',
  `Quantity` decimal(18,2) DEFAULT 0.00 COMMENT '数量',
  `Price` decimal(18,2) DEFAULT 0.00 COMMENT '单价',
  `Amount` decimal(18,2) DEFAULT 0.00 COMMENT '金额',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`InvoiceItemId`),
  KEY `IDX_InvoiceId` (`InvoiceId`),
  KEY `IDX_MaterialId` (`MaterialId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='发票明细表';


-- ============================================================
-- 8. 物料品牌相关表
-- ============================================================

-- 8.1 物料主表
DROP TABLE IF EXISTS `material`;
CREATE TABLE `material` (
  `MaterialId` char(36) NOT NULL COMMENT '物料ID (主键)',
  `MaterialCode` varchar(50) NOT NULL COMMENT '物料编码',
  `MaterialName` varchar(200) NOT NULL COMMENT '物料名称',
  `MaterialModel` varchar(100) DEFAULT NULL COMMENT '型号',
  `BrandId` char(36) DEFAULT NULL COMMENT '品牌ID (外键)',
  `CategoryId` char(36) DEFAULT NULL COMMENT '分类ID (外键)',
  `Unit` varchar(20) DEFAULT NULL COMMENT '单位',
  `Status` smallint DEFAULT 1 COMMENT '状态',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '修改时间',
  PRIMARY KEY (`MaterialId`),
  UNIQUE KEY `UK_MaterialCode` (`MaterialCode`),
  KEY `IDX_BrandId` (`BrandId`),
  KEY `IDX_CategoryId` (`CategoryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物料主表';

-- 8.2 物料分类表
DROP TABLE IF EXISTS `materialcategory`;
CREATE TABLE `materialcategory` (
  `CategoryId` char(36) NOT NULL COMMENT '分类ID (主键)',
  `CategoryCode` varchar(50) NOT NULL COMMENT '分类编码',
  `CategoryName` varchar(100) NOT NULL COMMENT '分类名称',
  `ParentId` char(36) DEFAULT NULL COMMENT '父分类ID',
  `Level` int DEFAULT 1 COMMENT '层级',
  `Sort` int DEFAULT 0 COMMENT '排序',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`CategoryId`),
  KEY `IDX_ParentId` (`ParentId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物料分类表';

-- 8.3 品牌表
DROP TABLE IF EXISTS `brand`;
CREATE TABLE `brand` (
  `BrandId` char(36) NOT NULL COMMENT '品牌ID (主键)',
  `BrandCode` varchar(50) NOT NULL COMMENT '品牌编码',
  `BrandName` varchar(100) NOT NULL COMMENT '品牌名称',
  `BrandNameEn` varchar(100) DEFAULT NULL COMMENT '品牌英文名',
  `Logo` varchar(200) DEFAULT NULL COMMENT '品牌Logo',
  `Status` smallint DEFAULT 1 COMMENT '状态',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`BrandId`),
  UNIQUE KEY `UK_BrandCode` (`BrandCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='品牌表';


-- ============================================================
-- 9. 系统日志表
-- ============================================================

-- 9.1 业务历史记录表
DROP TABLE IF EXISTS `sys_business_historical_record_item`;
CREATE TABLE `sys_business_historical_record_item` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `ObjectId` char(36) DEFAULT NULL COMMENT '对象ID (业务对象ID)',
  `EntityName` varchar(100) DEFAULT NULL COMMENT '实体名称',
  `FieldName` varchar(100) DEFAULT NULL COMMENT '字段名',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `OldValue` text COMMENT '旧值 (字段级别的旧值)',
  `NewValue` text COMMENT '新值 (字段级别的新值)',
  `CreateUserCname` varchar(50) DEFAULT NULL COMMENT '创建人中文名',
  `CreateUserEname` varchar(50) DEFAULT NULL COMMENT '创建人英文名',
  `EnterpriseId` bigint DEFAULT NULL COMMENT '企业ID',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `IsDelete` bit(1) DEFAULT b'0' COMMENT '是否删除',
  PRIMARY KEY (`Id`),
  KEY `IDX_ObjectId` (`ObjectId`),
  KEY `IDX_EntityName` (`EntityName`),
  KEY `IDX_CreateTime` (`CreateTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='业务历史记录表';

-- 9.2 单据变更表
DROP TABLE IF EXISTS `sys_document_change`;
CREATE TABLE `sys_document_change` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '变更记录ID (主键)',
  `Code` varchar(50) DEFAULT NULL COMMENT '变更单号',
  `Type` smallint DEFAULT NULL COMMENT '变更类型 (1:销售订单 2:采购订单 3:客户 4:供应商)',
  `ObjectId` char(36) DEFAULT NULL COMMENT '业务单据ID',
  `ObjectName` varchar(50) DEFAULT NULL COMMENT '变更对象 (单据编号)',
  `Status` smallint DEFAULT 0 COMMENT '变更状态',
  `ChangeLevel` smallint DEFAULT NULL COMMENT '变更等级 (普通/重要/紧急)',
  `OriginalValue` text COMMENT '变更前的值 (JSON格式,完整对象)',
  `NewValue` text COMMENT '变更后的值 (JSON格式,完整对象)',
  `ApprovalFlowResult` smallint DEFAULT NULL COMMENT '流程审批结果',
  `DownstreamData` text COMMENT '下游数据字符串数组',
  `StringAttachField1` varchar(500) DEFAULT NULL COMMENT '附加字段1 (变更类型列表)',
  `ChangeContent` text COMMENT '变更内容摘要',
  `EnterpriseId` bigint DEFAULT NULL COMMENT '企业ID',
  `CreateUserId` bigint DEFAULT NULL COMMENT '创建人ID',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `ModifyUserId` bigint DEFAULT NULL COMMENT '修改人ID',
  `ModifyTime` datetime DEFAULT NULL COMMENT '最后修改时间',
  PRIMARY KEY (`Id`),
  KEY `IDX_ObjectId` (`ObjectId`),
  KEY `IDX_Type` (`Type`),
  KEY `IDX_Status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='单据变更表';

-- 9.3 单据变更字段配置表
DROP TABLE IF EXISTS `sys_document_change_field_config`;
CREATE TABLE `sys_document_change_field_config` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '配置ID (主键)',
  `BusinessType` smallint DEFAULT NULL COMMENT '业务类型 (1:销售订单 2:采购订单 3:客户 4:供应商)',
  `FieldName` varchar(100) DEFAULT NULL COMMENT '字段名 (页面显示的中文名)',
  `FieldPropName` varchar(100) DEFAULT NULL COMMENT '字段属性名 (代码中的属性名)',
  `IsJoinChange` bit(1) DEFAULT b'1' COMMENT '是否参与变更 (是否记录)',
  `IsEffectSales` bit(1) DEFAULT b'0' COMMENT '是否影响销售',
  `IsEffectPurchase` bit(1) DEFAULT b'0' COMMENT '是否影响采购',
  `IsEffectWarehouse` bit(1) DEFAULT b'0' COMMENT '是否影响仓库',
  `IsEffectFinance` bit(1) DEFAULT b'0' COMMENT '是否影响财务',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`Id`),
  KEY `IDX_BusinessType` (`BusinessType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='单据变更字段配置表';

-- 9.4 用户登录日志表
DROP TABLE IF EXISTS `log_login`;
CREATE TABLE `log_login` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `UserId` bigint DEFAULT NULL COMMENT '用户ID',
  `UserName` varchar(50) DEFAULT NULL COMMENT '用户名',
  `LoginTime` datetime DEFAULT NULL COMMENT '登录时间',
  `LogoutTime` datetime DEFAULT NULL COMMENT '退出时间',
  `LoginIp` varchar(50) DEFAULT NULL COMMENT '登录IP',
  `Browser` varchar(200) DEFAULT NULL COMMENT '浏览器信息',
  `DeviceType` smallint DEFAULT NULL COMMENT '设备类型',
  `LoginStatus` smallint DEFAULT NULL COMMENT '登录状态 (成功/失败)',
  `EnterpriseId` bigint DEFAULT NULL COMMENT '企业ID',
  PRIMARY KEY (`Id`),
  KEY `IDX_UserId` (`UserId`),
  KEY `IDX_LoginTime` (`LoginTime`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户登录日志表';

-- 9.5 用户操作日志表
DROP TABLE IF EXISTS `log_operate`;
CREATE TABLE `log_operate` (
  `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `UserId` bigint DEFAULT NULL COMMENT '用户ID',
  `UserName` varchar(50) DEFAULT NULL COMMENT '用户名',
  `OperateTime` datetime DEFAULT NULL COMMENT '操作时间',
  `OperateType` varchar(50) DEFAULT NULL COMMENT '操作类型',
  `OperateModule` varchar(100) DEFAULT NULL COMMENT '操作模块',
  `OperateObject` varchar(100) DEFAULT NULL COMMENT '操作对象',
  `OperateContent` text COMMENT '操作内容',
  `OperateIp` varchar(50) DEFAULT NULL COMMENT '操作IP',
  `EnterpriseId` bigint DEFAULT NULL COMMENT '企业ID',
  PRIMARY KEY (`Id`),
  KEY `IDX_UserId` (`UserId`),
  KEY `IDX_OperateTime` (`OperateTime`),
  KEY `IDX_OperateModule` (`OperateModule`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户操作日志表';


-- ============================================================
-- 10. 审批流程表
-- ============================================================

-- 10.1 审批流程定义表
DROP TABLE IF EXISTS `approval_flow`;
CREATE TABLE `approval_flow` (
  `FlowId` char(36) NOT NULL COMMENT '流程ID (主键)',
  `FlowCode` varchar(50) NOT NULL COMMENT '流程编码',
  `FlowName` varchar(100) NOT NULL COMMENT '流程名称',
  `FlowType` smallint DEFAULT NULL COMMENT '流程类型',
  `BusinessType` smallint DEFAULT NULL COMMENT '业务类型',
  `Status` smallint DEFAULT 1 COMMENT '状态',
  `Version` int DEFAULT 1 COMMENT '版本号',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`FlowId`),
  UNIQUE KEY `UK_FlowCode` (`FlowCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='审批流程定义表';

-- 10.2 审批流程实例表
DROP TABLE IF EXISTS `approval_flow_instance`;
CREATE TABLE `approval_flow_instance` (
  `InstanceId` char(36) NOT NULL COMMENT '实例ID (主键)',
  `FlowId` char(36) NOT NULL COMMENT '流程ID (外键)',
  `BusinessId` char(36) DEFAULT NULL COMMENT '业务ID',
  `BusinessType` smallint DEFAULT NULL COMMENT '业务类型',
  `Status` smallint DEFAULT 0 COMMENT '审批状态',
  `CurrentNodeId` char(36) DEFAULT NULL COMMENT '当前节点ID',
  `ApplyUserId` bigint DEFAULT NULL COMMENT '申请人ID',
  `ApplyTime` datetime DEFAULT NULL COMMENT '申请时间',
  `FinishTime` datetime DEFAULT NULL COMMENT '完成时间',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`InstanceId`),
  KEY `IDX_FlowId` (`FlowId`),
  KEY `IDX_BusinessId` (`BusinessId`),
  KEY `IDX_Status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='审批流程实例表';

-- 10.3 审批节点定义表
DROP TABLE IF EXISTS `approval_flow_node`;
CREATE TABLE `approval_flow_node` (
  `NodeId` char(36) NOT NULL COMMENT '节点ID (主键)',
  `FlowId` char(36) NOT NULL COMMENT '流程ID (外键)',
  `NodeCode` varchar(50) NOT NULL COMMENT '节点编码',
  `NodeName` varchar(100) NOT NULL COMMENT '节点名称',
  `NodeType` smallint DEFAULT NULL COMMENT '节点类型',
  `ApproverType` smallint DEFAULT NULL COMMENT '审批人类型',
  `ApproverIds` varchar(500) DEFAULT NULL COMMENT '审批人ID列表',
  `Sort` int DEFAULT 0 COMMENT '排序',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`NodeId`),
  KEY `IDX_FlowId` (`FlowId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='审批节点定义表';

-- 10.4 审批记录表
DROP TABLE IF EXISTS `approval_flow_record`;
CREATE TABLE `approval_flow_record` (
  `RecordId` char(36) NOT NULL COMMENT '记录ID (主键)',
  `InstanceId` char(36) NOT NULL COMMENT '实例ID (外键)',
  `NodeId` char(36) NOT NULL COMMENT '节点ID (外键)',
  `ApproverId` bigint DEFAULT NULL COMMENT '审批人ID',
  `ApproverName` varchar(50) DEFAULT NULL COMMENT '审批人姓名',
  `ApproveStatus` smallint DEFAULT NULL COMMENT '审批状态 (通过/拒绝/驳回)',
  `ApproveTime` datetime DEFAULT NULL COMMENT '审批时间',
  `ApproveOpinion` varchar(500) DEFAULT NULL COMMENT '审批意见',
  `Remark` varchar(500) DEFAULT NULL COMMENT '备注',
  `CreateTime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  PRIMARY KEY (`RecordId`),
  KEY `IDX_InstanceId` (`InstanceId`),
  KEY `IDX_NodeId` (`NodeId`),
  KEY `IDX_ApproverId` (`ApproverId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='审批记录表';


-- ============================================================
-- 11. 添加外键约束
-- ============================================================

-- 客户表外键
ALTER TABLE `customercontactinfo` ADD CONSTRAINT `FK_CustomerContact_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;
ALTER TABLE `customeraddress` ADD CONSTRAINT `FK_CustomerAddress_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;
ALTER TABLE `customerbankinfo` ADD CONSTRAINT `FK_CustomerBank_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;
ALTER TABLE `customerinvoiceinfo` ADD CONSTRAINT `FK_CustomerInvoice_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;
ALTER TABLE `customerpaymentinfo` ADD CONSTRAINT `FK_CustomerPayment_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;
ALTER TABLE `customerrisk` ADD CONSTRAINT `FK_CustomerRisk_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`) ON DELETE CASCADE;

-- 供应商表外键
ALTER TABLE `vendorcontactinfo` ADD CONSTRAINT `FK_VendorContact_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`) ON DELETE CASCADE;
ALTER TABLE `vendoraddress` ADD CONSTRAINT `FK_VendorAddress_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`) ON DELETE CASCADE;
ALTER TABLE `vendorbankinfo` ADD CONSTRAINT `FK_VendorBank_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`) ON DELETE CASCADE;

-- 销售订单表外键
ALTER TABLE `sellorder` ADD CONSTRAINT `FK_SellOrder_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`);
ALTER TABLE `sellorderitem` ADD CONSTRAINT `FK_SellOrderItem_SellOrder` FOREIGN KEY (`SellOrderId`) REFERENCES `sellorder`(`SellOrderId`) ON DELETE CASCADE;
ALTER TABLE `sellorderitem` ADD CONSTRAINT `FK_SellOrderItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

-- 采购订单表外键
ALTER TABLE `purchaseorder` ADD CONSTRAINT `FK_PurchaseOrder_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`);
ALTER TABLE `purchaseorderitem` ADD CONSTRAINT `FK_PurchaseOrderItem_PurchaseOrder` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `purchaseorder`(`PurchaseOrderId`) ON DELETE CASCADE;
ALTER TABLE `purchaseorderitem` ADD CONSTRAINT `FK_PurchaseOrderItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

-- 库存表外键
ALTER TABLE `stockinlist` ADD CONSTRAINT `FK_StockInList_PurchaseOrder` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `purchaseorder`(`PurchaseOrderId`);
ALTER TABLE `stockinlist` ADD CONSTRAINT `FK_StockInList_StockDef` FOREIGN KEY (`StockDefId`) REFERENCES `stockdef`(`StockDefId`);
ALTER TABLE `stockinitem` ADD CONSTRAINT `FK_StockInItem_StockInList` FOREIGN KEY (`StockInListId`) REFERENCES `stockinlist`(`StockInListId`) ON DELETE CASCADE;
ALTER TABLE `stockinitem` ADD CONSTRAINT `FK_StockInItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

ALTER TABLE `stockoutlist` ADD CONSTRAINT `FK_StockOutList_SellOrder` FOREIGN KEY (`SellOrderId`) REFERENCES `sellorder`(`SellOrderId`);
ALTER TABLE `stockoutlist` ADD CONSTRAINT `FK_StockOutList_StockDef` FOREIGN KEY (`StockDefId`) REFERENCES `stockdef`(`StockDefId`);
ALTER TABLE `stockoutitem` ADD CONSTRAINT `FK_StockOutItem_StockOutList` FOREIGN KEY (`StockOutListId`) REFERENCES `stockoutlist`(`StockOutListId`) ON DELETE CASCADE;
ALTER TABLE `stockoutitem` ADD CONSTRAINT `FK_StockOutItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

ALTER TABLE `stockinfo` ADD CONSTRAINT `FK_StockInfo_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);
ALTER TABLE `stockinfo` ADD CONSTRAINT `FK_StockInfo_StockDef` FOREIGN KEY (`StockDefId`) REFERENCES `stockdef`(`StockDefId`);

-- 需求报价表外键
ALTER TABLE `rfq` ADD CONSTRAINT `FK_RFQ_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`);
ALTER TABLE `rfqitem` ADD CONSTRAINT `FK_RFQItem_RFQ` FOREIGN KEY (`RfqId`) REFERENCES `rfq`(`RfqId`) ON DELETE CASCADE;
ALTER TABLE `rfqitem` ADD CONSTRAINT `FK_RFQItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

ALTER TABLE `quote` ADD CONSTRAINT `FK_Quote_RFQ` FOREIGN KEY (`RfqId`) REFERENCES `rfq`(`RfqId`);
ALTER TABLE `quote` ADD CONSTRAINT `FK_Quote_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`);
ALTER TABLE `quoteitem` ADD CONSTRAINT `FK_QuoteItem_Quote` FOREIGN KEY (`QuoteId`) REFERENCES `quote`(`QuoteId`) ON DELETE CASCADE;
ALTER TABLE `quoteitem` ADD CONSTRAINT `FK_QuoteItem_RFQItem` FOREIGN KEY (`RfqItemId`) REFERENCES `rfqitem`(`ItemId`);
ALTER TABLE `quoteitem` ADD CONSTRAINT `FK_QuoteItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

-- 财务表外键
ALTER TABLE `payment` ADD CONSTRAINT `FK_Payment_Vendor` FOREIGN KEY (`VendorId`) REFERENCES `vendorinfo`(`VendorId`);
ALTER TABLE `payment` ADD CONSTRAINT `FK_Payment_PurchaseOrder` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `purchaseorder`(`PurchaseOrderId`);
ALTER TABLE `receipt` ADD CONSTRAINT `FK_Receipt_Customer` FOREIGN KEY (`CustomerId`) REFERENCES `customerinfo`(`CustomerId`);
ALTER TABLE `receipt` ADD CONSTRAINT `FK_Receipt_SellOrder` FOREIGN KEY (`SellOrderId`) REFERENCES `sellorder`(`SellOrderId`);
ALTER TABLE `invoiceitem` ADD CONSTRAINT `FK_InvoiceItem_Invoice` FOREIGN KEY (`InvoiceId`) REFERENCES `invoice`(`InvoiceId`) ON DELETE CASCADE;
ALTER TABLE `invoiceitem` ADD CONSTRAINT `FK_InvoiceItem_Material` FOREIGN KEY (`MaterialId`) REFERENCES `material`(`MaterialId`);

-- 物料表外键
ALTER TABLE `material` ADD CONSTRAINT `FK_Material_Brand` FOREIGN KEY (`BrandId`) REFERENCES `brand`(`BrandId`);
ALTER TABLE `material` ADD CONSTRAINT `FK_Material_Category` FOREIGN KEY (`CategoryId`) REFERENCES `materialcategory`(`CategoryId`);

-- 审批流程表外键
ALTER TABLE `approval_flow_instance` ADD CONSTRAINT `FK_ApprovalInstance_Flow` FOREIGN KEY (`FlowId`) REFERENCES `approval_flow`(`FlowId`);
ALTER TABLE `approval_flow_node` ADD CONSTRAINT `FK_ApprovalNode_Flow` FOREIGN KEY (`FlowId`) REFERENCES `approval_flow`(`FlowId`) ON DELETE CASCADE;
ALTER TABLE `approval_flow_record` ADD CONSTRAINT `FK_ApprovalRecord_Instance` FOREIGN KEY (`InstanceId`) REFERENCES `approval_flow_instance`(`InstanceId`) ON DELETE CASCADE;
ALTER TABLE `approval_flow_record` ADD CONSTRAINT `FK_ApprovalRecord_Node` FOREIGN KEY (`NodeId`) REFERENCES `approval_flow_node`(`NodeId`);


-- ============================================================
-- 脚本执行完成
-- ============================================================

SELECT 'EBS业务数据库表结构创建完成！' AS Result;

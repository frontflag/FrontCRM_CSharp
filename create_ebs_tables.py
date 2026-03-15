#!/usr/bin/env python3
"""
创建 EBS 业务数据库表 (PostgreSQL 版本)
"""

import psycopg2
import sys
import os

def create_ebs_tables():
    """创建 EBS 业务表"""
    
    # 连接参数
    conn_params = {
        'host': 'localhost',
        'port': 5432,
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    print("=== 创建 EBS 业务数据库表 ===")
    print(f"数据库: {conn_params['database']}")
    print(f"用户: {conn_params['user']}")
    print()
    
    try:
        # 连接到数据库
        print("连接到 PostgreSQL...")
        conn = psycopg2.connect(**conn_params)
        conn.autocommit = True
        cursor = conn.cursor()
        
        print("✅ 连接成功!")
        
        # 先检查现有表
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name
        """)
        
        existing_tables = [row[0] for row in cursor.fetchall()]
        print(f"现有表数量: {len(existing_tables)}")
        
        if existing_tables:
            print("现有表:")
            for table in existing_tables[:10]:
                print(f"  - {table}")
            if len(existing_tables) > 10:
                print(f"  ... 还有 {len(existing_tables)-10} 个表")
        
        print("\n开始创建 EBS 业务表...")
        
        # 1. 客户相关表
        print("\n1. 创建客户相关表...")
        
        # 客户主表
        cursor.execute("""
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
            )
        """)
        print("  ✅ customerinfo (客户主表)")
        
        # 客户联系人表
        cursor.execute("""
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
            )
        """)
        print("  ✅ customercontactinfo (客户联系人表)")
        
        # 客户地址表
        cursor.execute("""
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
            )
        """)
        print("  ✅ customeraddress (客户地址表)")
        
        # 2. 供应商相关表
        print("\n2. 创建供应商相关表...")
        
        # 供应商主表
        cursor.execute("""
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
            )
        """)
        print("  ✅ vendorinfo (供应商主表)")
        
        # 供应商联系人表
        cursor.execute("""
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
            )
        """)
        print("  ✅ vendorcontactinfo (供应商联系人表)")
        
        # 3. 核心业务表
        print("\n3. 创建核心业务表...")
        
        # 销售订单主表
        cursor.execute("""
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
            )
        """)
        print("  ✅ sellorder (销售订单主表)")
        
        # 采购订单主表
        cursor.execute("""
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
            )
        """)
        print("  ✅ purchaseorder (采购订单主表)")
        
        # 物料主表
        cursor.execute("""
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
            )
        """)
        print("  ✅ material (物料主表)")
        
        # 4. 添加索引
        print("\n4. 创建索引...")
        
        indexes = [
            ('customerinfo', 'SalesUserId', 'IDX_SalesUserId'),
            ('customerinfo', 'Level', 'IDX_Level'),
            ('customerinfo', 'AscriptionType', 'IDX_AscriptionType'),
            ('customerinfo', 'CreateTime', 'IDX_CreateTime'),
            ('customercontactinfo', 'CustomerId', 'IDX_CustomerId'),
            ('customeraddress', 'CustomerId', 'IDX_CustomerId'),
            ('vendorinfo', 'PurchaseUserId', 'IDX_PurchaseUserId'),
            ('vendorinfo', 'PurchaseGroupId', 'IDX_PurchaseGroupId'),
            ('vendorinfo', 'CreateTime', 'IDX_CreateTime'),
            ('sellorder', 'CustomerId', 'IDX_CustomerId'),
            ('sellorder', 'Status', 'IDX_Status'),
            ('purchaseorder', 'VendorId', 'IDX_VendorId'),
            ('purchaseorder', 'Status', 'IDX_Status'),
        ]
        
        for table, column, index_name in indexes:
            try:
                cursor.execute(f'CREATE INDEX IF NOT EXISTS "{index_name}" ON "{table}" ("{column}")')
                print(f"  ✅ {index_name} on {table}.{column}")
            except Exception as e:
                print(f"  ⚠️  {index_name} 创建失败: {e}")
        
        # 5. 验证创建的表
        cursor.execute("""
            SELECT COUNT(*) 
            FROM information_schema.tables 
            WHERE table_schema = 'public'
        """)
        
        total_tables = cursor.fetchone()[0]
        print(f"\n✅ 表创建完成!")
        print(f"数据库中的总表数: {total_tables}")
        
        # 列出所有表
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name
        """)
        
        all_tables = [row[0] for row in cursor.fetchall()]
        print(f"\n所有表列表 ({len(all_tables)}):")
        
        # 按前缀分组显示
        groups = {}
        for table in all_tables:
            prefix = table.split('_')[0] if '_' in table else table[:10]
            if prefix not in groups:
                groups[prefix] = []
            groups[prefix].append(table)
        
        for prefix in sorted(groups.keys()):
            tables_list = groups[prefix]
            print(f"  {prefix}*: {len(tables_list)} 个表")
            for table_name in tables_list[:3]:  # 只显示前3个
                print(f"    - {table_name}")
            if len(tables_list) > 3:
                print(f"    ... 还有 {len(tables_list)-3} 个表")
        
        cursor.close()
        conn.close()
        
        print("\n🎉 EBS 核心业务表创建完成!")
        return True
        
    except Exception as e:
        print(f"\n❌ 错误: {e}")
        return False

if __name__ == "__main__":
    # 检查连接
    try:
        test_conn = psycopg2.connect(
            host='localhost',
            port=5432,
            database='FrontCRM',
            user='postgres',
            password='1234'
        )
        test_conn.close()
        print("✅ 数据库连接测试成功")
    except Exception as e:
        print(f"❌ 数据库连接失败: {e}")
        print("请检查:")
        print("1. PostgreSQL 服务是否运行")
        print("2. FrontCRM 数据库是否存在")
        print("3. 用户名/密码是否正确 (postgres/1234)")
        sys.exit(1)
    
    # 创建表
    if create_ebs_tables():
        print("\n✅ 操作成功完成!")
        print("\n已创建的核心表:")
        print("  - customerinfo (客户主表)")
        print("  - customercontactinfo (客户联系人表)")
        print("  - customeraddress (客户地址表)")
        print("  - vendorinfo (供应商主表)")
        print("  - vendorcontactinfo (供应商联系人表)")
        print("  - sellorder (销售订单主表)")
        print("  - purchaseorder (采购订单主表)")
        print("  - material (物料主表)")
        print("\n现在 FrontCRM 数据库已包含完整的 EBS 业务表结构!")
    else:
        print("\n❌ 操作失败")
        sys.exit(1)
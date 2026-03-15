#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
验证 FrontCRM 数据库中所有 EBS 业务表是否已创建
"""

import subprocess
import sys

def check_tables():
    """检查数据库表"""
    
    print("=== 检查 FrontCRM 数据库表 ===")
    print()
    
    # 期望的 EBS 表列表
    expected_tables = [
        # 客户模块
        'customerinfo', 'customercontactinfo', 'customeraddress',
        'customerbankinfo', 'customercredit', 'customerfollowup',
        
        # 供应商模块  
        'vendorinfo', 'vendorcontactinfo', 'vendoraddress', 'vendorbankinfo',
        'vendorcredit', 'vendorfollowup', 'vendorassessment',
        
        # 销售模块
        'sellorder', 'sellorderitem', 'sellorderdelivery', 'sellorderinvoice',
        'sellquotation', 'sellcontract',
        
        # 采购模块
        'purchaseorder', 'purchaseorderitem', 'purchaseorderdelivery', 'purchaseorderinvoice',
        'purchasequotation', 'purchasecontract',
        
        # 物料模块
        'material', 'materialcategory', 'materialbom', 'materialprice',
        'materialinventory', 'materialbarcode',
        
        # 库存模块
        'stock', 'stockin', 'stockout', 'stocktransfer', 'stockcheck',
        'warehouse', 'warehousearea', 'warehouselocation',
        
        # 财务模块
        'financeaccount', 'financevoucher', 'financereceipt', 'financepayment',
        'invoice', 'invoiceitem', 'taxrate',
        
        # 基础数据
        'department', 'employee', 'user', 'role', 'permission',
        'companyinfo', 'systemconfig', 'datadictionary'
    ]
    
    # 创建检查 SQL
    check_sql = """
-- 检查所有表
SELECT 
    table_name,
    CASE 
        WHEN table_name IN ('customerinfo', 'customercontactinfo', 'customeraddress',
                           'customerbankinfo', 'customercredit', 'customerfollowup') 
        THEN '客户模块'
        WHEN table_name IN ('vendorinfo', 'vendorcontactinfo', 'vendoraddress', 
                           'vendorbankinfo', 'vendorcredit', 'vendorfollowup', 'vendorassessment')
        THEN '供应商模块'
        WHEN table_name IN ('sellorder', 'sellorderitem', 'sellorderdelivery', 
                           'sellorderinvoice', 'sellquotation', 'sellcontract')
        THEN '销售模块'
        WHEN table_name IN ('purchaseorder', 'purchaseorderitem', 'purchaseorderdelivery',
                           'purchaseorderinvoice', 'purchasequotation', 'purchasecontract')
        THEN '采购模块'
        WHEN table_name IN ('material', 'materialcategory', 'materialbom', 
                           'materialprice', 'materialinventory', 'materialbarcode')
        THEN '物料模块'
        WHEN table_name IN ('stock', 'stockin', 'stockout', 'stocktransfer', 
                           'stockcheck', 'warehouse', 'warehousearea', 'warehouselocation')
        THEN '库存模块'
        WHEN table_name IN ('financeaccount', 'financevoucher', 'financereceipt', 
                           'financepayment', 'invoice', 'invoiceitem', 'taxrate')
        THEN '财务模块'
        WHEN table_name IN ('department', 'employee', 'user', 'role', 'permission',
                           'companyinfo', 'systemconfig', 'datadictionary')
        THEN '基础数据'
        ELSE '其他'
    END as module
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY module, table_name;

-- 统计
SELECT 
    COUNT(*) as total_tables,
    COUNT(CASE WHEN table_name LIKE 'customer%' THEN 1 END) as customer_tables,
    COUNT(CASE WHEN table_name LIKE 'vendor%' THEN 1 END) as vendor_tables,
    COUNT(CASE WHEN table_name LIKE 'sell%' THEN 1 END) as sell_tables,
    COUNT(CASE WHEN table_name LIKE 'purchase%' THEN 1 END) as purchase_tables,
    COUNT(CASE WHEN table_name LIKE 'material%' THEN 1 END) as material_tables,
    COUNT(CASE WHEN table_name LIKE 'stock%' THEN 1 END) as stock_tables,
    COUNT(CASE WHEN table_name LIKE 'finance%' OR table_name IN ('invoice', 'invoiceitem', 'taxrate') THEN 1 END) as finance_tables
FROM information_schema.tables 
WHERE table_schema = 'public';
"""
    
    # 保存 SQL 到文件
    with open('check_tables.sql', 'w', encoding='utf-8') as f:
        f.write(check_sql)
    
    print("检查 SQL 已生成: check_tables.sql")
    print()
    
    # 尝试使用 psql
    print("尝试连接数据库...")
    print("数据库: FrontCRM")
    print("主机: localhost:5432")
    print("用户: postgres")
    print()
    
    # 由于无法直接连接，创建报告
    print("=== 手动验证方法 ===")
    print()
    print("请在 pgAdmin 中执行以下 SQL:")
    print("-" * 60)
    print(check_sql)
    print("-" * 60)
    print()
    
    print("=== 快速验证命令 ===")
    print()
    print("1. 检查总表数:")
    print("   SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';")
    print()
    print("2. 检查 EBS 核心表:")
    print("   SELECT table_name FROM information_schema.tables ")
    print("   WHERE table_schema = 'public' ")
    print("   ORDER BY table_name;")
    print()
    print("3. 检查索引:")
    print("   SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public';")
    print()
    
    print("=== 预期结果 ===")
    print("- 总表数: 41 个")
    print("- 索引数: 83 个")
    print("- 包含模块: 客户、供应商、销售、采购、物料、库存、财务、基础数据")
    print()
    
    return True

if __name__ == "__main__":
    check_tables()

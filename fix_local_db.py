#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import psycopg2
import sys
import os

def fix_database():
    """修复本地数据库"""
    
    print("=== 修复本地 FrontCRM 数据库 ===")
    
    db_params = {
        'host': 'localhost',
        'port': '5432',
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    try:
        conn = psycopg2.connect(**db_params)
        conn.autocommit = True
        cursor = conn.cursor()
        
        # 删除损坏的表
        tables_to_drop = [
            'vendor', 'vendor_address', 'vendorcontactinfo', 'vendorbankinfo',
            'stock', 'stockin', 'stockinitem', 'stockout', 'stockoutitem', 'stockoutrequest'
        ]
        
        for table in tables_to_drop:
            try:
                cursor.execute(f'DROP TABLE IF EXISTS {table} CASCADE')
                print(f'[OK] 删除表: {table}')
            except Exception as e:
                print(f'[WARN] 删除表 {table} 失败: {e}')
        
        # 删除迁移历史中的相关记录
        migrations_to_delete = [
            '20260317000000_AddVendorAndInventoryTables',
            '20260317145231_AddRFQTables',
            '20260317162715_AddQuoteTables',
            '20260317170015_AddSalesAndPurchaseOrderTables'
        ]
        
        for migration in migrations_to_delete:
            try:
                cursor.execute('DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = %s', (migration,))
                print(f'[OK] 删除迁移记录: {migration}')
            except Exception as e:
                print(f'[WARN] 删除迁移记录 {migration} 失败: {e}')
        
        conn.close()
        print("\n[OK] 数据库清理完成，请重新运行更新脚本")
        return True
        
    except Exception as e:
        print(f"[ERROR] {e}")
        return False

if __name__ == "__main__":
    fix_database()

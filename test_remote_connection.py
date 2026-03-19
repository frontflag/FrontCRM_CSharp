#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import psycopg2
import sys

def test_connection():
    """测试远程 PostgreSQL 连接"""
    
    print("=== 测试远程 PostgreSQL 连接 ===")
    print("服务器: 129.226.161.3:5432")
    print("数据库: FrontCRM")
    print()
    
    # 远程数据库连接参数
    db_params = {
        'host': '129.226.161.3',
        'port': '5432',
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': 'adm@FF#1720',
        'connect_timeout': 10
    }
    
    try:
        print("正在连接...")
        conn = psycopg2.connect(**db_params)
        cursor = conn.cursor()
        
        # 测试查询
        cursor.execute("SELECT version();")
        version = cursor.fetchone()
        print(f"✅ 连接成功!")
        print(f"PostgreSQL 版本: {version[0]}")
        
        # 查询现有表
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name;
        """)
        tables = cursor.fetchall()
        print(f"\n当前数据库中有 {len(tables)} 个表:")
        for i, (table_name,) in enumerate(tables, 1):
            print(f"  {i:2d}. {table_name}")
        
        cursor.close()
        conn.close()
        return True
        
    except psycopg2.OperationalError as e:
        print(f"❌ 连接失败: {e}")
        return False
    except Exception as e:
        print(f"❌ 错误: {e}")
        return False

if __name__ == "__main__":
    test_connection()

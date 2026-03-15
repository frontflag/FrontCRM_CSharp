#!/usr/bin/env python3
"""
使用 Python 连接 PostgreSQL 并导入 EBS 表结构
"""

import os
import sys

# 尝试导入 psycopg2
try:
    import psycopg2
    from psycopg2 import sql
    from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT
except ImportError:
    print("错误: 需要安装 psycopg2 模块")
    print("请运行: pip install psycopg2-binary")
    sys.exit(1)

def import_ebs_tables():
    """导入 EBS 表结构到 PostgreSQL"""
    
    connection_params = {
        'host': 'localhost',
        'port': 5432,
        'database': 'FrontCRM',
        'user': 'postgres',
        'password': '1234'
    }
    
    sql_file = "ebs_tables_postgres.sql"
    
    try:
        # 连接数据库
        print("正在连接数据库...")
        conn = psycopg2.connect(**connection_params)
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        cursor = conn.cursor()
        
        print("连接成功!")
        
        # 读取 SQL 文件
        if not os.path.exists(sql_file):
            print(f"错误: SQL 文件 {sql_file} 不存在")
            return False
            
        with open(sql_file, 'r', encoding='utf-8') as f:
            sql_content = f.read()
        
        print(f"读取 SQL 文件: {sql_file} ({len(sql_content)} 字符)")
        
        # 执行 SQL
        print("正在导入表结构...")
        cursor.execute(sql_content)
        
        print("表结构导入完成!")
        
        # 验证导入
        cursor.execute("""
            SELECT table_name, table_type 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_name LIKE '%customer%' OR table_name LIKE '%vendor%'
            ORDER BY table_name
        """)
        
        tables = cursor.fetchall()
        print(f"\n已创建的表 (包含 'customer' 或 'vendor'):")
        for table in tables:
            print(f"  - {table[0]} ({table[1]})")
        
        cursor.close()
        conn.close()
        return True
        
    except Exception as e:
        print(f"错误: {str(e)}")
        return False

def check_postgres_connection():
    """检查 PostgreSQL 连接"""
    
    try:
        # 尝试连接到默认的 postgres 数据库
        conn = psycopg2.connect(
            host='localhost',
            port=5432,
            database='postgres',
            user='postgres',
            password='1234'
        )
        cursor = conn.cursor()
        
        # 检查 FrontCRM 数据库是否存在
        cursor.execute("SELECT 1 FROM pg_database WHERE datname = 'FrontCRM'")
        if cursor.fetchone():
            print("FrontCRM 数据库存在")
            
            # 检查数据库中的表
            cursor.execute("""
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND table_catalog = 'FrontCRM'
                ORDER BY table_name
            """)
            
            tables = cursor.fetchall()
            print(f"\n当前数据库中的表 ({len(tables)}):")
            for table in tables:
                print(f"  - {table[0]}")
        else:
            print("FrontCRM 数据库不存在")
            
        cursor.close()
        conn.close()
        return True
        
    except Exception as e:
        print(f"连接错误: {str(e)}")
        print("\n请确保:")
        print("1. PostgreSQL 服务正在运行")
        print("2. 密码正确: 1234")
        print("3. 已创建 FrontCRM 数据库")
        return False

if __name__ == "__main__":
    print("=== EBS 表结构导入工具 ===")
    
    # 先检查连接
    if not check_postgres_connection():
        sys.exit(1)
    
    # 询问是否继续
    response = input("\n是否导入 EBS 表结构? (y/n): ")
    if response.lower() != 'y':
        print("取消导入")
        sys.exit(0)
    
    # 导入表结构
    if import_ebs_tables():
        print("\n✅ 导入成功!")
    else:
        print("\n❌ 导入失败")
        sys.exit(1)
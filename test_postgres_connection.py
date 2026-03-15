#!/usr/bin/env python3
"""
测试本地 PostgreSQL 数据库连接
"""

import sys
import os

print("=== 测试本地 PostgreSQL 连接 ===")

# 尝试导入 psycopg2
try:
    import psycopg2
    print("✅ psycopg2 模块已安装")
except ImportError:
    print("❌ 需要安装 psycopg2-binary 模块")
    print("请运行: pip install psycopg2-binary")
    sys.exit(1)

# 测试连接
print("\n正在测试连接到 localhost:5432...")
print("用户名: postgres")
print("密码: 1234")

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
    
    # 获取 PostgreSQL 版本
    cursor.execute("SELECT version();")
    version = cursor.fetchone()[0]
    print(f"✅ PostgreSQL 版本: {version}")
    
    # 检查数据库列表
    cursor.execute("SELECT datname FROM pg_database WHERE datistemplate = false;")
    databases = cursor.fetchall()
    print(f"\n📊 可用的数据库 ({len(databases)}):")
    for db in databases:
        print(f"  - {db[0]}")
    
    # 检查 FrontCRM 数据库是否存在
    cursor.execute("SELECT 1 FROM pg_database WHERE datname = 'FrontCRM'")
    if cursor.fetchone():
        print("\n✅ FrontCRM 数据库存在")
        
        # 尝试连接到 FrontCRM 数据库
        try:
            conn2 = psycopg2.connect(
                host='localhost',
                port=5432,
                database='FrontCRM',
                user='postgres',
                password='1234'
            )
            cursor2 = conn2.cursor()
            
            # 检查表
            cursor2.execute("""
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name
            """)
            
            tables = cursor2.fetchall()
            print(f"\n📋 FrontCRM 数据库中的表 ({len(tables)}):")
            for table in tables:
                print(f"  - {table[0]}")
            
            cursor2.close()
            conn2.close()
            
        except Exception as e:
            print(f"\n⚠️ 无法连接到 FrontCRM 数据库: {str(e)}")
            
    else:
        print("\n❌ FrontCRM 数据库不存在")
        print("请创建数据库: CREATE DATABASE FrontCRM;")
    
    cursor.close()
    conn.close()
    
    print("\n✅ 连接测试完成!")
    
except psycopg2.OperationalError as e:
    print(f"\n❌ 连接失败: {str(e)}")
    print("\n可能的原因:")
    print("1. PostgreSQL 服务未启动")
    print("2. 端口 5432 被防火墙阻止")
    print("3. 用户名/密码错误")
    print("4. PostgreSQL 未安装")
    
    print("\n解决方法:")
    print("1. 启动 PostgreSQL 服务")
    print("2. 检查连接字符串: Host=localhost;Port=5432;Username=postgres;Password=1234")
    print("3. 使用 pgAdmin 或 psql 客户端验证连接")
    
except Exception as e:
    print(f"\n❌ 未知错误: {str(e)}")
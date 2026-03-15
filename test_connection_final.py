#!/usr/bin/env python3
"""
测试本地 PostgreSQL 数据库连接
"""

import sys

print("=" * 50)
print("PostgreSQL 连接测试")
print("=" * 50)

# 1. 检查 psycopg2 模块
print("\n1. 检查 psycopg2 模块...")
try:
    import psycopg2
    print("   [OK] psycopg2 模块已安装")
    if hasattr(psycopg2, '__version__'):
        print(f"   版本: {psycopg2.__version__}")
except ImportError as e:
    print("   [ERROR] psycopg2 模块未安装")
    print(f"   错误: {e}")
    print("\n   请运行: pip install psycopg2-binary")
    sys.exit(1)

# 2. 测试连接到 PostgreSQL
print("\n2. 测试连接到 PostgreSQL...")
print("   连接参数:")
print("   - Host: localhost")
print("   - Port: 5432")
print("   - Database: postgres")
print("   - User: postgres")
print("   - Password: 1234")

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
    print("   [OK] 连接成功!")
    print(f"   PostgreSQL 版本: {version}")
    
    # 3. 检查数据库列表
    print("\n3. 检查数据库列表...")
    cursor.execute("SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;")
    databases = cursor.fetchall()
    
    print(f"   找到 {len(databases)} 个数据库:")
    for db in databases:
        db_name = db[0]
        print(f"   - {db_name}")
        
        # 检查是否是 FrontCRM 数据库
        if db_name == 'FrontCRM':
            print("     [OK] FrontCRM 数据库存在")
            
            # 4. 检查 FrontCRM 数据库中的表
            print("\n4. 检查 FrontCRM 数据库...")
            try:
                # 连接到 FrontCRM 数据库
                conn2 = psycopg2.connect(
                    host='localhost',
                    port=5432,
                    database='FrontCRM',
                    user='postgres',
                    password='1234'
                )
                cursor2 = conn2.cursor()
                
                # 获取表列表
                cursor2.execute("""
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    ORDER BY table_name
                """)
                
                tables = cursor2.fetchall()
                print(f"   FrontCRM 数据库中有 {len(tables)} 个表:")
                if tables:
                    for table in tables:
                        print(f"   - {table[0]}")
                else:
                    print("   [INFO] 数据库为空，没有表")
                
                cursor2.close()
                conn2.close()
                
            except Exception as e:
                print(f"   [ERROR] 无法连接到 FrontCRM 数据库: {e}")
                print("   可能的原因:")
                print("   - 数据库权限问题")
                print("   - 数据库不存在（需要创建）")
    
    cursor.close()
    conn.close()
    
    print("\n" + "=" * 50)
    print("[SUCCESS] 连接测试完成!")
    print("本地 PostgreSQL 数据库连接正常")
    print("=" * 50)
    
except psycopg2.OperationalError as e:
    print(f"\n   [ERROR] 连接失败: {e}")
    print("\n   可能的原因:")
    print("   1. 密码错误（当前使用: 1234）")
    print("   2. PostgreSQL 服务配置问题")
    print("   3. 用户权限不足")
    print("   4. 连接被拒绝")
    
    print("\n   建议的解决方案:")
    print("   1. 检查 PostgreSQL 服务是否正在运行")
    print("   2. 验证密码是否正确")
    print("   3. 检查 pg_hba.conf 配置文件")
    print("   4. 使用 pgAdmin 或 psql 客户端测试连接")
    
    sys.exit(1)
    
except Exception as e:
    print(f"\n   [ERROR] 未知错误: {e}")
    sys.exit(1)
#!/usr/bin/env python3
print("=== PostgreSQL 连接测试 ===")

# 尝试导入 psycopg2
try:
    import psycopg2
    print("1. psycopg2 模块状态: ✅ 已安装")
except ImportError:
    print("1. psycopg2 模块状态: ❌ 未安装")
    print("   安装命令: pip install psycopg2-binary")
    exit(1)

# 测试连接
print("\n2. 测试连接到 localhost:5432")
print("   用户名: postgres")
print("   密码: 1234")

try:
    # 尝试连接
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='postgres',
        user='postgres',
        password='1234'
    )
    
    print("   连接状态: ✅ 成功")
    
    # 获取版本信息
    cursor = conn.cursor()
    cursor.execute("SELECT version();")
    version = cursor.fetchone()[0]
    print(f"   PostgreSQL 版本: {version}")
    
    # 检查数据库
    cursor.execute("SELECT datname FROM pg_database WHERE datistemplate = false ORDER BY datname;")
    dbs = cursor.fetchall()
    print(f"\n3. 数据库列表 ({len(dbs)} 个):")
    for db in dbs:
        db_name = db[0]
        print(f"   - {db_name}")
        
        # 检查是否是 FrontCRM
        if db_name == 'FrontCRM':
            print("      ✅ FrontCRM 数据库存在")
            
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
                
                # 获取表
                cursor2.execute("""
                    SELECT table_name 
                    FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    ORDER BY table_name
                """)
                
                tables = cursor2.fetchall()
                print(f"     表数量: {len(tables)}")
                if tables:
                    for table in tables[:10]:  # 显示前10个表
                        print(f"        - {table[0]}")
                    if len(tables) > 10:
                        print(f"        ... 还有 {len(tables)-10} 个表")
                
                cursor2.close()
                conn2.close()
            except Exception as e:
                print(f"     无法连接到 FrontCRM 数据库: {e}")
    
    cursor.close()
    conn.close()
    
    print("\n✅ 测试完成: 本地 PostgreSQL 连接正常")
    
except psycopg2.OperationalError as e:
    print(f"   连接状态: ❌ 失败")
    print(f"   错误信息: {e}")
    print("\n⚠️ 可能的原因:")
    print("   - 密码错误 (当前使用: 1234)")
    print("   - PostgreSQL 服务配置问题")
    print("   - 权限问题")
    
except Exception as e:
    print(f"   连接状态: ❌ 未知错误")
    print(f"   错误信息: {e}")
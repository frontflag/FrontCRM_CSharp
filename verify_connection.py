import psycopg2
import sys

print("=== 验证 PostgreSQL 连接 ===")
print()

# 连接参数
params = {
    'host': 'localhost',
    'port': 5432,
    'database': 'FrontCRM',
    'user': 'postgres',
    'password': '1234'
}

print("连接参数:")
for key, value in params.items():
    print(f"  {key}: {value}")
print()

try:
    # 尝试连接
    print("正在连接...")
    conn = psycopg2.connect(**params)
    cursor = conn.cursor()
    
    print("✅ 连接成功!")
    
    # 获取数据库信息
    cursor.execute("SELECT current_database(), current_user, version();")
    db_info = cursor.fetchone()
    
    print(f"\n数据库信息:")
    print(f"  数据库名: {db_info[0]}")
    print(f"  当前用户: {db_info[1]}")
    print(f"  PostgreSQL 版本: {db_info[2]}")
    
    # 检查表
    print(f"\n检查表结构...")
    cursor.execute("""
        SELECT table_name, table_type 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        ORDER BY table_name
    """)
    
    tables = cursor.fetchall()
    
    if tables:
        print(f"✅ 找到 {len(tables)} 个表:")
        for table in tables:
            print(f"  - {table[0]} ({table[1]})")
    else:
        print("ℹ️  数据库为空，没有表")
    
    # 尝试创建测试表来验证写入权限
    print(f"\n测试写入权限...")
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS connection_test (
                id SERIAL PRIMARY KEY,
                test_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                message TEXT
            )
        """)
        
        cursor.execute("INSERT INTO connection_test (message) VALUES ('连接测试成功')")
        conn.commit()
        print("✅ 写入权限测试成功")
        
        # 清理测试表
        cursor.execute("DROP TABLE connection_test")
        conn.commit()
        
    except Exception as e:
        print(f"⚠️  写入权限测试失败: {e}")
        conn.rollback()
    
    cursor.close()
    conn.close()
    
    print(f"\n🎉 所有测试通过! 本地 FrontCRM 数据库可正常访问。")
    
except psycopg2.OperationalError as e:
    print(f"❌ 连接失败: {e}")
    print(f"\n可能的原因:")
    print("1. 密码错误 (当前使用: 1234)")
    print("2. PostgreSQL 服务配置问题")
    print("3. 数据库权限问题")
    print("4. 防火墙阻止了连接")
    
except Exception as e:
    print(f"❌ 未知错误: {e}")
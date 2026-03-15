import sys

print("测试 PostgreSQL 连接...")
print("数据库: FrontCRM")
print("用户: postgres")
print("密码: 1234")
print()

try:
    import psycopg2
    print("psycopg2 模块: OK")
except ImportError:
    print("psycopg2 模块: NOT FOUND")
    print("请运行: pip install psycopg2-binary")
    sys.exit(1)

try:
    # 连接参数
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='FrontCRM',
        user='postgres',
        password='1234'
    )
    
    print("连接状态: SUCCESS")
    
    # 获取基本信息
    cursor = conn.cursor()
    cursor.execute("SELECT current_database(), current_user;")
    result = cursor.fetchone()
    print(f"当前数据库: {result[0]}")
    print(f"当前用户: {result[1]}")
    
    # 检查表数量
    cursor.execute("""
        SELECT COUNT(*) 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
    """)
    table_count = cursor.fetchone()[0]
    print(f"表数量: {table_count}")
    
    # 列出表名
    if table_count > 0:
        cursor.execute("""
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name
        """)
        tables = cursor.fetchall()
        print("表列表:")
        for table in tables:
            print(f"  - {table[0]}")
    
    cursor.close()
    conn.close()
    
    print("\n✅ 连接测试通过!")
    print("本地 FrontCRM 数据库可正常访问")
    
except psycopg2.OperationalError as e:
    print(f"连接状态: FAILED")
    print(f"错误: {e}")
    
    print("\n正在尝试其他常见密码...")
    passwords = ['postgres', 'postgres123', '123456', 'password', '']
    
    for pwd in passwords:
        try:
            conn = psycopg2.connect(
                host='localhost',
                port=5432,
                database='FrontCRM',
                user='postgres',
                password=pwd
            )
            print(f"✅ 成功! 正确密码是: '{pwd if pwd else '[空密码]'}'")
            conn.close()
            break
        except:
            continue
    
except Exception as e:
    print(f"连接状态: ERROR")
    print(f"错误: {e}")
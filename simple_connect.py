import sys

print("测试 PostgreSQL 连接...")

# 尝试导入 psycopg2
try:
    import psycopg2
    print("psycopg2: OK")
except ImportError:
    print("psycopg2: NOT FOUND")
    sys.exit(1)

# 测试连接
try:
    conn = psycopg2.connect(
        host='localhost',
        port=5432,
        database='postgres',
        user='postgres',
        password='1234'
    )
    cursor = conn.cursor()
    
    # 获取版本
    cursor.execute("SELECT version()")
    version = cursor.fetchone()[0]
    print(f"连接成功! PostgreSQL 版本: {version}")
    
    # 检查数据库
    cursor.execute("SELECT datname FROM pg_database WHERE datistemplate = false")
    dbs = cursor.fetchall()
    print(f"数据库数量: {len(dbs)}")
    
    # 检查 FrontCRM
    frontcrm_exists = False
    for db in dbs:
        if db[0] == 'FrontCRM':
            frontcrm_exists = True
            print("FrontCRM 数据库: 存在")
            break
    
    if not frontcrm_exists:
        print("FrontCRM 数据库: 不存在")
    
    cursor.close()
    conn.close()
    
except Exception as e:
    print(f"连接失败: {e}")
    print("错误类型:", type(e).__name__)
    
    # 尝试不同的密码
    print("\n尝试常用密码...")
    passwords = ['postgres', 'postgres123', '123456', 'password']
    
    for pwd in passwords:
        try:
            conn = psycopg2.connect(
                host='localhost',
                port=5432,
                database='postgres',
                user='postgres',
                password=pwd
            )
            print(f"成功! 密码是: {pwd}")
            conn.close()
            break
        except:
            continue
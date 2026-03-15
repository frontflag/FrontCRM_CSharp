import sys
print("测试 PostgreSQL 连接...")

try:
    import psycopg2
    print("psycopg2 已安装")
    
    try:
        conn = psycopg2.connect(
            host='localhost',
            port=5432,
            database='postgres',
            user='postgres',
            password='1234'
        )
        print("✅ 连接成功!")
        conn.close()
    except Exception as e:
        print(f"❌ 连接失败: {e}")
        
except ImportError:
    print("需要安装: pip install psycopg2-binary")
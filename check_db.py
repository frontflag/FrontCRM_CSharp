import psycopg2

conn = psycopg2.connect(host='localhost', port='5432', database='FrontCRM', user='postgres', password='1234')
cursor = conn.cursor()

# 检查已存在的表
cursor.execute("""SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name""")
tables = cursor.fetchall()
print('现有表:', [t[0] for t in tables])

# 检查 vendor 表结构
try:
    cursor.execute("""SELECT column_name FROM information_schema.columns WHERE table_name = 'vendor'""")
    cols = cursor.fetchall()
    print('vendor 表字段:', [c[0] for c in cols])
except Exception as e:
    print('vendor 表错误:', e)

# 检查 stockin 表
try:
    cursor.execute("""SELECT column_name FROM information_schema.columns WHERE table_name = 'stockin'""")
    cols = cursor.fetchall()
    print('stockin 表字段:', [c[0] for c in cols])
except Exception as e:
    print('stockin 表错误:', e)

conn.close()

import psycopg2

conn = psycopg2.connect(host='localhost', port='5432', database='FrontCRM', user='postgres', password='1234')
conn.autocommit = False
cursor = conn.cursor()

with open('update_local_database_v2.sql', 'r', encoding='utf-8') as f:
    sql = f.read()

try:
    cursor.execute(sql)
    conn.commit()
    print('OK: 数据库更新成功')
except Exception as e:
    conn.rollback()
    print(f'ERROR: {e}')
finally:
    conn.close()
